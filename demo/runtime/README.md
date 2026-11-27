# Demo: C++ runtime

Mali Fortran program ([main.f90](main.f90)) i njegov ručni C++ prevod ([main.cpp](main.cpp)), napisan onako kako bi ga generisao CodeGen uz `runtime/f2c`. Skripta pokreće oba na istim ulazima i proverava da su izlazi i izlazni kodovi isti.

```bash
./run.sh
```

Treba: `gfortran` i `g++` 11 ili noviji. Binarni fajlovi idu u `build/`, koji git ignoriše.

## Šta program pokazuje

| Deo programa | Runtime |
|---|---|
| `read *, n`, `read *, v` (vrednosti preko dve linije, zarez kao razdvajač), `read *, name` | `f2c::Read` |
| `real :: v(0:4)`, donja granica 0 | `f2c::Array<float, 1>` sa `f2c::Bounds(0, 4)` |
| `real :: m(2, 3)`, `print *, m` ispisuje po kolonama | `f2c::Array<float, 2>` |
| `call scale(v, n)` menja niz, `mean(v)` ga samo čita (`INTENT(IN)`) | `Array&` i `const Array&` |
| `1.0 / 3.0` i `1.0d0 / 3.0d0`: 9 i 17 cifara, kao gfortran | `f2c::Print` |
| `2**10`, `2**(-1)` (= 0), `1.5**2`, `2.0**0.5` | `f2c::ipow`, `f2c::rpow` |
| `character(len=5)`: dopuna razmacima, odsecanje `'Aleksandra'` → `'Aleks'`, `name == 'Ana'` je tačno | `f2c::FixedString` |
| `'Fort' // 'ran'` | `f2c::concat` |
| `stop 1` za loš ulaz: `STOP 1` na stderr i izlazni kod 1 | `f2c::stop` |

## Ulazi

- `inputs/01_tipican.txt`: običan ulaz, program prolazi do kraja (izlazni kod 0).
- `inputs/02_los_n.txt`: `n = 0`, program staje sa `stop 1` (izlazni kod 1).

Razmaci u izlazu nisu isti kao kod gfortrana (gfortran poravnava kolone), pa skripta poredi samo tokene, kao i `OutputComparer`.
