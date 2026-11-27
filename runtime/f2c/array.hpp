#ifndef F2C_ARRAY_HPP
#define F2C_ARRAY_HPP

// f2c::Array<T, Rank>: Fortran niz u C++-u.
//
// Ponaša se kao Fortran niz:
//   - indeksi kreću od donje granice (podrazumevano 1), a ne od 0;
//   - elementi su u memoriji poređani po kolonama (column-major):
//     za m(2,3) redosled je m(1,1), m(2,1), m(1,2), m(2,2), m(1,3), m(2,3);
//   - elementu se pristupa sa a(i) ili m(i, j), kao u Fortranu.
//
// Primeri prevoda:
//   real :: a(5)                  →  f2c::Array<float, 1> a(5);
//   real :: m(2, 3)               →  f2c::Array<float, 2> m(2, 3);
//   real :: z(0:9)                →  f2c::Array<float, 1> z({f2c::Bounds(0, 9)});
//   integer :: g(0:4, -1:1)       →  f2c::Array<int, 2> g({f2c::Bounds(0, 4), f2c::Bounds(-1, 1)});
//   character(len=3) :: names(2)  →  f2c::Array<f2c::FixedString, 1> names({f2c::Bounds(1, 2)}, f2c::FixedString(3));
//
// Elementi su u std::vector-u (heap), jer dummy nizovi (real :: a(n)) imaju veličinu
// poznatu tek u toku rada, a veliki nizovi bi prepunili stek. Vidi docs/odluke.md.
//
// Provera granica radi samo u debug build-u, kad NDEBUG nije definisan.
// To je ekvivalent "gfortran -fcheck=bounds".

#include <array>
#include <concepts>
#include <cstddef>
#include <cstdio>
#include <cstdlib>
#include <iostream>
#include <vector>

namespace f2c
{
    // Granice jedne dimenzije, kao Fortranski zapis "lower:upper", npr. 0:9.
    struct Bounds
    {
        long lower;
        long upper;

        Bounds(long lowerBound, long upperBound) : lower(lowerBound), upper(upperBound) {}

        // Broj elemenata. Fortran dozvoljava prazan niz (npr. 5:4), pa je najmanje 0;
        // negativan broj bi kao veličina vektora postao ogroman size_t.
        long length() const { return upper >= lower ? upper - lower + 1 : 0; }
    };

    namespace detail
    {
        // Omotač oko jednog elementa. Postoji samo zbog std::vector<bool>: on je u C++-u
        // specijalan (pakuje bitove) i ne može da vrati bool&, pa LOGICAL nizovi ne bi radili.
        // std::vector<Cell<bool>> je običan vektor, pa je cell.value pravi bool&.
        template <typename T>
        struct Cell
        {
            T value;
        };
    }

    template <typename T, int Rank>
    class Array
    {
        static_assert(Rank >= 1, "niz mora imati bar jednu dimenziju");

    public:
        // Samo dužine, donje granice su 1:  Array<float, 2> m(2, 3)  ↔  real :: m(2, 3)
        // explicit: bez njega bi "Array<float, 1> a = 5;" ili poziv f(7) tiho napravili niz.
        template <std::integral... Length>
            requires(sizeof...(Length) == Rank)
        explicit Array(Length... lengths)
            : Array({Bounds(1, static_cast<long>(lengths))...})
        {
        }

        // Granice svake dimenzije i, po želji, početna vrednost elemenata.
        // Početna vrednost je obavezna za nizove stringova, jer svaki element mora da zna
        // svoju dužinu; za brojeve je podrazumevano 0, a za LOGICAL false.
        explicit Array(const std::array<Bounds, Rank>& bounds, const T& initial = T())
            : bounds_(bounds), elements_(countElements(bounds), detail::Cell<T>{initial})
        {
        }

        // Pristup elementu: a(i), m(i, j)... Prima tačno Rank celih brojeva.
        // Verzija bez const je za upis (a(i) = 5), a const za čitanje iz nizova
        // koji se ne menjaju (INTENT(IN) argumenti su const Array&).
        template <std::integral... Index>
            requires(sizeof...(Index) == Rank)
        T& operator()(Index... indices)
        {
            return elements_[position({static_cast<long>(indices)...})].value;
        }

        template <std::integral... Index>
            requires(sizeof...(Index) == Rank)
        const T& operator()(Index... indices) const
        {
            return elements_[position({static_cast<long>(indices)...})].value;
        }

        // Granice; dimension kreće od 1, kao u Fortranu (LBOUND(a, 1), UBOUND(m, 2)).
        long lowerBound(int dimension) const { return bounds_[dimension - 1].lower; }
        long upperBound(int dimension) const { return bounds_[dimension - 1].upper; }

        // Ukupan broj elemenata, kao SIZE(a).
        std::size_t size() const { return elements_.size(); }

        // a = 0  (ista vrednost u sve elemente)
        void fill(const T& value)
        {
            for (auto& cell : elements_)
                cell.value = value;
        }

        // k-ti element redom iz memorije (po kolonama), k od 0. Za "print *, a" i "read *, a".
        T& element(std::size_t k) { return elements_[k].value; }
        const T& element(std::size_t k) const { return elements_[k].value; }

    private:
        static std::size_t countElements(const std::array<Bounds, Rank>& bounds)
        {
            std::size_t count = 1;
            for (const Bounds& b : bounds)
                count *= static_cast<std::size_t>(b.length());
            return count;
        }

        // Pretvara Fortran indekse u poziciju u elements_.
        //
        // Po kolonama: prva dimenzija se menja najbrže. Korak (stride) dimenzije je
        // koliko elemenata se preskoči kad se njen indeks poveća za 1:
        //   dimenzija 1: korak 1
        //   dimenzija 2: korak = dužina dimenzije 1
        //   dimenzija 3: korak = dužina dim. 1 · dužina dim. 2 ...
        //
        // Primer, m(2, 3) i element m(2, 2):  (2-1)·1 + (2-1)·2 = 3
        std::size_t position(const std::array<long, Rank>& indices) const
        {
            std::size_t pos = 0;
            std::size_t stride = 1;
            for (int dim = 0; dim < Rank; ++dim)
            {
                checkIndex(dim, indices[dim]);
                pos += static_cast<std::size_t>(indices[dim] - bounds_[dim].lower) * stride;
                stride *= static_cast<std::size_t>(bounds_[dim].length());
            }
            return pos;
        }

        // U release build-u (-DNDEBUG) telo je prazno, pa provera ne košta ništa.
        void checkIndex([[maybe_unused]] int dim, [[maybe_unused]] long index) const
        {
        #ifndef NDEBUG
            const Bounds& b = bounds_[dim];
            if (index < b.lower || index > b.upper)
            {
                // Prvo ispiši ono što je program već napisao, inače se izgubi pri abort-u.
                std::cout.flush();
                std::fprintf(stderr, "f2c: indeks %ld van granica [%ld:%ld] u dimenziji %d\n",
                             index, b.lower, b.upper, dim + 1);
                std::abort();
            }
        #endif
        }

        std::array<Bounds, Rank> bounds_;          // granice svake dimenzije
        std::vector<detail::Cell<T>> elements_;    // svi elementi, po kolonama
    };
}

#endif
