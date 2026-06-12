# Budget Chatbot – System Zarządzania Budżetem Domowym

Inteligentna aplikacja webowa służąca do kontroli finansów osobistych, która pozwala użytkownikowi na intuicyjne wprowadzanie operacji finansowych za pomocą interaktywnego czatu oraz zaawansowaną analizę danych na dynamicznych wykresach.

---

## 1. Instrukcja Uruchomienia Lokalnego (Dla Programisty)

### Wymagania wstępne:
* Środowisko **Visual Studio 2022** z zainstalowanym pakietem `.NET Web Development`.
* Zainstalowane środowisko **.NET 8 SDK** (lub nowsze).
* Lokalna instancja serwera bazodanowego **SQL Server (LocalDB)**.

### Krok po kroku:
1. **Pobranie kodu:** Sklonuj to repozytorium na swój dysk lokalny lub pobierz paczkę ZIP.
2. **Otwarcie projektu:** Uruchom plik rozwiązania `Budget-Chatbot.sln` w programie Visual Studio.
3. **Konfiguracja Bazy Danych:** Otwórz konsolę menedżera pakietów (*Tools* -> *NuGet Package Manager* -> *Package Manager Console*) i wykonaj poniższą komendę, aby automatycznie zbudować tabele i dodać konto administratora:
   ```shell
   Update-Database
Uruchomienie: Naciśnij klawisz F5 lub kliknij przycisk Uruchom na górnym pasku Visual Studio. Aplikacja otworzy się automatycznie w przeglądarce pod adresem http://localhost:5000 (lub pokrewnym portem lokalnym).

2. Wykaz Użytych Technologii
Backend (Logika systemu): .NET 8 / C# (ASP.NET Core MVC)

Baza danych i ORM: Entity Framework Core (SQL Server / LocalDB)

Bezpieczeństwo: Microsoft.AspNetCore.Identity / PasswordHasher (Szyfrowanie haseł)

Frontend (Interfejs): Razor Views, HTML5, CSS3, Bootstrap 5, JavaScript (Fetch API)

Wizualizacja danych: Chart.js (Wykresy dynamiczne)

3. Pełna Dokumentacja Techniczna
Architektura Rozwiązania
Aplikacja została zaprojektowana zgodnie z zasadami architektury wielowarstwowej (Layered Architecture), co zapewnia wyraźne odseparowanie logiki od wyglądu strony:

Warstwa Core (Entities): Zawiera czyste definicje obiektów biznesowych (klasy reprezentujące tabele, np. użytkownika czy transakcję). Pozbawiona jest jakichkolwiek zależności zewnętrznych.

Warstwa Infrastructure (Data): Odpowiada za bezpośrednią komunikację z bazą danych SQL Server. Zawiera klasę AppDbContext, konfigurację precyzji pól finansowych, wymuszenie unikalności danych oraz skrypt automatycznego seedowania (wstrzykiwania) jednego, głównego konta administratora.

Warstwa Presentation (Web/MVC): Warstwa końcowa, odpowiadająca za obsługę żądań użytkownika. Kontrolery (Controllers) przetwarzają zapytania, komunikują się z usługami, a Widoki (Views) generują dynamiczny kod HTML wyświetlany w przeglądarce.

Struktura Bazy Danych
System opiera się na relacyjnej bazie danych. Poniżej znajduje się opis kluczowych tabel:

Users (Użytkownicy): Przechowuje dane konta (Id, Username, Email, PasswordHash). Na kolumnę Username nałożony został indeks UNIQUE, co fizycznie uniemożliwia zarejestrowanie lub dodanie innego konta o nazwie "admin". Hasło jest bezpiecznie zahaszowane kryptograficznie.

Categories (Kategorie): Słownik zawierający rodzaje operacji finansowych (Id, Name, IsExpense). Rozróżnia przychody od wydatków.

Transactions (Transakcje): Główny rejestr finansowy (Id, Amount, Description, Date, UserId, CategoryId). Pole Amount (Kwota) posiada wymuszoną precyzję dziesiętną (18, 2) dla zachowania dokładności groszowej.

RecurringTransactions: Tabela planowanych płatności cyklicznych (np. miesięczne abonamenty).

ChatHistories: Logi rozmów użytkownika z chatbotem, pozwalające zachować ciągłość kontekstu konwersacji.

Reguły powiązań (Relacje):
Relacja Jeden-do-wielu między tabelą Users a Transactions (jeden użytkownik posiada wiele wpisów finansowych). W systemie domyślnie powiązane z UserId = 1.

Relacja Jeden-do-wielu między tabelą Categories a Transactions. Zastosowano regułę DeleteBehavior.Restrict – usunięcie kategorii z systemu jest blokowane, jeśli są do niej przypisane jakiekolwiek historyczne transakcje (ochrona przed uszkodzeniem spójności danych).

4. Instrukcja Obsługi (User Manual dla Amatora)
Witaj w prostym przewodniku po systemie Budget Chatbot! Ta instrukcja wyjaśni Ci, jak w kilku prostych krokach zarządzać swoimi pieniędzmi.

Krok 1: Logowanie do Panelu Administratora
W systemie zostało przygotowane jedno, bezpieczne konto główne, na które musisz się zalogować, aby aplikacja mogła poprawnie zapisywać Twoje wydatki.

Po uruchomieniu strony kliknij odnośnik Login / Zaloguj w menu górnym.

W formularzu, który pojawi się na ekranie, wpisz następujące dane:

Nazwa użytkownika: admin

Hasło: Admin123!

Kliknij niebieski przycisk Zaloguj się.

Poniżej widok poprawnego formularza logowania:

Krok 2: Korzystanie z inteligentnego Chatbota
Główny ekran systemu to Twój osobisty asystent finansowy.

W dolnej części ekranu znajdziesz pole tekstowe z napisem "Napisz wiadomość...".

Wpisz tam naturalne zdanie, np.: Dodałem dzisiaj wydatek 45 zł na Zakupy Spożywcze.

Kliknij ikonę wysyłania lub naciśnij Enter. Chatbot automatycznie rozpozna kwotę oraz kategorię, dopisze ją do Twojego konta i odpowie potwierdzeniem w oknie rozmowy.

Poniżej wygląd okna rozmowy z asystentem:

Krok 3: Przeglądanie wykresów i filtrowanie dat
Wszystkie dane, które wpisujesz na czacie, system natychmiast zamienia w kolorowe analizy graficzne.

W menu górnym kliknij w zakładkę Zaawansowane Raporty (lub wejdź pod adres /Home/Charts).

Nad wykresami znajdziesz dwa pola kalendarza: Od (From) oraz Do (To). Kliknij w nie i wybierz zakres dni, który chcesz przeanalizować (np. obecny miesiąc).

Kliknij niebieski przycisk Generuj Wszystkie Wykresy.

System automatycznie odświeży stronę i wyświetli trzy zaawansowane wykresy:

Wykres kołowy/słupkowy kategorii: Pokazuje, na co wydajesz najwięcej (np. Jedzenie, Rozrywka).

Wykres sumaryczny (Bilans): Prezentuje ogólne zestawienie Twoich zarobków (kolor zielony) w starciu z wydatkami (kolor czerwony).

Wykres liniowy trendu: Pokazuje, w jakie dni miesiąca Twój portfel chudł najszybciej.

Poniżej przykładowe zestawienie wygenerowanych wykresów analitycznych:

Krok 4: Customizacja (Dostosowanie) treści
Kolorystyka: Wykresy posiadają wbudowaną, automatyczną logikę kolorów. Wszystkie przychody zawsze generują się w odcieniach zieleni, a wydatki w odcieniach czerwieni/pomarańczu, ułatwiając szybką ocenę sytuacji.

Kategorie: Nazwy na wykresach dopasowują się dynamicznie do bazy danych. Jeśli chatbot doda transakcję do nowej kategorii, wykres sam stworzy dla niej nową, podpisaną sekcję.
