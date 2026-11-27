// Ručni prevod main.f90, onako kako bi ga generisao CodeGen.
// Svaki blok ima Fortran original u komentaru.
#include "f2c/runtime.hpp"

using namespace f2c;

// subroutine scale(x, factor)
//   real, intent(inout) :: x(0:4)     → referenca, jer potprogram menja niz
//   integer, intent(in) :: factor     → const referenca
void scale(Array<float, 1>& x, const int& factor)
{
    int k = 0;
    for (k = 0; k <= 4; ++k) {
        x(k) = x(k) * static_cast<float>(factor);
    }
}

// double precision function mean(x)
//   real, intent(in) :: x(0:4)
double mean(const Array<float, 1>& x)
{
    double result = 0;  // promenljiva rezultata (u Fortranu se zove isto kao funkcija)
    int k = 0;
    result = 0.0;
    for (k = 0; k <= 4; ++k) {
        result = result + static_cast<double>(x(k));
    }
    result = result / 5;
    return result;
}

int main()
{
    int n = 0, i = 0, j = 0;
    Array<float, 1> v({Bounds(0, 4)});  // real :: v(0:4)
    Array<float, 2> m(2, 3);                 // real :: m(2, 3)
    double avg = 0;
    bool ok = false;
    FixedString name(5);                     // character(len=5) :: name

    // read *, n  /  if (n <= 0) ... stop 1
    Read() >> n;
    if (n <= 0) {
        Print() << "n mora biti pozitivan";
        stop(1);
    }
    Read() >> v;
    Read() >> name;

    // m(i, j) = 10 * i + j
    for (j = 1; j <= 3; ++j) {
        for (i = 1; i <= 2; ++i) {
            m(i, j) = static_cast<float>(10 * i + j);
        }
    }
    Print() << "m:" << m;

    // call scale(v, n)  /  avg = mean(v)
    scale(v, n);
    Print() << "v * n:" << v;
    avg = mean(v);
    Print() << "prosek:" << avg;

    // 1.0 je REAL (float), 1.0d0 je DOUBLE PRECISION (double)
    Print() << "real, double:" << 1.0f / 3.0f << 1.0 / 3.0;

    // 2**10, 2**(-1), 1.5**2, 2.0**0.5
    Print() << "stepeni:" << ipow(2, 10) << ipow(2, -1)
                 << ipow(1.5f, 2) << rpow(2.0f, 0.5f);

    // ok = name == 'Ana'  (razmaci na kraju se ne računaju)
    ok = name == "Ana";
    Print() << "ime: [" << name << "] isto kao Ana: " << ok;
    name = "Aleksandra";
    Print() << "odseceno: [" << name << "]";
    Print() << "spojeno: " << concat("Fort", "ran");
}
