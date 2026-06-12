# 💰 Budget Chatbot – System Zarządzania Budżetem

Inteligentna aplikacja webowa służąca do kontroli finansów osobistych za pomocą interaktywnego czatu oraz zaawansowanych paneli analitycznych.

---

## 1. Instrukcja Uruchomienia (Dla Programisty)

### Wymagania wstępne:
* Visual Studio 2022 / .NET 8 SDK
* Serwer lokalny SQL Server (LocalDB)

### Kroki do uruchomienia lokalnego:
1. Pobierz lub sklonuj repozytorium kodu.
2. Otwórz plik rozwiązania `Budget-Chatbot.sln` w programie Visual Studio.
3. Otwórz **Package Manager Console** (Narzędzia -> NuGet Package Manager) i wpisz komendę aktualizacji bazy danych:
   ```shell
   Update-Database
