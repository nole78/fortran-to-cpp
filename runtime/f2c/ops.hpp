#ifndef F2C_OPS_HPP
#define F2C_OPS_HPP

// Operator ** iz Fortrana.
//
//   i ** n   (oba INTEGER)              →  f2c::ipow(i, n)    rezultat int
//   x ** n   (x REAL/DOUBLE, n INTEGER) →  f2c::ipow(x, n)    rezultat float/double
//   x ** y   (oba realna)               →  f2c::rpow(x, y)
//
// Mešane tipove (npr. REAL ** DOUBLE) CodeGen prvo svodi na zajednički tip, kao Fortran.
//
// Ponašanje je provereno sa gfortranom:
//   2 ** (-1) = 0,  (-1) ** (-3) = -1,  0 ** (-1) = 0,  2 ** 31 = -2147483648 (prelivanje)
//   2.0 ** (-2) = 0.25,  0.0 ** (-1) = Infinity

#include <cmath>
#include <concepts>

namespace f2c
{
    // INTEGER ** INTEGER
    inline int ipow(int base, int exponent)
    {
        if (exponent < 0)
        {
            // Celobrojno 1 / base^n: to je 0, osim za base = 1 i base = -1.
            if (base == 1)
                return 1;
            if (base == -1)
                return exponent % 2 == 0 ? 1 : -1;
            return 0;
        }

        // Kvadriranje i množenje: za n = 13 (binarno 1101) treba 4 koraka umesto 13 množenja.
        // Računa se u unsigned, jer je prelivanje signed int-a u C++-u nedefinisano ponašanje,
        // a gfortran se tada "prelije" (2 ** 31 = -2147483648). Unsigned daje isti rezultat.
        unsigned result = 1;
        unsigned factor = static_cast<unsigned>(base);
        for (unsigned remaining = static_cast<unsigned>(exponent); remaining > 0; remaining >>= 1)
        {
            if (remaining & 1u)
                result *= factor;
            factor *= factor;
        }
        return static_cast<int>(result);
    }

    // REAL ** INTEGER i DOUBLE PRECISION ** INTEGER (Real je float ili double).
    template <std::floating_point Real>
    Real ipow(Real base, int exponent)
    {
        // Pozitivan eksponent kao unsigned, da i -2147483648 radi bez prelivanja.
        const unsigned magnitude = exponent < 0 ? 0u - static_cast<unsigned>(exponent)
                                                : static_cast<unsigned>(exponent);
        Real result = 1;
        Real factor = base;
        for (unsigned remaining = magnitude; remaining > 0; remaining >>= 1)
        {
            if (remaining & 1u)
                result *= factor;
            factor *= factor;
        }
        // x ** (-n) = 1 / x^n; za x = 0 to je Infinity, isto kao gfortran.
        return exponent < 0 ? Real(1) / result : result;
    }

    // REAL ** REAL i DOUBLE ** DOUBLE. Za realan eksponent ne postoji način sa množenjem
    // (x ** 2.5 je exp(2.5 · ln x)), pa je std::pow ovde jedina razumna opcija.
    template <std::floating_point Real>
    Real rpow(Real base, Real exponent)
    {
        return std::pow(base, exponent);
    }
}

#endif
