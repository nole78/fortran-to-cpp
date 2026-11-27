# Odluke

Format: datum, šta je odlučeno, zašto, odbačene alternative.

## 2026-11-25: Ugovor između front-end-a i back-end-a (NACRT, čeka Anjin pregled)

Fajlovi: `src/Fortran2Cpp.Core/Ast/`, `Diagnostics/`, `Semantics/SemanticModel.cs`.

1. **AST je verna slika sintakse.** Ne sadrži tipove ni razrešene simbole; to je u `SemanticModel`-u.
   *Zašto:* `AstBuilder` ostaje jednostavan, a promena pravila tipova ne dira AST.
   *Odbačeno:* AST sa poljem `Type` koje se popunjava kasnije (mutabilno, lako ostane prazno).

2. **Imena su u AST-u mala slova.** Fortran ne razlikuje velika i mala slova, pa se to rešava jednom, u `AstBuilder`-u.

3. **`SemanticModel` prepoznaje čvorove po referenci** (`ReferenceEqualityComparer`).
   *Zašto:* record-i imaju vrednosnu jednakost, pa bi dva identična `i` čvora bila isti ključ.

4. **Simboli su klase, ne record-i**, i prave se samo preko `SemanticModelBuilder`-a.
   *Zašto:* imaju cikluse (promenljiva → jedinica → promenljive), na kojima bi record `Equals` i `ToString` upali u beskonačnu rekurziju.

5. **`UnitSymbol.Variables` sadrži sve promenljive jedinice**, uključujući implicitno tipizovane. CodeGen deklaracije pravi odatle, a ne iz `TypeDecl` čvorova, jer implicitne promenljive nemaju deklaraciju.

6. **`SemanticModel.GetType` postoji za svaki izraz**, i za literale i za međurezultate. CodeGen nikad sam ne zaključuje tipove.

7. **`PRINT *` i `WRITE(*,*)` su isti čvor (`PrintStmt`).** Drugi unit ili format prijavljuje se kao `Unsupported`.

8. **Nepodržane naredbe ostaju u AST-u kao `UnsupportedStmt`**, da bi dijagnostika imala tačno mesto. CodeGen se ne pokreće ako `DiagnosticBag.CanGenerate` vrati `false`.

9. **Realni literali čuvaju originalni tekst** (`RealLiteral.Text`), ne `double`, da se ne izgubi preciznost.

10. **Opsezi šifara dijagnostika:** `F2C1xxx` parsiranje, `2xxx` semantika, `3xxx` nepodržano, `4xxx` CodeGen, `5xxx` verifikacija, `9xxx` interne greške (`DiagnosticCodes.cs`).

### Otvorena pitanja (rešiti na pregledu)

- Da li `SemanticAnalyzer` umeće implicitne konverzije kao čvorove (npr. `ConvertExpr`), ili ih CodeGen ubacuje sam, poredeći `GetType` operanada? Nacrt pretpostavlja ovo drugo.
- Host association (`CONTAINS` u `PROGRAM`): kako CodeGen prevodi pristup promenljivim programa iz interne procedure? To je odluka za CodeGen, ali utiče na to šta model mora da kaže (`VariableSymbol.Owner` je dovoljan za nacrt).
- `DO` bez kontrole je `DoWhile` sa `Condition = null`. Da li je to u redu, ili treba poseban čvor?
- Da li faza 1 podržava `STOP` i `RETURN`? U AST-u su, jer su česti u korpusu.
- Obilazak AST-a: visitor ili `switch` pattern matching (nije deo ugovora, ali se odlučuje sada).

## 2026-11-27: C++ runtime za fazu 1 (`runtime/f2c/`)

Fajlovi: `array.hpp`, `strings.hpp`, `ops.hpp`, `io.hpp`, `stop.hpp`, `runtime.hpp`. Ponašanje je provereno ručno prevedenim programima protiv gfortrana 14.2 (isti tokeni u izlazu i isti izlazni kodovi).

1. **Runtime je C++20, samo headeri, bez spoljnih zavisnosti.** Generisani `CMakeLists.txt` postavlja `CMAKE_CXX_STANDARD 20`; potreban je g++ 11 ili noviji.
   *Zašto:* `requires`/concepts, `operator<=>` i `std::from_chars` skraćuju runtime (jedna klasa niza umesto dve, jedan `operator>>` za sve brojeve, poređenje stringova bez 12 operatora).
   *Odbačeno:* C++17 (probano: ~20% više koda, dve klase nizova i ručni overload-i); C++23 `std::mdspan` (nema ga u g++ 14, a indeksi su mu ionako od 0).

2. **Jedan generički niz `f2c::Array<T, Rank>`**, sa indeksima od donje granice (podrazumevano 1) i rasporedom po kolonama. Pristup je `a(i)` / `m(i, j)`, kao u Fortranu, a broj indeksa se proverava pri kompajliranju (`m(1)` na 2D nizu je greška).
   *Odbačeno:* dve klase `Array1D`/`Array2D` (više koda); `vector<vector<T>>` (memorija nije u komadu, nije column-major, a `a(i, j)` bi postao `a(j)(i)`).

3. **Elementi niza su u `std::vector`-u (heap).**
   *Zašto:* dummy nizovi (`real :: a(n)`) imaju veličinu poznatu tek u toku rada, a veliki nizovi bi prepunili stek (~8 MB).
   *Odbačeno:* C nizovi i VLA (`float a[n]` sa promenljivim `n` nije standardni C++).

4. **Element niza je umotan u `detail::Cell<T>`.**
   *Zašto:* `std::vector<bool>` je specijalan (pakuje bitove) i ne može da vrati `bool&`, pa `LOGICAL` nizovi ne bi radili.

5. **Provera granica samo u debug build-u (`#ifndef NDEBUG`)**, ekvivalent `gfortran -fcheck=bounds`. Pre `abort()` se prazni `std::cout`, da se ne izgubi izlaz. **Verifier build-uje generisani kod u Debug režimu**, da provera uhvati greške u prevodu (npr. pogrešnu donju granicu).

6. **`CHARACTER(len=n)` je `f2c::FixedString`** sa dužinom poznatom u toku rada: dodela dopunjava razmacima ili odseca, poređenje ignoriše razmake na kraju. Dužina nije template parametar, jer `character(len=*)` dummy argument dobija dužinu od stvarnog argumenta.

7. **I/O je jedan privremeni objekat po naredbi:** `f2c::Print() << a << b;` i `f2c::Read() >> x >> y;`.
   *Zašto:* destruktor `Print`-a završava liniju, a svaki `Read` počinje od nove linije, isto kao Fortran `READ`.
   *Odbačeno:* variadic `f2c::print(a, b)` iz prvog primera u README-u (README treba ažurirati).

8. **Ispis prati gfortranov format tokena**: 9 značajnih cifara za `REAL`, 17 za `DOUBLE PRECISION`, eksponent `E+XX` / `E+XXX`, `T`/`F`, bez razmaka između dva susedna stringa. Razmaci između stavki se ne poklapaju, jer ih `OutputComparer` ionako ignoriše.

9. **Greške se ponašaju kao kod gfortrana:** kraj ulaza ili loša vrednost u `READ` daju izlazni kod 2, `STOP n` ispisuje `STOP n` na stderr i vraća `n`, a celobrojni `**` se "prelije" kao gfortran (računa se u `unsigned`, bez nedefinisanog ponašanja).

10. **`**` bez `std::pow` gde god može:** `f2c::ipow` za ceo eksponent (kvadriranje i množenje), `f2c::rpow` (`std::pow`) samo za realan eksponent.

### Napomene za CodeGen

- Deklaracije inicijalizovati na nulu (`int c = 0;`): gfortran promenljive glavnog programa kreću od 0, a lokalne promenljive u C++-u su smeće.
- Niz stringova traži početnu vrednost: `f2c::Array<f2c::FixedString, 1> names({f2c::Bounds(1, 2)}, f2c::FixedString(3));`.
- Literal kao string piše se kao `"..."`; `Print` ima poseban overload za `const char*`, inače bi se literal ispisao kao `T`.
