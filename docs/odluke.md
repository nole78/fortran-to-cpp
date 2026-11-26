# Odluke

Format: datum, šta je odlučeno, zašto, odbačene alternative.

## 2026-11-25: Ugovor između front-end-a i back-end-a

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

11. **Implicitne konverzije:** Nema čvora `ConvertExpr`. `SemanticAnalyzer` beleži tip svakog izraza, a CodeGen poredi `GetType(izraz)` sa tipom koji kontekst traži (leva strana dodele, parametar, promovisani tip operacije) i emituje `static_cast` samo kad se tipovi razlikuju. Pravila promocije su jedna funkcija `TypeRules.Promote` u Semantics, koju koriste i analizator i CodeGen.
*Zašto:* `SemanticModel` je vezan za reference čvorova (odluka 3), pa umetanje čvorova menja AST i kvari model. C++ pri mešanju tipova u aritmetici već radi isto što i Fortran, pa je cast potreban samo pri dodeli, prosleđivanju argumenata i za literale.

12. **Host association:** Faza 1 podržava samo domaćina `PROGRAM`. Promenljive glavnog programa su na nivou fajla (anonimni `namespace`), interne procedure su obične funkcije iznad `main`. `CONTAINS` u podprogramu ili funkciji prijavljuje se kao `Unsupported` (F2C3xxx). Model dobija `UnitSymbol.IsInternal => Host is not null`. Pravilo traženja imena u analizatoru: lokalno → host → implicitna tipizacija.
*Zašto:* program nije rekurzivan i njegove promenljive imaju implicitni `SAVE`, pa globalne promenljive daju istu semantiku. Izbegavaju se lambde i redosled definicija.

13. **`DO` bez kontrole:** Poseban čvor `DoInfinite`, a ne `DoWhile` sa `Condition = null`.

14. **`STOP` i `RETURN`:** Oba u fazi 1. `STOP` ide kroz `fortran::stop(...)` u runtime-u i podržava `stop`, `stop <int>` i `stop 'tekst'`. `RETURN` u podprogramu je `return;`, u funkciji `return <rezultat>;`, a u glavnom programu je semantička greška (F2C2xxx).
*Zašto:* trivijalno i često u korpusu. Verifikacija mora da poredi i kod izlaza procesa (`stop 3` vraća 3), a runtime funkcija drži to ponašanje na jednom mestu.

15. **Obilazak AST-a:** `switch` pattern matching. ANTLR visitor ostaje samo za prelazak sa parse stabla na AST
*Zašto:* sa `record`-ima je `switch` kraći od visitora, a reflection test nadoknađuje nedostatak provere potpunosti.

