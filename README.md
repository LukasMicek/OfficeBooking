# OfficeBooking – System rezerwacji sal konferencyjnych

[![CI](https://github.com/LukasMicek/OfficeBooking/actions/workflows/ci.yml/badge.svg)](https://github.com/LukasMicek/OfficeBooking/actions/workflows/ci.yml)

## Wymagania
- .NET 8 SDK
- Visual Studio 2022 (lub nowsze)

## Uruchomienie
1. Otwórz projekt w Visual Studio.
2. Uruchom aplikację (F5).

Baza danych działa na SQLite i tworzy się automatycznie jako plik `officebooking.db`.

## Konta i role
- Admin:
  - login: admin@local
  - hasło: Admin123!
- Zwykły użytkownik: rejestracja w aplikacji (Register)

## Funkcje
- Wyszukiwanie sal po terminie, pojemności i wyposażeniu
- Rezerwacje z walidacją konfliktów i zasad biznesowych
- „Moje rezerwacje” + edycja i usuwanie (tylko własne)
- Panel Admin:
  - CRUD sal i wyposażenia
  - przypisywanie wyposażenia do sal (checkboxy)
  - anulowanie dowolnej rezerwacji z powodem
  - raport obłożenia sal (minuty/godziny w miesiącu)
