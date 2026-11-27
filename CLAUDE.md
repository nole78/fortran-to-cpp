# CLAUDE.md

Fortran2Cpp prevodi podržani skup Fortrana u C++. Arhitektura je u [README.md](README.md), plan i pravila rada u [CONTRIBUTING.md](CONTRIBUTING.md), a tehničke odluke u [docs/odluke.md](docs/odluke.md).

## Uloga AI-ja u ovom projektu

AI u ovom repou piše **samo testove** (i pomaže oko issue-a i PR-ova). Implementaciju pišu ljudi, jer je cilj prakse da je razumeju.

- Menjaj samo fajlove u `tests/`. Ne menjaj `src/`, `runtime/`, `samples/` ni `docs/`, i ne predlaži kod implementacije.
- Ako test ne može da prođe zato što implementacija još ne postoji ili je pogrešna, ostavi test da pada i reci zašto. Ne prilagođavaj test kodu da bi prošao.
- Ako za test fali nešto u implementaciji (npr. klasa ili metoda koja ne postoji), napiši šta fali i stani, umesto da je napraviš.

## Testovi se pišu iz specifikacije, ne iz koda

Test kaže šta kod **treba** da radi. Izvori istine, ovim redom:

1. Issue na koji se test odnosi (cilj, zadaci, kriterijum prihvatanja).
2. `docs/odluke.md`: ugovor (AST, `SemanticModel`, dijagnostike) i odluke o runtime-u i CodeGen-u.
3. Ponašanje gfortrana: generisani C++ mora da da isti izlaz i izlazni kod.

Implementaciju čitaj samo da vidiš imena i potpise. Kad se kod i specifikacija ne slažu, test prati specifikaciju, a razliku prijavi.

## Konvencije

- xUnit (`[Fact]`, `[Theory]` sa `[InlineData]`/`[MemberData]` za varijante), Shouldly za provere, Verify za snapshot-ove.
- Jedna test klasa po klasi koja se testira: `ExprEmitter` → `ExprEmitterTests`.
- Folder i namespace prate `src/`: `src/Fortran2Cpp.Core/CodeGen/ExprEmitter.cs` → `tests/Fortran2Cpp.Tests/CodeGen/ExprEmitterTests.cs`, `namespace Fortran2Cpp.Tests.CodeGen;`. Testovi za `Fortran2Cpp.Verification` idu u `Verification/`.
- Ime testa: `Akcija_Uslov_OčekivaniRezultat`, na engleskom, bez dijakritika. Primeri: `CanGenerate_UnsupportedDiagnostic_ShouldBeFalse`, `EmitDoLoop_NegativeStep_ShouldCountDown`.
- Telo testa je podeljeno komentarima `// Arrange`, `// Act`, `// Assert`.
- Iznad testa koji gradi AST ide `/// <summary>` sa Fortran kodom koji taj AST predstavlja.
- Jedan test proverava jedno ponašanje. Više slučajeva istog ponašanja ide u `[Theory]`.
- Pokrij i granične slučajeve iz specifikacije (prazna petlja, negativan korak, donja granica različita od 1, prazan ulaz), ne samo srećan put.

## Vrste testova

### Unit testovi za CodeGen i Semantics

AST i `SemanticModel` se prave ručno, bez parsera. Obrazac je u [tests/Fortran2Cpp.Tests/Semantics/SemanticModelTests.cs](tests/Fortran2Cpp.Tests/Semantics/SemanticModelTests.cs):

- `SourceSpan` preko pomoćne metode `L(line)`.
- Model preko `SemanticModelBuilder`-a: `DeclareUnit`, `DeclareVariable`, `Bind`, `SetType`, `SetKind`, `SetConstant`.
- `SemanticModel` prepoznaje čvorove po referenci (ugovor #3): isti objekat čvora mora da se koristi i u AST-u i u `Bind`/`SetType`. Dva čvora sa istim sadržajem nisu isti ključ.
- Svaki izraz koji CodeGen čita mora da ima tip u modelu (ugovor #6).

### Snapshot testovi (Verify)

Za generisani C++ kod: test vraća `Verify(cppText)`.

- `.verified.txt` fajlovi se commit-uju, `.received.txt` su u `.gitignore`.
- Nikad ne prihvataj novi snapshot (preimenovanje `.received` u `.verified`) bez provere. Novi ili promenjen snapshot pomeni u izveštaju, da ga čovek pregleda.
- Snapshot ne zamenjuje proveru ponašanja: uz snapshot ide i E2E ili Shouldly provera onoga što je bitno (npr. da se `**` prevodi u `f2c::ipow`).

### Parser testovi

Preko `TestParser` ([tests/Fortran2Cpp.Tests/Parsing/TestParser.cs](tests/Fortran2Cpp.Tests/Parsing/TestParser.cs)): `ParseOk` kad ulaz mora da prođe, `Parse` sa `DiagnosticBag` kad se očekuje greška.

### E2E testovi

Kompajliraju i pokreću Fortran i C++ (`gfortran`, `g++`, CMake), pa su sporiji i traže alate.

- Označeni sa `[Trait("Category", "E2E")]`.
- Uzorci su u `samples/free-form/`, a ručno pisan par Fortran/C++ je u `demo/runtime/`.
- Za proveru da poređenje hvata grešku koristi namerno pokvaren `.cpp` kao fixture u `tests/`, ne menjaj uzorke.

### Dijagnostike

Greške se ne bacaju kao izuzeci, nego upisuju u `DiagnosticBag`. Test proverava šifru (`DiagnosticCodes`) i `Severity`, ne tekst poruke. Opsezi šifara su u ugovoru #10.

## Pokretanje

```bash
dotnet test
dotnet test --filter "Category!=E2E"
dotnet test --filter "FullyQualifiedName~CodeGen"
```

Pre kraja pokreni testove i u izveštaju navedi: koje testove si dodao, koji prolaze, koji padaju i zašto (npr. "pada jer `ExprEmitter` još ne postoji").

## PR-ovi

Opis PR-a prati [.github/pull_request_template.md](.github/pull_request_template.md): iste sekcije, istim redom, sve popunjene. `gh pr create --body` ne učitava template sam, pa ga popuni ručno.

- Kratko i konkretno: stavke, bez uvoda i ponavljanja issue-a.
- `Issue:` je `Closes #N`, da se issue zatvori pri merge-u.
- `AI:` iskreno navodi šta je generisano.
- `Verification:` navodi samo ono što je stvarno pokrenuto, i šta nije provereno.
