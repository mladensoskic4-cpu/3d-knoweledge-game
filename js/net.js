// Veza sa serverom (Supabase). Dok CONFIG nije popunjen, igra radi offline i sve čuva na uređaju.
// Upiši Project URL i anon (publishable) ključ iz Supabase → Project Settings → API. Service role ključ NIKAD ovdje.
export const CONFIG = {
  url: 'https://soyaqpuronnhxcbwhnwt.supabase.co',
  anonKey: 'sb_publishable_RoIgUlgm6tyatqSPQOa-Og_ZBVsGUYz' // javni (publishable) ključ, smije biti u kodu
};

let sb = null, user = null;
export const isOnline = () => !!(sb && user);

function loadScript(src) {
  return new Promise((res, rej) => {
    const s = document.createElement('script');
    s.src = src; s.onload = res; s.onerror = rej;
    document.head.append(s);
  });
}

export async function initNet() {
  if (!CONFIG.url || !CONFIG.anonKey) return false;
  try {
    await loadScript('https://cdn.jsdelivr.net/npm/@supabase/supabase-js@2.117.2/dist/umd/supabase.js');
    sb = window.supabase.createClient(CONFIG.url, CONFIG.anonKey);
    let { data } = await sb.auth.getSession();
    if (!data.session) ({ data } = await sb.auth.signInAnonymously());
    user = data.session?.user || data.user || null;
    return !!user;
  } catch (e) {
    sb = null; user = null;
    return false;
  }
}

export async function pushProfile(P) {
  if (!isOnline()) return;
  await sb.from('profiles').upsert({
    id: user.id, ime: P.ime || 'Igrač', grad: P.grad || null, skola: P.skola || null,
    bodovi: P.bodovi, novcici: P.novcici, izgled: P.izgled, kupljeno: P.kupljeno, powerupi: P.powerupi,
    uzrast: P.podesavanja.uzrast, updated_at: new Date().toISOString()
  });
}

export async function pullProfile() {
  if (!isOnline()) return null;
  const { data } = await sb.from('profiles').select('*').eq('id', user.id).maybeSingle();
  return data;
}

export async function submitResult(r) {
  if (!isOnline()) return;
  await sb.from('rezultati').insert({ user_id: user.id, mapa: r.mapa, mjesto: r.mjesto, bodovi: r.bodovi, tacni: r.tacni, odgovori: r.odgovori });
}

// scope: 'danas' | 'sedmica' | 'ukupno'; filter: { grad } ili { skola }
export async function leaderboard(scope, filter = {}) {
  if (!isOnline()) return null;
  let q = sb.from(`rang_${scope}`).select('id, ime, grad, skola, bodovi').limit(50);
  if (filter.grad) q = q.eq('grad', filter.grad);
  if (filter.skola) q = q.eq('skola', filter.skola);
  const { data, error } = await q;
  return error ? null : data.map(r => ({ ...r, ja: r.id === user.id }));
}

export async function submitSuggestion(s) {
  if (!isOnline()) return false;
  const { error } = await sb.from('predlozi_pitanja').insert({ user_id: user.id, ...s });
  return !error;
}

export async function approvedQuestions() {
  if (!isOnline()) return [];
  const { data } = await sb.from('odobrena_pitanja').select('*').limit(1000);
  return (data || []).map(r => ({ id: `igr-${r.id}`, oblast: r.oblast, tip: r.tip, tezina: 2, pitanje: r.pitanje, opcije: r.opcije, tacno: r.tacno, info: 'Pitanje je predložio igrač.' }));
}
