-- Trka do Durmitora: šema baze za Supabase.
-- Pokreni u Supabase → SQL Editor → New query → Run.
-- Uključi i anonimnu prijavu: Authentication → Sign In / Providers → Anonymous sign-ins.

-- 1) Profil igrača (jedan red po nalogu; i gost dobija anonimni nalog)
create table if not exists public.profiles (
  id uuid primary key references auth.users (id) on delete cascade,
  ime text not null default 'Igrač' check (char_length(ime) <= 20),
  grad text check (char_length(grad) <= 40),
  skola text check (char_length(skola) <= 60),
  drzava text default 'ME',
  bodovi integer not null default 0 check (bodovi >= 0),
  novcici integer not null default 150 check (novcici >= 0),
  izgled jsonb not null default '{}'::jsonb,
  kupljeno jsonb not null default '[]'::jsonb,
  powerupi jsonb not null default '{}'::jsonb,
  uzrast text default 'odrasli',
  updated_at timestamptz not null default now()
);

-- 2) Rezultati trka (za dnevne i sedmične rang-liste)
create table if not exists public.rezultati (
  id bigint generated always as identity primary key,
  user_id uuid not null references public.profiles (id) on delete cascade,
  mapa text not null,
  mjesto smallint not null check (mjesto between 1 and 4),
  bodovi integer not null check (bodovi between 0 and 1000),
  tacni smallint not null default 0,
  odgovori smallint not null default 0,
  created_at timestamptz not null default now()
);
create index if not exists rezultati_vrijeme on public.rezultati (created_at desc);
create index if not exists rezultati_korisnik on public.rezultati (user_id);

-- 3) Pitanja koja predlažu igrači (ti ih odobravaš: status 'odobreno')
create table if not exists public.predlozi_pitanja (
  id bigint generated always as identity primary key,
  user_id uuid not null references public.profiles (id) on delete cascade,
  oblast text not null,
  tip text not null default 'izbor',
  pitanje text not null check (char_length(pitanje) between 10 and 300),
  opcije jsonb not null,
  tacno text not null,
  status text not null default 'na_cekanju' check (status in ('na_cekanju', 'odobreno', 'odbijeno')),
  created_at timestamptz not null default now()
);

-- Sigurnost na nivou reda (RLS): svako vidi i mijenja samo svoje podatke
alter table public.profiles enable row level security;
alter table public.rezultati enable row level security;
alter table public.predlozi_pitanja enable row level security;

create policy "profil: čitam svoj" on public.profiles for select using (auth.uid() = id);
create policy "profil: pravim svoj" on public.profiles for insert with check (auth.uid() = id);
create policy "profil: mijenjam svoj" on public.profiles for update using (auth.uid() = id);

create policy "rezultati: čitam svoje" on public.rezultati for select using (auth.uid() = user_id);
create policy "rezultati: upisujem svoje" on public.rezultati for insert with check (auth.uid() = user_id);

create policy "predlozi: čitam svoje" on public.predlozi_pitanja for select using (auth.uid() = user_id);
create policy "predlozi: šaljem svoje" on public.predlozi_pitanja for insert with check (auth.uid() = user_id and status = 'na_cekanju');

-- Javne rang-liste: pokazuju samo ime, grad, školu i bodove (bez novčića i opreme)
create or replace view public.rang_ukupno as
  select id, ime, grad, skola, bodovi from public.profiles order by bodovi desc;

create or replace view public.rang_sedmica as
  select p.id, p.ime, p.grad, p.skola, sum(r.bodovi)::int as bodovi, count(*)::int as trke
  from public.rezultati r join public.profiles p on p.id = r.user_id
  where r.created_at > now() - interval '7 days'
  group by p.id order by bodovi desc;

create or replace view public.rang_danas as
  select p.id, p.ime, p.grad, p.skola, sum(r.bodovi)::int as bodovi, count(*)::int as trke
  from public.rezultati r join public.profiles p on p.id = r.user_id
  where r.created_at >= date_trunc('day', now())
  group by p.id order by bodovi desc;

grant select on public.rang_ukupno, public.rang_sedmica, public.rang_danas to anon, authenticated;

-- Odobrena pitanja igrača mogu se učitati u igru
create or replace view public.odobrena_pitanja as
  select id, oblast, tip, pitanje, opcije, tacno from public.predlozi_pitanja where status = 'odobreno';
grant select on public.odobrena_pitanja to anon, authenticated;

-- NAPOMENA: bodovi i novčići se sada upisuju iz aplikacije, pa ih vješt igrač može lažirati.
-- Prije pravog izlaska to prebacujemo u Edge Function koja provjerava rezultat trke na serveru.
