#!/usr/bin/env python3
"""
Skida slike za edukativnu igru u četiri kategorije:
  - mjesta       (217 poznatih mjesta u svijetu, s koordinatama)
  - zivotinje    (111 životinja)
  - tijelo       (36 dijelova ljudskog tijela)
  - instrumenti  (55 muzičkih instrumenata)

Slike dolaze s Wikipedije / Wikimedia Commonsa i uzimaju se SAMO slike
sa slobodnom licencom (pilicense=free). Za svaku sliku se zapisuju autor i
licenca, koje treba prikazati u igri (npr. na ekranu "Zasluge").

Pokretanje:
  python skini_sve_slike.py                  # sve četiri kategorije
  python skini_sve_slike.py mjesta           # samo jedna
  python skini_sve_slike.py tijelo instrumenti

Može se ponovo pokrenuti: preskače ono što je već skinuto.
Potrebno: Python 3.8+, bez dodatnih paketa, internet veza.

Rezultat (za svaku kategoriju poseban folder):
  zivotinje/slike/zivotinja_001.jpg ...   slike (imena ne otkrivaju odgovor)
  zivotinje/podaci.json                   svi podaci za igru
  zivotinje/zasluge.txt                   spisak autora i licenci
"""

import html
import json
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

# ---------------------------------------------------------------------------
# PODEŠAVANJA
# ---------------------------------------------------------------------------

# Wikimedia traži da u User-Agentu stoji neki kontakt. Upiši svoj email.
KONTAKT = "tvoj.email@primjer.com"
USER_AGENT = f"EdukativnaIgra/1.0 (skidanje slika; {KONTAKT})"

SIRINA = 1280            # širina slike u pikselima
PAUZA = 1.0              # sekundi između stavki (da ne opteretimo server)

WIKI_API = "https://en.wikipedia.org/w/api.php"

# ---------------------------------------------------------------------------
# MJESTA: (naziv, naslov na engleskoj Wikipediji, država, kontinent)
# Nova mjesta dodaji NA KRAJ spiska, da već skinute slike zadrže brojeve.
# ---------------------------------------------------------------------------

MJESTA = [
    # --- EVROPA ---
    ("Ajfelov toranj", "Eiffel Tower", "Francuska", "Evropa"),
    ("Luvr", "Louvre", "Francuska", "Evropa"),
    ("Notre-Dame", "Notre-Dame de Paris", "Francuska", "Evropa"),
    ("Versajska palata", "Palace of Versailles", "Francuska", "Evropa"),
    ("Mont-Saint-Michel", "Mont-Saint-Michel", "Francuska", "Evropa"),
    ("Koloseum", "Colosseum", "Italija", "Evropa"),
    ("Fontana di Trevi", "Trevi Fountain", "Italija", "Evropa"),
    ("Krivi toranj u Pizi", "Leaning Tower of Pisa", "Italija", "Evropa"),
    ("Kanal Grande, Venecija", "Grand Canal (Venice)", "Italija", "Evropa"),
    ("Firentinska katedrala", "Florence Cathedral", "Italija", "Evropa"),
    ("Milanska katedrala", "Milan Cathedral", "Italija", "Evropa"),
    ("Amalfijska obala", "Amalfi Coast", "Italija", "Evropa"),
    ("Bazilika svetog Petra", "St. Peter's Basilica", "Vatikan", "Evropa"),
    ("Sagrada Familia", "Sagrada Família", "Španija", "Evropa"),
    ("Alhambra", "Alhambra", "Španija", "Evropa"),
    ("Kula Belém", "Belém Tower", "Portugal", "Evropa"),
    ("Big Ben", "Big Ben", "Ujedinjeno Kraljevstvo", "Evropa"),
    ("Tower Bridge", "Tower Bridge", "Ujedinjeno Kraljevstvo", "Evropa"),
    ("Stonehenge", "Stonehenge", "Ujedinjeno Kraljevstvo", "Evropa"),
    ("Edinburški dvorac", "Edinburgh Castle", "Ujedinjeno Kraljevstvo", "Evropa"),
    ("Divov nasip", "Giant's Causeway", "Ujedinjeno Kraljevstvo", "Evropa"),
    ("Klifovi Moher", "Cliffs of Moher", "Irska", "Evropa"),
    ("Brandenburška kapija", "Brandenburg Gate", "Njemačka", "Evropa"),
    ("Dvorac Neuschwanstein", "Neuschwanstein Castle", "Njemačka", "Evropa"),
    ("Kelnska katedrala", "Cologne Cathedral", "Njemačka", "Evropa"),
    ("Amsterdamski kanali", "Canals of Amsterdam", "Holandija", "Evropa"),
    ("Atomium", "Atomium", "Belgija", "Evropa"),
    ("Matterhorn", "Matterhorn", "Švicarska", "Evropa"),
    ("Dvorac Schönbrunn", "Schönbrunn Palace", "Austrija", "Evropa"),
    ("Karlov most", "Charles Bridge", "Češka", "Evropa"),
    ("Dvorac Wawel", "Wawel Castle", "Poljska", "Evropa"),
    ("Mađarski parlament", "Hungarian Parliament Building", "Mađarska", "Evropa"),
    ("Dvorac Bran", "Bran Castle", "Rumunija", "Evropa"),
    ("Rilski manastir", "Rila Monastery", "Bugarska", "Evropa"),
    ("Akropolj", "Acropolis of Athens", "Grčka", "Evropa"),
    ("Santorini", "Santorini", "Grčka", "Evropa"),
    ("Meteori", "Meteora", "Grčka", "Evropa"),
    ("Stari most u Mostaru", "Stari Most", "Bosna i Hercegovina", "Evropa"),
    ("Baščaršija", "Baščaršija", "Bosna i Hercegovina", "Evropa"),
    ("Dubrovačke zidine", "Walls of Dubrovnik", "Hrvatska", "Evropa"),
    ("Plitvička jezera", "Plitvice Lakes National Park", "Hrvatska", "Evropa"),
    ("Kotor", "Kotor", "Crna Gora", "Evropa"),
    ("Beogradska tvrđava", "Belgrade Fortress", "Srbija", "Evropa"),
    ("Bledsko jezero", "Lake Bled", "Slovenija", "Evropa"),
    ("Ohridsko jezero", "Lake Ohrid", "Sjeverna Makedonija", "Evropa"),
    ("Aja Sofija", "Hagia Sophia", "Turska", "Evropa"),
    ("Hram Vasilija Blaženog", "Saint Basil's Cathedral", "Rusija", "Evropa"),
    ("Moskovski Kremlj", "Moscow Kremlin", "Rusija", "Evropa"),
    ("Zimski dvorac", "Winter Palace", "Rusija", "Evropa"),
    ("Geirangerfjord", "Geirangerfjord", "Norveška", "Evropa"),
    ("Gullfoss", "Gullfoss", "Island", "Evropa"),

    # --- AZIJA ---
    ("Kapadokija", "Cappadocia", "Turska", "Azija"),
    ("Pamukkale", "Pamukkale", "Turska", "Azija"),
    ("Kineski zid", "Great Wall of China", "Kina", "Azija"),
    ("Zabranjeni grad", "Forbidden City", "Kina", "Azija"),
    ("Terakota vojska", "Terracotta Army", "Kina", "Azija"),
    ("Planine Zhangjiajie", "Zhangjiajie National Forest Park", "Kina", "Azija"),
    ("Palata Potala", "Potala Palace", "Kina", "Azija"),
    ("Luka Victoria, Hong Kong", "Victoria Harbour", "Kina (Hong Kong)", "Azija"),
    ("Taipei 101", "Taipei 101", "Tajvan", "Azija"),
    ("Fudži", "Mount Fuji", "Japan", "Azija"),
    ("Fushimi Inari", "Fushimi Inari-taisha", "Japan", "Azija"),
    ("Kinkaku-ji (Zlatni paviljon)", "Kinkaku-ji", "Japan", "Azija"),
    ("Raskrsnica Shibuya", "Shibuya Crossing", "Japan", "Azija"),
    ("Palata Gyeongbokgung", "Gyeongbokgung", "Južna Koreja", "Azija"),
    ("Tadž Mahal", "Taj Mahal", "Indija", "Azija"),
    ("Zlatni hram", "Golden Temple", "Indija", "Azija"),
    ("Kapija Indije", "India Gate", "Indija", "Azija"),
    ("Mount Everest", "Mount Everest", "Nepal / Kina", "Azija"),
    ("Sigiriya", "Sigiriya", "Šri Lanka", "Azija"),
    ("Angkor Vat", "Angkor Wat", "Kambodža", "Azija"),
    ("Wat Arun", "Wat Arun", "Tajland", "Azija"),
    ("Bagan", "Bagan", "Mjanmar", "Azija"),
    ("Zaliv Ha Long", "Ha Long Bay", "Vijetnam", "Azija"),
    ("Petronas tornjevi", "Petronas Towers", "Malezija", "Azija"),
    ("Marina Bay Sands", "Marina Bay Sands", "Singapur", "Azija"),
    ("Borobudur", "Borobudur", "Indonezija", "Azija"),
    ("Rižine terase Banaue", "Banaue Rice Terraces", "Filipini", "Azija"),
    ("Čokoladna brda", "Chocolate Hills", "Filipini", "Azija"),
    ("Burdž Halifa", "Burj Khalifa", "Ujedinjeni Arapski Emirati", "Azija"),
    ("Džamija šeika Zajeda", "Sheikh Zayed Grand Mosque", "Ujedinjeni Arapski Emirati", "Azija"),
    ("Petra", "Petra", "Jordan", "Azija"),
    ("Persepolis", "Persepolis", "Iran", "Azija"),
    ("Trg Naqsh-e Jahan, Isfahan", "Naqsh-e Jahan Square", "Iran", "Azija"),
    ("Registan, Samarkand", "Registan", "Uzbekistan", "Azija"),
    ("Bajkalsko jezero", "Lake Baikal", "Rusija", "Azija"),

    # --- AFRIKA ---
    ("Piramide u Gizi", "Giza pyramid complex", "Egipat", "Afrika"),
    ("Velika sfinga", "Great Sphinx of Giza", "Egipat", "Afrika"),
    ("Abu Simbel", "Abu Simbel", "Egipat", "Afrika"),
    ("Karnak", "Karnak", "Egipat", "Afrika"),
    ("Kartaga", "Carthage", "Tunis", "Afrika"),
    ("Chefchaouen (Plavi grad)", "Chefchaouen", "Maroko", "Afrika"),
    ("Džamija Hasana II", "Hassan II Mosque", "Maroko", "Afrika"),
    ("Pustinja Erg Chebbi", "Erg Chebbi", "Maroko", "Afrika"),
    ("Velika džamija u Dženeu", "Great Mosque of Djenné", "Mali", "Afrika"),
    ("Crkva sv. Đorđa, Lalibela", "Church of Saint George, Lalibela", "Etiopija", "Afrika"),
    ("Kilimandžaro", "Mount Kilimanjaro", "Tanzanija", "Afrika"),
    ("Serengeti", "Serengeti National Park", "Tanzanija", "Afrika"),
    ("Ngorongoro", "Ngorongoro Conservation Area", "Tanzanija", "Afrika"),
    ("Viktorijini slapovi", "Victoria Falls", "Zambija / Zimbabve", "Afrika"),
    ("Veliki Zimbabve", "Great Zimbabwe", "Zimbabve", "Afrika"),
    ("Delta Okavanga", "Okavango Delta", "Bocvana", "Afrika"),
    ("Sossusvlei", "Sossusvlei", "Namibija", "Afrika"),
    ("Stolna planina", "Table Mountain", "Južna Afrika", "Afrika"),
    ("Rt dobre nade", "Cape of Good Hope", "Južna Afrika", "Afrika"),
    ("Aleja baobaba", "Avenue of the Baobabs", "Madagaskar", "Afrika"),

    # --- SJEVERNA I SREDNJA AMERIKA ---
    ("Kip slobode", "Statue of Liberty", "SAD", "Sjeverna Amerika"),
    ("Empire State Building", "Empire State Building", "SAD", "Sjeverna Amerika"),
    ("Times Square", "Times Square", "SAD", "Sjeverna Amerika"),
    ("Bijela kuća", "White House", "SAD", "Sjeverna Amerika"),
    ("Kapitol", "United States Capitol", "SAD", "Sjeverna Amerika"),
    ("Most Golden Gate", "Golden Gate Bridge", "SAD", "Sjeverna Amerika"),
    ("Natpis Hollywood", "Hollywood Sign", "SAD", "Sjeverna Amerika"),
    ("Las Vegas Strip", "Las Vegas Strip", "SAD", "Sjeverna Amerika"),
    ("Space Needle", "Space Needle", "SAD", "Sjeverna Amerika"),
    ("Gateway Arch", "Gateway Arch", "SAD", "Sjeverna Amerika"),
    ("Mount Rushmore", "Mount Rushmore", "SAD", "Sjeverna Amerika"),
    ("Veliki kanjon", "Grand Canyon", "SAD", "Sjeverna Amerika"),
    ("Horseshoe Bend", "Horseshoe Bend (Arizona)", "SAD", "Sjeverna Amerika"),
    ("Kanjon Antilope", "Antelope Canyon", "SAD", "Sjeverna Amerika"),
    ("Monument Valley", "Monument Valley", "SAD", "Sjeverna Amerika"),
    ("Kanjon Bryce", "Bryce Canyon National Park", "SAD", "Sjeverna Amerika"),
    ("Half Dome, Yosemite", "Half Dome", "SAD", "Sjeverna Amerika"),
    ("Gejzir Old Faithful", "Old Faithful", "SAD", "Sjeverna Amerika"),
    ("Denali", "Denali", "SAD", "Sjeverna Amerika"),
    ("Nijagarini slapovi", "Niagara Falls", "Kanada / SAD", "Sjeverna Amerika"),
    ("CN toranj", "CN Tower", "Kanada", "Sjeverna Amerika"),
    ("Jezero Moraine", "Moraine Lake", "Kanada", "Sjeverna Amerika"),
    ("Château Frontenac", "Château Frontenac", "Kanada", "Sjeverna Amerika"),
    ("Chichén Itzá", "Chichen Itza", "Meksiko", "Sjeverna Amerika"),
    ("Teotihuacán", "Teotihuacan", "Meksiko", "Sjeverna Amerika"),
    ("Tulum", "Tulum", "Meksiko", "Sjeverna Amerika"),
    ("Tikal", "Tikal", "Gvatemala", "Sjeverna Amerika"),
    ("Velika plava rupa", "Great Blue Hole", "Beliz", "Sjeverna Amerika"),
    ("Panamski kanal", "Panama Canal", "Panama", "Sjeverna Amerika"),
    ("Malecón, Havana", "Malecón, Havana", "Kuba", "Sjeverna Amerika"),

    # --- JUŽNA AMERIKA ---
    ("Machu Picchu", "Machu Picchu", "Peru", "Južna Amerika"),
    ("Linije Nazca", "Nazca Lines", "Peru", "Južna Amerika"),
    ("Planina duge (Vinicunca)", "Vinicunca", "Peru", "Južna Amerika"),
    ("Jezero Titikaka", "Lake Titicaca", "Peru / Bolivija", "Južna Amerika"),
    ("Salar de Uyuni", "Salar de Uyuni", "Bolivija", "Južna Amerika"),
    ("Krist Iskupitelj", "Christ the Redeemer (statue)", "Brazil", "Južna Amerika"),
    ("Šećerna glava", "Sugarloaf Mountain", "Brazil", "Južna Amerika"),
    ("Katedrala u Brasiliji", "Cathedral of Brasília", "Brazil", "Južna Amerika"),
    ("Lençóis Maranhenses", "Lençóis Maranhenses National Park", "Brazil", "Južna Amerika"),
    ("Slapovi Iguazú", "Iguazu Falls", "Argentina / Brazil", "Južna Amerika"),
    ("Obelisk u Buenos Airesu", "Obelisco de Buenos Aires", "Argentina", "Južna Amerika"),
    ("Glečer Perito Moreno", "Perito Moreno Glacier", "Argentina", "Južna Amerika"),
    ("Fitz Roy", "Monte Fitz Roy", "Argentina / Čile", "Južna Amerika"),
    ("Torres del Paine", "Torres del Paine National Park", "Čile", "Južna Amerika"),
    ("Moai, Uskršnje ostrvo", "Moai", "Čile", "Južna Amerika"),
    ("Anđeoski slapovi", "Angel Falls", "Venecuela", "Južna Amerika"),
    ("Cartagena", "Cartagena, Colombia", "Kolumbija", "Južna Amerika"),
    ("Caño Cristales", "Caño Cristales", "Kolumbija", "Južna Amerika"),
    ("Galapagos", "Galápagos Islands", "Ekvador", "Južna Amerika"),

    # --- OKEANIJA ---
    ("Sidnejska opera", "Sydney Opera House", "Australija", "Okeanija"),
    ("Sidnejski lučki most", "Sydney Harbour Bridge", "Australija", "Okeanija"),
    ("Uluru", "Uluru", "Australija", "Okeanija"),
    ("Veliki koralni greben", "Great Barrier Reef", "Australija", "Okeanija"),
    ("Dvanaest apostola", "Twelve Apostles (Victoria)", "Australija", "Okeanija"),
    ("Plaža Whitehaven", "Whitehaven Beach", "Australija", "Okeanija"),
    ("Pinnacles", "The Pinnacles (Western Australia)", "Australija", "Okeanija"),
    ("Milford Sound", "Milford Sound", "Novi Zeland", "Okeanija"),
    ("Aoraki / Mount Cook", "Aoraki / Mount Cook", "Novi Zeland", "Okeanija"),
    ("Hobbiton", "Hobbiton Movie Set", "Novi Zeland", "Okeanija"),
    ("Sky Tower, Auckland", "Sky Tower (Auckland)", "Novi Zeland", "Okeanija"),
    ("Wai-O-Tapu", "Wai-O-Tapu", "Novi Zeland", "Okeanija"),
    ("Bora Bora", "Bora Bora", "Francuska Polinezija", "Okeanija"),

    # --- NAŠI PROSTORI (dodatak) ---
    # Dodano na kraj spiska, da slike koje su već skinute zadrže svoje brojeve.

    # Crna Gora
    ("Sveti Stefan", "Sveti Stefan", "Crna Gora", "Evropa"),
    ("Perast", "Perast", "Crna Gora", "Evropa"),
    ("Gospa od Škrpjela", "Our Lady of the Rocks", "Crna Gora", "Evropa"),
    ("Budva, Stari grad", "Budva", "Crna Gora", "Evropa"),
    ("Herceg Novi", "Herceg Novi", "Crna Gora", "Evropa"),
    ("Ulcinj", "Ulcinj", "Crna Gora", "Evropa"),
    ("Stari Bar", "Stari Bar", "Crna Gora", "Evropa"),
    ("Manastir Ostrog", "Ostrog Monastery", "Crna Gora", "Evropa"),
    ("Cetinjski manastir", "Cetinje Monastery", "Crna Gora", "Evropa"),
    ("Lovćen", "Lovćen", "Crna Gora", "Evropa"),
    ("Kanjon Tare", "Tara River Canyon", "Crna Gora", "Evropa"),
    ("Most na Đurđevića Tari", "Đurđevića Tara Bridge", "Crna Gora", "Evropa"),
    ("Durmitor", "Durmitor", "Crna Gora", "Evropa"),
    ("Biogradska gora", "Biogradska Gora National Park", "Crna Gora", "Evropa"),
    ("Skadarsko jezero", "Lake Skadar", "Crna Gora / Albanija", "Evropa"),
    ("Rijeka Crnojevića", "Rijeka Crnojevića", "Crna Gora", "Evropa"),
    ("Manastir Morača", "Morača Monastery", "Crna Gora", "Evropa"),

    # Bosna i Hercegovina
    ("Višegradska ćuprija", "Mehmed Paša Sokolović Bridge", "Bosna i Hercegovina", "Evropa"),
    ("Vodopad Kravica", "Kravica Waterfall", "Bosna i Hercegovina", "Evropa"),
    ("Vodopad u Jajcu", "Pliva Waterfall", "Bosna i Hercegovina", "Evropa"),
    ("Štrbački buk", "Štrbački buk", "Bosna i Hercegovina", "Evropa"),
    ("Blagajska tekija", "Blagaj Tekke", "Bosna i Hercegovina", "Evropa"),
    ("Počitelj", "Počitelj", "Bosna i Hercegovina", "Evropa"),
    ("Vijećnica, Sarajevo", "Sarajevo City Hall", "Bosna i Hercegovina", "Evropa"),
    ("Arslanagića most, Trebinje", "Arslanagić Bridge", "Bosna i Hercegovina", "Evropa"),
    ("Nacionalni park Sutjeska", "Sutjeska National Park", "Bosna i Hercegovina", "Evropa"),

    # Hrvatska
    ("Dioklecijanova palača, Split", "Diocletian's Palace", "Hrvatska", "Evropa"),
    ("Arena u Puli", "Pula Arena", "Hrvatska", "Evropa"),
    ("Zlatni rat, Brač", "Zlatni Rat", "Hrvatska", "Evropa"),
    ("Nacionalni park Krka", "Krka National Park", "Hrvatska", "Evropa"),
    ("Zagrebačka katedrala", "Zagreb Cathedral", "Hrvatska", "Evropa"),
    ("Trogir", "Trogir", "Hrvatska", "Evropa"),
    ("Rovinj", "Rovinj", "Hrvatska", "Evropa"),
    ("Katedrala sv. Jakova, Šibenik", "Cathedral of St. James, Šibenik", "Hrvatska", "Evropa"),

    # Srbija
    ("Petrovaradinska tvrđava", "Petrovaradin Fortress", "Srbija", "Evropa"),
    ("Hram Svetog Save", "Church of Saint Sava", "Srbija", "Evropa"),
    ("Đavolja varoš", "Devil's Town", "Srbija", "Evropa"),
    ("Manastir Studenica", "Studenica Monastery", "Srbija", "Evropa"),
    ("Golubačka tvrđava", "Golubac Fortress", "Srbija", "Evropa"),
    ("Drvengrad", "Drvengrad", "Srbija", "Evropa"),
    ("Felix Romuliana", "Felix Romuliana", "Srbija", "Evropa"),

    # Slovenija
    ("Postojnska jama", "Postojna Cave", "Slovenija", "Evropa"),
    ("Predjamski grad", "Predjama Castle", "Slovenija", "Evropa"),
    ("Triglav", "Triglav", "Slovenija", "Evropa"),
    ("Piran", "Piran", "Slovenija", "Evropa"),
    ("Tromostovlje, Ljubljana", "Triple Bridge", "Slovenija", "Evropa"),

    # Sjeverna Makedonija
    ("Kanjon Matka", "Matka Canyon", "Sjeverna Makedonija", "Evropa"),
    ("Crkva sv. Jovana Kaneo, Ohrid", "Church of St. John at Kaneo", "Sjeverna Makedonija", "Evropa"),
    ("Kameni most, Skoplje", "Stone Bridge (Skopje)", "Sjeverna Makedonija", "Evropa"),
]

# ---------------------------------------------------------------------------
# ŽIVOTINJE: (naziv, naslov na engleskoj Wikipediji, grupa, gdje živi)
# ---------------------------------------------------------------------------

ZIVOTINJE = [
    # Sisari
    ("Lav", "Lion", "Sisar", "Afrika"),
    ("Tigar", "Tiger", "Sisar", "Azija"),
    ("Slon", "African bush elephant", "Sisar", "Afrika"),
    ("Žirafa", "Giraffe", "Sisar", "Afrika"),
    ("Zebra", "Plains zebra", "Sisar", "Afrika"),
    ("Nilski konj", "Hippopotamus", "Sisar", "Afrika"),
    ("Nosorog", "White rhinoceros", "Sisar", "Afrika"),
    ("Gepard", "Cheetah", "Sisar", "Afrika"),
    ("Leopard", "Leopard", "Sisar", "Afrika, Azija"),
    ("Gorila", "Western gorilla", "Sisar", "Afrika"),
    ("Šimpanza", "Chimpanzee", "Sisar", "Afrika"),
    ("Orangutan", "Bornean orangutan", "Sisar", "Azija"),
    ("Velika panda", "Giant panda", "Sisar", "Azija"),
    ("Crvena panda", "Red panda", "Sisar", "Azija"),
    ("Koala", "Koala", "Sisar", "Australija"),
    ("Kengur", "Red kangaroo", "Sisar", "Australija"),
    ("Čudnovati kljunaš", "Platypus", "Sisar", "Australija"),
    ("Polarni medvjed", "Polar bear", "Sisar", "Arktik"),
    ("Mrki medvjed", "Brown bear", "Sisar", "Evropa, Azija, Sjeverna Amerika"),
    ("Vuk", "Wolf", "Sisar", "Evropa, Azija, Sjeverna Amerika"),
    ("Lisica", "Red fox", "Sisar", "Evropa, Azija, Sjeverna Amerika"),
    ("Los", "Moose", "Sisar", "Evropa, Azija, Sjeverna Amerika"),
    ("Sobov", "Reindeer", "Sisar", "Arktik i daleki sjever"),
    ("Bizon", "American bison", "Sisar", "Sjeverna Amerika"),
    ("Deva", "Dromedary", "Sisar", "Afrika, Azija"),
    ("Lama", "Llama", "Sisar", "Južna Amerika"),
    ("Lijenčina", "Brown-throated sloth", "Sisar", "Srednja i Južna Amerika"),
    ("Jaguar", "Jaguar", "Sisar", "Srednja i Južna Amerika"),
    ("Rakun", "Raccoon", "Sisar", "Sjeverna Amerika"),
    ("Jež", "European hedgehog", "Sisar", "Evropa"),
    ("Vjeverica", "Red squirrel", "Sisar", "Evropa, Azija"),
    ("Zec", "European rabbit", "Sisar", "Evropa"),
    ("Dabar", "North American beaver", "Sisar", "Sjeverna Amerika"),
    ("Vidra", "Eurasian otter", "Sisar", "Evropa, Azija"),
    ("Merkat", "Meerkat", "Sisar", "Afrika"),
    ("Hijena", "Spotted hyena", "Sisar", "Afrika"),
    ("Ris", "Eurasian lynx", "Sisar", "Evropa, Azija"),
    ("Snježni leopard", "Snow leopard", "Sisar", "Azija"),
    ("Armadilo", "Nine-banded armadillo", "Sisar", "Sjeverna i Južna Amerika"),
    ("Mravojed", "Giant anteater", "Sisar", "Srednja i Južna Amerika"),
    ("Lemur", "Ring-tailed lemur", "Sisar", "Madagaskar (Afrika)"),
    ("Konj", "Horse", "Sisar", "Domaća životinja"),
    ("Krava", "Cattle", "Sisar", "Domaća životinja"),
    ("Svinja", "Domestic pig", "Sisar", "Domaća životinja"),
    ("Ovca", "Sheep", "Sisar", "Domaća životinja"),
    ("Koza", "Goat", "Sisar", "Domaća životinja"),
    ("Pas", "Dog", "Sisar", "Domaća životinja"),
    ("Mačka", "Cat", "Sisar", "Domaća životinja"),
    ("Magarac", "Donkey", "Sisar", "Domaća životinja"),
    ("Plavi kit", "Blue whale", "Sisar", "Okeani"),
    ("Grbavi kit", "Humpback whale", "Sisar", "Okeani"),
    ("Kit ubica (orka)", "Orca", "Sisar", "Okeani"),
    ("Delfin", "Common bottlenose dolphin", "Sisar", "Okeani"),
    ("Morž", "Walrus", "Sisar", "Arktik"),
    ("Tuljan", "Harbor seal", "Sisar", "Sjeverna mora"),
    ("Morska vidra", "Sea otter", "Sisar", "Tihi okean"),

    # Ptice
    ("Bjeloglavi orao", "Bald eagle", "Ptica", "Sjeverna Amerika"),
    ("Suri orao", "Golden eagle", "Ptica", "Evropa, Azija, Sjeverna Amerika"),
    ("Kukuvija", "Barn owl", "Ptica", "Skoro cijeli svijet"),
    ("Snježna sova", "Snowy owl", "Ptica", "Arktik"),
    ("Flamingo", "American flamingo", "Ptica", "Karibi, Južna Amerika"),
    ("Paun", "Indian peafowl", "Ptica", "Azija"),
    ("Carski pingvin", "Emperor penguin", "Ptica", "Antarktik"),
    ("Noj", "Common ostrich", "Ptica", "Afrika"),
    ("Tukan", "Toco toucan", "Ptica", "Južna Amerika"),
    ("Papagaj ara", "Scarlet macaw", "Ptica", "Srednja i Južna Amerika"),
    ("Kolibri", "Ruby-throated hummingbird", "Ptica", "Sjeverna i Srednja Amerika"),
    ("Pelikan", "Brown pelican", "Ptica", "Sjeverna i Južna Amerika"),
    ("Roda", "White stork", "Ptica", "Evropa, Afrika, Azija"),
    ("Labud", "Mute swan", "Ptica", "Evropa, Azija"),
    ("Divlja patka", "Mallard", "Ptica", "Evropa, Azija, Sjeverna Amerika"),
    ("Djetlić", "Great spotted woodpecker", "Ptica", "Evropa, Azija"),
    ("Vodomar", "Common kingfisher", "Ptica", "Evropa, Azija, Afrika"),
    ("Kokoška", "Chicken", "Ptica", "Domaća životinja"),
    ("Golub", "Rock dove", "Ptica", "Skoro cijeli svijet"),
    ("Kivi", "North Island brown kiwi", "Ptica", "Novi Zeland"),

    # Gmizavci
    ("Nilski krokodil", "Nile crocodile", "Gmizavac", "Afrika"),
    ("Aligator", "American alligator", "Gmizavac", "Sjeverna Amerika"),
    ("Morska kornjača", "Green sea turtle", "Gmizavac", "Okeani"),
    ("Galapagoska kornjača", "Galápagos tortoise", "Gmizavac", "Galapagos (Južna Amerika)"),
    ("Kraljevska kobra", "King cobra", "Gmizavac", "Azija"),
    ("Anakonda", "Green anaconda", "Gmizavac", "Južna Amerika"),
    ("Kameleon", "Veiled chameleon", "Gmizavac", "Azija (Arabija)"),
    ("Komodski varan", "Komodo dragon", "Gmizavac", "Azija (Indonezija)"),
    ("Iguana", "Green iguana", "Gmizavac", "Srednja i Južna Amerika"),
    ("Gekon", "Tokay gecko", "Gmizavac", "Azija"),

    # Vodozemci
    ("Žaba", "Common frog", "Vodozemac", "Evropa"),
    ("Crvenooka gatalinka", "Agalychnis callidryas", "Vodozemac", "Srednja Amerika"),
    ("Aksolotl", "Axolotl", "Vodozemac", "Sjeverna Amerika (Meksiko)"),
    ("Daždevnjak", "Fire salamander", "Vodozemac", "Evropa"),

    # Ribe
    ("Bijela ajkula", "Great white shark", "Riba", "Okeani"),
    ("Kitopsina", "Whale shark", "Riba", "Okeani"),
    ("Riba klaun", "Ocellaris clownfish", "Riba", "Indijski i Tihi okean"),
    ("Morski konjic", "Seahorse", "Riba", "Okeani"),
    ("Manta", "Giant oceanic manta ray", "Riba", "Okeani"),
    ("Losos", "Atlantic salmon", "Riba", "Atlantski okean i rijeke"),
    ("Zlatna ribica", "Goldfish", "Riba", "Domaća (akvarij)"),

    # Insekti
    ("Pčela", "Western honey bee", "Insekt", "Skoro cijeli svijet"),
    ("Leptir monarh", "Monarch butterfly", "Insekt", "Sjeverna Amerika"),
    ("Bubamara", "Coccinella septempunctata", "Insekt", "Evropa, Azija"),
    ("Vilin konjic", "Anax imperator", "Insekt", "Evropa, Afrika, Azija"),
    ("Šumski mrav", "Formica rufa", "Insekt", "Evropa"),
    ("Bogomoljka", "Mantis religiosa", "Insekt", "Evropa, Afrika, Azija"),
    ("Jelenak", "Lucanus cervus", "Insekt", "Evropa"),

    # Ostali beskičmenjaci
    ("Krstaš (pauk)", "Araneus diadematus", "Beskičmenjak", "Evropa, Sjeverna Amerika"),
    ("Škorpija", "Pandinus imperator", "Beskičmenjak", "Afrika"),
    ("Puž", "Cornu aspersum", "Beskičmenjak", "Evropa"),
    ("Hobotnica", "Common octopus", "Beskičmenjak", "Okeani"),
    ("Meduza", "Aurelia aurita", "Beskičmenjak", "Okeani"),
    ("Morska zvijezda", "Asterias rubens", "Beskičmenjak", "Atlantski okean"),
    ("Jastog", "European lobster", "Beskičmenjak", "Atlantski okean"),
]

# ---------------------------------------------------------------------------
# DIJELOVI TIJELA: (naziv, naslov na engleskoj Wikipediji, sistem)
# ---------------------------------------------------------------------------

TIJELO = [
    ("Srce", "Heart", "Krvožilni sistem"),
    ("Krv", "Blood", "Krvožilni sistem"),
    ("Krvni sudovi", "Blood vessel", "Krvožilni sistem"),
    ("Mozak", "Human brain", "Nervni sistem"),
    ("Kičmena moždina", "Spinal cord", "Nervni sistem"),
    ("Pluća", "Lung", "Disajni sistem"),
    ("Dušnik", "Trachea", "Disajni sistem"),
    ("Oko", "Human eye", "Čula"),
    ("Uho", "Ear", "Čula"),
    ("Nos", "Human nose", "Čula"),
    ("Jezik", "Tongue", "Čula"),
    ("Koža", "Skin", "Koža, kosa i nokti"),
    ("Kosa", "Hair", "Koža, kosa i nokti"),
    ("Nokat", "Nail (anatomy)", "Koža, kosa i nokti"),
    ("Usta", "Mouth", "Probavni sistem"),
    ("Zub", "Human tooth", "Probavni sistem"),
    ("Želudac", "Stomach", "Probavni sistem"),
    ("Jetra", "Liver", "Probavni sistem"),
    ("Tanko crijevo", "Small intestine", "Probavni sistem"),
    ("Debelo crijevo", "Large intestine", "Probavni sistem"),
    ("Bubreg", "Kidney", "Mokraćni sistem"),
    ("Mokraćni mjehur", "Urinary bladder", "Mokraćni sistem"),
    ("Kostur", "Human skeleton", "Koštani sistem"),
    ("Lobanja", "Human skull", "Koštani sistem"),
    ("Kičma", "Vertebral column", "Koštani sistem"),
    ("Grudni koš", "Rib cage", "Koštani sistem"),
    ("Butna kost", "Femur", "Koštani sistem"),
    ("Biceps", "Biceps", "Mišićni sistem"),
    ("Ahilova tetiva", "Achilles tendon", "Mišićni sistem"),
    ("Šaka", "Hand", "Dijelovi tijela"),
    ("Prst", "Finger", "Dijelovi tijela"),
    ("Stopalo", "Foot", "Dijelovi tijela"),
    ("Koljeno", "Knee", "Dijelovi tijela"),
    ("Lakat", "Elbow", "Dijelovi tijela"),
    ("Rame", "Shoulder", "Dijelovi tijela"),
    ("Obrva", "Eyebrow", "Dijelovi tijela"),
]

# ---------------------------------------------------------------------------
# MUZIČKI INSTRUMENTI: (naziv, naslov na engleskoj Wikipediji, vrsta)
# ---------------------------------------------------------------------------

INSTRUMENTI = [
    # Žičani (trzalački)
    ("Gitara", "Guitar", "Žičani"),
    ("Električna gitara", "Electric guitar", "Žičani"),
    ("Bas gitara", "Bass guitar", "Žičani"),
    ("Harfa", "Harp", "Žičani"),
    ("Bendžo", "Banjo", "Žičani"),
    ("Mandolina", "Mandolin", "Žičani"),
    ("Ukulele", "Ukulele", "Žičani"),
    ("Sitar", "Sitar", "Žičani"),
    ("Balalajka", "Balalaika", "Žičani"),
    ("Tamburica", "Tamburica", "Žičani"),
    ("Šargija", "Šargija", "Žičani"),
    ("Saz", "Bağlama", "Žičani"),
    ("Ud", "Oud", "Žičani"),
    ("Koto", "Koto (instrument)", "Žičani"),

    # Gudački
    ("Violina", "Violin", "Gudački"),
    ("Viola", "Viola", "Gudački"),
    ("Violončelo", "Cello", "Gudački"),
    ("Kontrabas", "Double bass", "Gudački"),
    ("Gusle", "Gusle", "Gudački"),
    ("Erhu", "Erhu", "Gudački"),

    # Drveni puhački
    ("Flauta", "Western concert flute", "Drveni puhački"),
    ("Blok flauta", "Recorder (musical instrument)", "Drveni puhački"),
    ("Frula", "Frula", "Drveni puhački"),
    ("Panova frula", "Pan flute", "Drveni puhački"),
    ("Klarinet", "Clarinet", "Drveni puhački"),
    ("Oboa", "Oboe", "Drveni puhački"),
    ("Fagot", "Bassoon", "Drveni puhački"),
    ("Saksofon", "Saxophone", "Drveni puhački"),
    ("Gajde", "Bagpipes", "Drveni puhački"),
    ("Didžeridu", "Didgeridoo", "Drveni puhački"),
    ("Usna harmonika", "Harmonica", "Puhački"),

    # Limeni puhački
    ("Truba", "Trumpet", "Limeni puhački"),
    ("Trombon", "Trombone", "Limeni puhački"),
    ("Horna", "French horn", "Limeni puhački"),
    ("Tuba", "Tuba", "Limeni puhački"),

    # Klavijature
    ("Klavir", "Piano", "Klavijature"),
    ("Orgulje", "Pipe organ", "Klavijature"),
    ("Harmonika", "Accordion", "Klavijature"),
    ("Čembalo", "Harpsichord", "Klavijature"),
    ("Sintisajzer", "Synthesizer", "Elektronski"),

    # Udaraljke
    ("Bubnjevi", "Drum kit", "Udaraljke"),
    ("Timpani", "Timpani", "Udaraljke"),
    ("Ksilofon", "Xylophone", "Udaraljke"),
    ("Triangl", "Triangle (musical instrument)", "Udaraljke"),
    ("Činele", "Cymbal", "Udaraljke"),
    ("Tamburin", "Tambourine", "Udaraljke"),
    ("Marakas", "Maraca", "Udaraljke"),
    ("Kastanjete", "Castanets", "Udaraljke"),
    ("Bongo bubnjevi", "Bongo drum", "Udaraljke"),
    ("Džembe", "Djembe", "Udaraljke"),
    ("Kahon", "Cajón", "Udaraljke"),
    ("Tabla", "Tabla", "Udaraljke"),
    ("Čelični bubanj", "Steelpan", "Udaraljke"),
    ("Cjevasta zvona", "Tubular bells", "Udaraljke"),
    ("Gong", "Gong", "Udaraljke"),
]

# ---------------------------------------------------------------------------
# KATEGORIJE: ime foldera -> prefiks slika, dodatna polja, spisak
# ---------------------------------------------------------------------------

KATEGORIJE = {
    "mjesta": {"prefiks": "mjesto", "polja": ["drzava", "kontinent"],
               "spisak": MJESTA, "koordinate": True},
    "zivotinje": {"prefiks": "zivotinja", "polja": ["grupa", "zivi"],
                  "spisak": ZIVOTINJE},
    "tijelo": {"prefiks": "dio_tijela", "polja": ["sistem"],
               "spisak": TIJELO},
    "instrumenti": {"prefiks": "instrument", "polja": ["vrsta"],
                    "spisak": INSTRUMENTI},
}

# Ako ti se neka slika ne sviđa, nađi bolju na commons.wikimedia.org i upiši
# ime fajla ovdje (ključ je naslov s Wikipedije iz spiska), pa pokreni
# skriptu ponovo. Primjer:
#   "Liver": "File:Human liver anatomy.svg",
ZAMJENE = {
}

# ---------------------------------------------------------------------------
# KOD
# ---------------------------------------------------------------------------


def preuzmi(url, pokusaja=5):
    """Preuzme URL, uz ponovne pokušaje ako je server zauzet."""
    for i in range(pokusaja):
        req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
        try:
            with urllib.request.urlopen(req, timeout=30) as r:
                return r.read()
        except urllib.error.HTTPError as e:
            if e.code in (429, 500, 502, 503, 504) and i < pokusaja - 1:
                try:
                    cekaj = int(e.headers.get("Retry-After") or 0)
                except ValueError:
                    cekaj = 0
                cekaj = cekaj or 5 * (i + 1)
                print(f"   server zauzet ({e.code}), čekam {cekaj} s...")
                time.sleep(cekaj)
                continue
            raise
        except urllib.error.URLError:
            if i < pokusaja - 1:
                time.sleep(5 * (i + 1))
                continue
            raise


def api(params):
    params = {**params, "format": "json", "formatversion": "2"}
    url = WIKI_API + "?" + urllib.parse.urlencode(params)
    return json.loads(preuzmi(url).decode("utf-8"))


def ocisti(tekst):
    """Ukloni HTML oznake iz teksta (autor često dolazi kao link)."""
    tekst = re.sub(r"<[^>]+>", "", tekst or "")
    return re.sub(r"\s+", " ", html.unescape(tekst)).strip()


def info_o_clanku(naslov):
    """Vrati (ime glavne slike, lat, lon) za članak na Wikipediji.
    Uzima samo slike sa slobodnom licencom."""
    d = api({
        "action": "query",
        "titles": naslov,
        "redirects": 1,
        "prop": "pageimages|coordinates",
        "piprop": "name",
        "pilicense": "free",
    })
    stranica = d["query"]["pages"][0]
    if stranica.get("missing"):
        return None, None, None
    koord = stranica.get("coordinates")
    lat, lon = (koord[0]["lat"], koord[0]["lon"]) if koord else (None, None)
    return stranica.get("pageimage"), lat, lon


def info_o_slici(datoteka):
    """Vrati URL umanjene slike, autora i licencu."""
    if not datoteka.startswith("File:"):
        datoteka = "File:" + datoteka
    d = api({
        "action": "query",
        "titles": datoteka,
        "prop": "imageinfo",
        "iiprop": "url|extmetadata",
        "iiurlwidth": SIRINA,
    })
    stranica = d["query"]["pages"][0]
    if not stranica.get("imageinfo"):
        return None
    ii = stranica["imageinfo"][0]
    meta = ii.get("extmetadata", {})

    def m(kljuc):
        return ocisti(meta.get(kljuc, {}).get("value", ""))

    return {
        "url": ii.get("thumburl") or ii["url"],
        "izvor": ii.get("descriptionurl", ""),
        "autor": m("Artist") or "nepoznat autor",
        "licenca": m("LicenseShortName") or "nepoznata licenca",
        "licenca_url": m("LicenseUrl"),
    }


def ekstenzija(url):
    ext = os.path.splitext(urllib.parse.urlparse(url).path)[1].lower()
    return ext if ext in (".jpg", ".jpeg", ".png", ".webp", ".gif") else ".jpg"


def ucitaj_postojece(putanja_json):
    """Podaci iz prethodnog pokretanja, da se ne skida sve ispočetka."""
    if not os.path.exists(putanja_json):
        return {}
    try:
        with open(putanja_json, encoding="utf-8") as f:
            return {s["wikipedia"]: s for s in json.load(f)}
    except (OSError, ValueError, KeyError):
        return {}


def sacuvaj(folder, rezultati):
    with open(os.path.join(folder, "podaci.json"), "w", encoding="utf-8") as f:
        json.dump(rezultati, f, ensure_ascii=False, indent=2)
    with open(os.path.join(folder, "zasluge.txt"), "w", encoding="utf-8") as f:
        f.write("Zasluge za fotografije (Wikimedia Commons)\n\n")
        for s in rezultati:
            f.write(f"{s['naziv']}: {s['autor']}, {s['licenca']}\n"
                    f"    {s['izvor']}\n")


def skini_kategoriju(ime, podesavanja):
    folder = ime
    folder_slike = os.path.join(folder, "slike")
    os.makedirs(folder_slike, exist_ok=True)
    putanja_json = os.path.join(folder, "podaci.json")

    postojece = ucitaj_postojece(putanja_json)
    spisak = podesavanja["spisak"]
    polja = podesavanja["polja"]
    rezultati, neuspjela = [], []

    print(f"\n=== {ime.upper()} ({len(spisak)} stavki) ===")
    try:
        for i, (naziv, naslov, *dodatno) in enumerate(spisak, 1):
            id_ = f"{podesavanja['prefiks']}_{i:03d}"
            print(f"[{i}/{len(spisak)}] {naziv}")
            dodatni_podaci = dict(zip(polja, dodatno))

            stari = postojece.get(naslov)
            if (stari and naslov not in ZAMJENE
                    and os.path.exists(stari.get("slika", ""))):
                stari.update(naziv=naziv, **dodatni_podaci)
                rezultati.append(stari)
                print("   već skinuto, preskačem")
                continue

            try:
                datoteka, lat, lon = info_o_clanku(naslov)
                datoteka = ZAMJENE.get(naslov) or datoteka
                if not datoteka:
                    raise ValueError("članak nema slobodnu glavnu sliku")

                slika = info_o_slici(datoteka)
                if not slika:
                    raise ValueError(f"nema podataka o slici {datoteka}")

                putanja = os.path.join(folder_slike,
                                       id_ + ekstenzija(slika["url"]))
                with open(putanja, "wb") as f:
                    f.write(preuzmi(slika["url"]))

                if podesavanja.get("koordinate"):
                    dodatni_podaci.update(lat=lat, lon=lon)

                rezultati.append({
                    "id": id_,
                    "naziv": naziv,
                    **dodatni_podaci,
                    "slika": putanja.replace(os.sep, "/"),
                    "autor": slika["autor"],
                    "licenca": slika["licenca"],
                    "licenca_url": slika["licenca_url"],
                    "izvor": slika["izvor"],
                    "wikipedia": naslov,
                })
                print(f"   OK -> {putanja}")
            except Exception as e:  # nastavi sa sljedećom stavkom
                print(f"   NIJE USPJELO: {e}")
                neuspjela.append((naziv, str(e)))

            time.sleep(PAUZA)
    finally:
        sacuvaj(folder, rezultati)

    print(f"Gotovo: {len(rezultati)} stavki u '{folder}/podaci.json'.")
    if podesavanja.get("koordinate"):
        bez = [s["naziv"] for s in rezultati if s.get("lat") is None]
        if bez:
            print("Bez koordinata (dodaj ih ručno u JSON):", ", ".join(bez))
    if neuspjela:
        print("Nije uspjelo:")
        for naziv, razlog in neuspjela:
            print(f"  - {naziv}: {razlog}")


def main():
    if "primjer.com" in KONTAKT:
        print("Napomena: upiši svoj email u KONTAKT na vrhu skripte "
              "(Wikimedia to traži).")

    trazene = sys.argv[1:] or list(KATEGORIJE)
    nepoznate = [k for k in trazene if k not in KATEGORIJE]
    if nepoznate:
        print("Nepoznata kategorija:", ", ".join(nepoznate))
        print("Moguće kategorije:", ", ".join(KATEGORIJE))
        return 1

    try:
        for ime in trazene:
            skini_kategoriju(ime, KATEGORIJE[ime])
    except KeyboardInterrupt:
        print("\nPrekinuto. Ono što je skinuto je sačuvano; "
              "pokreni ponovo da nastaviš.")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
