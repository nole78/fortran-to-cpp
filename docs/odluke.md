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
