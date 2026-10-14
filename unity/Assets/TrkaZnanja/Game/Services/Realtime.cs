// Veza sa Supabase Realtime (WebSocket, Phoenix protokol 1.0.0) za jedan kanal: broadcast poruke i presence (ko je tu).
// Prijem ide u pozadinskoj niti, a događaji se isporučuju na glavnoj niti preko Pump() (zove ga MpSession iz Update-a).
// WebGL nema System.Net.WebSockets, pa tamo multiplayer zasad nije dostupan.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrkaZnanja.Core;
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class RealtimeChannel
    {
        public enum State { Idle, Connecting, Joined, Closed, Failed }

        public string Topic { get; }
        public State Status { get; private set; } = State.Idle;
        public string Error { get; private set; }
        public readonly Dictionary<string, Dictionary<string, object>> Presence = new Dictionary<string, Dictionary<string, object>>();

        public event Action<string, Dictionary<string, object>> OnBroadcast;   // događaj, sadržaj
        public event Action OnPresence;
        public event Action<State> OnStatus;

        readonly string presenceKey;
        readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<Action> events = new ConcurrentQueue<Action>();
        ClientWebSocket ws;
        CancellationTokenSource cts;
        int nextRef = 1;
        string joinRef;
        float heartbeatAt;
        Dictionary<string, object> pendingTrack;
        readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);

        public static bool Supported
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return false;
#else
                return true;
#endif
            }
        }

        public RealtimeChannel(string channel, string presenceKey)
        {
            Topic = "realtime:" + channel;
            this.presenceKey = presenceKey;
        }

        string Ref() => (nextRef++).ToString();

        // Za testove: kanali u istom procesu pričaju preko memorije (bez interneta i servera).
        public static bool Loopback;

        public void Connect()
        {
            if (Loopback) { LocalHub.Join(this); return; }
            if (!Supported) { SetStatus(State.Failed, "Multiplayer nije dostupan u web verziji."); return; }
            Status = State.Connecting;
            cts = new CancellationTokenSource();
            ws = new ClientWebSocket();
            var host = Net.Url.Replace("https://", "wss://").Replace("http://", "ws://");
            var uri = new Uri($"{host}/realtime/v1/websocket?apikey={Uri.EscapeDataString(Net.AnonKey)}&vsn=1.0.0");
            var token = string.IsNullOrEmpty(Net.AccessToken) ? Net.AnonKey : Net.AccessToken;
            Task.Run(async () =>
            {
                try
                {
                    await ws.ConnectAsync(uri, cts.Token);
                    joinRef = Ref();
                    await SendRaw(Phoenix.Join(Topic, presenceKey, token, joinRef));
                    await ReceiveLoop();
                }
                catch (Exception e)
                {
                    if (!cts.IsCancellationRequested) events.Enqueue(() => SetStatus(State.Failed, e.Message));
                }
            });
        }

        async Task ReceiveLoop()
        {
            var buf = new byte[16384];
            var sb = new StringBuilder();
            while (ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
            {
                var r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
                if (r.MessageType == WebSocketMessageType.Close) break;
                sb.Append(Encoding.UTF8.GetString(buf, 0, r.Count));
                if (!r.EndOfMessage) continue;
                inbox.Enqueue(sb.ToString());
                sb.Clear();
            }
            if (!cts.IsCancellationRequested) events.Enqueue(() => SetStatus(State.Closed, "Veza je prekinuta."));
        }

        async Task SendRaw(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            await sendLock.WaitAsync();
            try { if (ws != null && ws.State == WebSocketState.Open) await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token); }
            finally { sendLock.Release(); }
        }

        void Send(string text)
        {
            if (ws == null || ws.State != WebSocketState.Open) return;
            Task.Run(async () => { try { await SendRaw(text); } catch (Exception) { /* prekid se javlja iz ReceiveLoop */ } });
        }

        public void Broadcast(string ev, Dictionary<string, object> payload)
        {
            if (Status != State.Joined) return;
            if (Loopback) { LocalHub.Send(this, ev, payload); return; }
            Send(Phoenix.Broadcast(Topic, ev, payload, Ref(), joinRef));
        }

        // Moji podaci u presence (ime, izgled, vrijeme ulaska…). Ako još nismo ušli u kanal, šalje se odmah po ulasku.
        public void Track(Dictionary<string, object> meta)
        {
            pendingTrack = meta;
            if (Loopback) { if (Status == State.Joined) LocalHub.Track(this, meta); return; }
            if (Status == State.Joined) Send(Phoenix.Track(Topic, meta, Ref(), joinRef));
        }

        public void Close()
        {
            if (Status == State.Closed) return;
            if (Loopback) { LocalHub.Leave(this); Status = State.Closed; return; }
            try
            {
                if (ws != null && ws.State == WebSocketState.Open)
                {
                    var leave = Phoenix.Leave(Topic, Ref(), joinRef);
                    var sock = ws; var c = cts;
                    Task.Run(async () =>
                    {
                        try { await SendRaw(leave); await sock.CloseAsync(WebSocketCloseStatus.NormalClosure, "kraj", CancellationToken.None); }
                        catch (Exception) { }
                        finally { c.Cancel(); }
                    });
                }
                else cts?.Cancel();
            }
            catch (Exception) { }
            Status = State.Closed;
        }

        // ---- lokalna veza (testovi) ----
        static class LocalHub
        {
            static readonly List<RealtimeChannel> all = new List<RealtimeChannel>();
            static readonly Dictionary<RealtimeChannel, Dictionary<string, object>> metas = new Dictionary<RealtimeChannel, Dictionary<string, object>>();

            public static void Join(RealtimeChannel c)
            {
                all.Add(c);
                c.events.Enqueue(() =>
                {
                    c.SetStatus(State.Joined);
                    if (c.pendingTrack != null) Track(c, c.pendingTrack);
                    else SyncPresence(c.Topic);
                });
            }

            public static void Track(RealtimeChannel c, Dictionary<string, object> meta)
            {
                metas[c] = meta;
                SyncPresence(c.Topic);
            }

            public static void Leave(RealtimeChannel c)
            {
                all.Remove(c);
                metas.Remove(c);
                SyncPresence(c.Topic);
            }

            static void SyncPresence(string topic)
            {
                var state = all.Where(x => x.Topic == topic && metas.ContainsKey(x)).ToDictionary(x => x.presenceKey, x => metas[x]);
                foreach (var x in all.Where(x => x.Topic == topic).ToList())
                {
                    var copy = state.ToDictionary(kv => kv.Key, kv => Json.Obj(Json.Parse(Json.Write(kv.Value))));
                    x.events.Enqueue(() => { x.Presence.Clear(); foreach (var kv in copy) x.Presence[kv.Key] = kv.Value; x.OnPresence?.Invoke(); });
                }
            }

            public static void Send(RealtimeChannel from, string ev, Dictionary<string, object> payload)
            {
                var text = Json.Write(payload);
                foreach (var x in all.Where(x => x != from && x.Topic == from.Topic).ToList())
                    x.events.Enqueue(() => x.OnBroadcast?.Invoke(ev, Json.Obj(Json.Parse(text))));
            }
        }

        void SetStatus(State s, string error = null)
        {
            Status = s;
            Error = error;
            if (error != null) Debug.LogWarning($"Trka znanja · multiplayer ({Topic}): {error}");
            OnStatus?.Invoke(s);
        }

        // Glavna nit: obradi primljene poruke i šalji heartbeat svakih 25 s.
        public void Pump()
        {
            while (events.TryDequeue(out var a)) a();
            while (inbox.TryDequeue(out var text)) Handle(Phoenix.Decode(text));
            if (Status == State.Joined && Time.realtimeSinceStartup >= heartbeatAt)
            {
                heartbeatAt = Time.realtimeSinceStartup + 25;
                Send(Phoenix.Heartbeat(Ref()));
            }
        }

        void Handle(Phoenix.Msg m)
        {
            if (m == null || m.Topic != Topic) return;
            switch (m.Event)
            {
                case "phx_reply":
                    if (m.Ref == joinRef && Status != State.Joined)
                    {
                        if (Json.Str(m.Payload, "status") == "ok")
                        {
                            heartbeatAt = Time.realtimeSinceStartup + 25;
                            SetStatus(State.Joined);
                            if (pendingTrack != null) Send(Phoenix.Track(Topic, pendingTrack, Ref(), joinRef));
                        }
                        else SetStatus(State.Failed, Json.Str(Json.Obj(Json.Get(m.Payload, "response")), "reason", "Ulazak u sobu nije uspio."));
                    }
                    break;
                case "broadcast":
                    OnBroadcast?.Invoke(Json.Str(m.Payload, "event"), Json.Obj(Json.Get(m.Payload, "payload")) ?? new Dictionary<string, object>());
                    break;
                case "presence_state":
                    Presence.Clear();
                    foreach (var kv in Phoenix.PresenceMetas(m.Payload)) Presence[kv.Key] = kv.Value;
                    OnPresence?.Invoke();
                    break;
                case "presence_diff":
                    foreach (var kv in Phoenix.PresenceMetas(Json.Obj(Json.Get(m.Payload, "leaves")))) Presence.Remove(kv.Key);
                    foreach (var kv in Phoenix.PresenceMetas(Json.Obj(Json.Get(m.Payload, "joins")))) Presence[kv.Key] = kv.Value;
                    OnPresence?.Invoke();
                    break;
                case "phx_error":
                case "phx_close":
                    SetStatus(State.Closed, "Kanal je zatvoren.");
                    break;
            }
        }
    }
}
