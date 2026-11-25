# Doprinos projektu

Ovaj dokument opisuje ko šta radi, plan po nedeljama i pravila rada. Kako alat funkcioniše i kako izgleda arhitektura opisano je u [README.md](README.md).

## Tim i podela odgovornosti

| Ko | Uloga | Delovi koda |
|---|---|---|
| **Anja** | Front-end: razumevanje Fortrana | `Grammar/`, `Preprocessing/`, `Parsing/`, `Semantics/` |
| **David** | Back-end: pravljenje i provera C++-a | `CodeGen/`, `runtime/`, `Fortran2Cpp.Verification/` (sa izveštajem) |
| **Zajedno** | Ugovor i povezivanje | `Ast/`, `Diagnostics/`, `Semantics/SemanticModel.cs`, `Transpiler.cs`, `Fortran2Cpp.Cli/`, `Fortran2Cpp.Ai/`, `samples/`, `docs/` |

Na dijagramu arhitekture u README-u: ljubičasto je Anjin deo, zeleno Davidov, a narandžasto zajednički.

Vlasnik nekog dela je onaj ko ga razvija i ko ima poslednju reč o njemu. Drugi može da menja taj kod, ali kroz PR koji vlasnik pregleda.

## Ugovor (Anja ↔ David)

Ugovor čine `Ast/`, `Diagnostics/` i `Semantics/SemanticModel.cs`. To je jedina tačka gde se delovi dodiruju:

- Anja ga popunjava (`AstBuilder` pravi AST, `SemanticAnalyzer` pravi `SemanticModel`).
- David ga čita (emiteri prave C++ iz AST-a i `SemanticModel`-a).
- Dogovara se zajedno u prva dva dana nedelje 1.
- Posle toga se menja samo kroz PR koji oboje pregledate.
- David u testovima ručno pravi AST i `SemanticModel`, pa ne čeka parser.

## Plan po nedeljama

Oznake fajlova (`[1]`, `[2]`, `[3]`) u README-u odgovaraju nedeljama.

### Nedelja 1: jedan fajl (faza 1)

Cilj: ceo korpus `samples/free-form/` prolazi end-to-end (Fortran → C++ → g++ → poređenje sa gfortranom).

| Ko | Zadaci |
|---|---|
| Zajedno (dani 1–2) | Inicijalizacija solution-a, CI, devcontainer. Ugovor: `AstNodes`, `SourceSpan`, `Diagnostic`, `DiagnosticBag`, `SemanticModel`. Korpus od 8–10 malih programa sa ulazima. `docs/analiza.md` sa podržanim skupom. |
| Anja | Integracija ANTLR-a i provera gramatike na korpusu (velika/mala slova, `&`). `SyntaxErrorListener`. `AstBuilder` za konstrukte faze 1. `SemanticAnalyzer` za jedan fajl: deklaracije, implicitni tipovi, niz ili poziv, `INTENT`. `UnsupportedDetector`. Snapshot testovi AST-a. |
| David | Runtime: `array.hpp`, `io.hpp`, `ops.hpp`. `CppWriter`, `TypeMapper`, `ExprEmitter`, `StmtEmitter`, `DeclEmitter`, `CMakeGenerator`. `CompilerRunner`, `OutputComparer`, `Verifier`, izveštaj u konzoli i JSON-u. Snapshot testovi generisanog C++-a. |
| Zajedno (kraj) | `Transpiler.cs` i `Cli` komanda `convert`. |

### Nedelja 2: više fajlova (faza 2)

Cilj: folder sa više fajlova i modula se prevodi, kompajlira i verifikuje, uz izveštaj.

| Ko | Zadaci |
|---|---|
| Anja | Učitavanje svih fajlova projekta. `IncludeResolver` uz `SourceMap`. Parsiranje `MODULE`, `USE`, `ONLY`, preimenovanja, `PUBLIC`/`PRIVATE`. Globalna tabela simbola. `ModuleGraph`: topološki redosled i detekcija ciklusa. |
| David | `HeaderGenerator`: `.hpp/.cpp` po modulu, `namespace`, `externals.hpp`. CMake za više fajlova. Verifikacija celog projekta (gfortran topološkim redom). HTML izveštaj sa statusom po fajlu, dijagnostikama i mapom Fortran → C++ linija. |
| Zajedno | Korpus `samples/multi-file/`: 3–4 projekta sa modulima i jedan sa namernim ciklusom. Komanda `verify`. |

Najveći rizik su moduli i `USE`. Oni treba da rade do sredine nedelje.

### Nedelja 3: intrinsici, AI, završetak

| Dani | Anja | David |
|---|---|---|
| 1–2 | `Intrinsics.cs`: potpisi, tipovi rezultata, generičko razrešavanje (npr. `ABS` za integer i real) | `intrinsics.hpp`, mapiranje u emiteru, testovi |
| 3–4 | `Fortran2Cpp.Ai`: Ollama klijent, promptovi, JSON predlozi (imena, komentari, objašnjenja nepodržanog) | Bezbedna primena predloga kroz tabelu simbola, ponovna verifikacija, povratak na deterministički izlaz, AI deo izveštaja |
| 5 | `docs/analiza.md`: podržani skup, ograničenja, arhitektura | README, demo skripta |

Prezentaciju spremate zajedno.

## Inicijalno postavljanje

Ovo se radi jednom, na početku:

```bash
dotnet new sln -n Fortran2Cpp   # .NET 10 može da napravi .slnx; to je u redu

dotnet new classlib -n Fortran2Cpp.Core         -o src/Fortran2Cpp.Core         -f net10.0
dotnet new classlib -n Fortran2Cpp.Verification -o src/Fortran2Cpp.Verification -f net10.0
dotnet new console  -n Fortran2Cpp.Cli          -o src/Fortran2Cpp.Cli          -f net10.0
dotnet new xunit    -n Fortran2Cpp.Tests        -o tests/Fortran2Cpp.Tests      -f net10.0
# Fortran2Cpp.Ai se pravi u nedelji 3

dotnet sln add src/Fortran2Cpp.Core src/Fortran2Cpp.Verification src/Fortran2Cpp.Cli tests/Fortran2Cpp.Tests

dotnet add src/Fortran2Cpp.Verification reference src/Fortran2Cpp.Core
dotnet add src/Fortran2Cpp.Cli reference src/Fortran2Cpp.Core src/Fortran2Cpp.Verification
dotnet add tests/Fortran2Cpp.Tests reference src/Fortran2Cpp.Core src/Fortran2Cpp.Verification

dotnet add src/Fortran2Cpp.Core package Antlr4.Runtime.Standard
dotnet add src/Fortran2Cpp.Core package Antlr4BuildTasks
dotnet add src/Fortran2Cpp.Verification package CliWrap
dotnet add src/Fortran2Cpp.Cli package System.CommandLine
dotnet add src/Fortran2Cpp.Cli package Spectre.Console
dotnet add tests/Fortran2Cpp.Tests package Verify.Xunit
dotnet add tests/Fortran2Cpp.Tests package Shouldly
```

U nedelji 3 se u `Fortran2Cpp.Ai` dodaju `Microsoft.Extensions.AI` i `OllamaSharp`.

Napomene:

- `Antlr4BuildTasks` generiše parser iz `Fortran.g4` pri build-u. U `.csproj` dodati `<Antlr4 Include="Grammar\Fortran.g4" />` i proveriti da li traži Javu.
- U `.gitignore` idu `bin/`, `obj/`, generisani ANTLR kod i `out/`.
- Oboje koristite isti devcontainer ili Docker image, da verzije `gfortran` i `g++` budu iste.
- CI: GitHub Actions na `ubuntu-latest`, uz instaliran `gfortran`.

## Pravila zavisnosti

- `Core` ne zavisi ni od jednog drugog projekta.
- `Verification` i `Ai` zavise samo od `Core`.
- `Cli` jedini spaja sve delove.
- `CodeGen` koristi samo `Ast`, `Diagnostics` i `SemanticModel`, a nikada `Parsing` ili ANTLR tipove.

## Način rada

### Grane i PR-ovi

- `main` je zaštićena grana i uvek se build-uje.
- Radi se na feature granama sa imenom autora: `anja/ast-builder`, `david/runtime-array`, `zajedno/contract`.
- Svaki PR pregleda onaj drugi.
- PR je spreman za merge kad prolaze build i `dotnet test --filter "Category!=E2E"`, a E2E testovi prolaze u CI-ju.

### Novi podržani konstrukt

1. Dodati uzorak u `samples/` (`.f90`, ulazi i `test.json`).
2. Generisati očekivane izlaze gfortranom i commit-ovati ih u `expected/`.
3. Dodati snapshot test (AST i/ili C++).
4. Ažurirati tabelu podržanog skupa u README-u i `docs/analiza.md`.

### Nepodržan konstrukt

Dodati uzorak u `samples/negative/` sa očekivanom šifrom dijagnostike. Alat nikad ne sme da generiše pogrešan kod bez upozorenja.

### Odluke

Svaka važna tehnička odluka ide u `docs/odluke.md`, uz datum, šta je odlučeno, zašto i koje su alternative odbačene. Isto važi za zaključke sa sastanaka sa mentorima.

### Konvencije koda

- AST čvorovi su nepromenljivi `record`-i i svaki ima `SourceSpan`.
- Greške se ne bacaju kao izuzeci, nego se upisuju u `DiagnosticBag`. Izuzetak je samo za greške u samom alatu.
- Šifre dijagnostika su stabilne (npr. `F2C1001`) i navode se u `docs/analiza.md`.
- Za obilazak AST-a se jednom odlučuje između visitora i `switch` pattern matching-a. Odluka ide u `odluke.md`.

### Testovi

- Framework je xUnit (`[Fact]`, `[Theory]`), uz Shouldly za provere i Verify za snapshot testove.
- Jedna test klasa po klasi koja se testira (`DiagnosticBag` → `DiagnosticBagTests`), u folderu koji prati `src/` (`Semantics/`, `CodeGen/`...).
- Ime testa ima oblik `Akcija_Uslov_OčekivaniRezultat`, na engleskom i bez dijakritika, npr. `CanGenerate_UnsupportedDiagnostic_ShouldBeFalse` ili `EmitDoLoop_NegativeStep_ShouldCountDown`.
- Telo testa je podeljeno komentarima `// Arrange`, `// Act`, `// Assert`.
