#ifndef F2C_IO_HPP
#define F2C_IO_HPP

// List-directed ulaz i izlaz: PRINT *, WRITE(*,*) i READ *.
//
// Primeri prevoda:
//   print *, x, i, 'tekst'     →  f2c::Print() << x << i << "tekst";
//   write(*,*) a               →  f2c::Print() << a;          (ceo niz, po kolonama)
//   read *, n, x               →  f2c::Read() >> n >> x;
//
// Jedna naredba je jedan privremeni objekat:
//   - Print() na kraju naredbe (u destruktoru) završava liniju sa '\n';
//   - Read() počinje čitanje od NOVE linije, kao Fortran READ. Ono što je prethodni
//     READ ostavio u svojoj liniji se preskače.
//
// Format ispisa je usklađen sa gfortranom:
//   INTEGER        42
//   REAL           0.333333343      1.00000000      100000000.      9.99900028E-02
//   DOUBLE         0.33333333333333331      1.0000000000000000E-002
//   LOGICAL        T  F
//   CHARACTER      sadržaj sa razmacima na kraju
// Razmaci između stavki NISU isti kao kod gfortrana (OutputComparer ih ignoriše),
// ali su tokeni (brojevi i reči) isti.

#include <charconv>
#include <cmath>
#include <concepts>
#include <cstddef>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <iostream>
#include <string>

#include "array.hpp"
#include "strings.hpp"

namespace f2c
{
    namespace detail
    {
        // Realan broj kao kod gfortrana: tačno `digits` značajnih cifara
        // (9 za REAL, 17 za DOUBLE PRECISION).
        //
        // Pravilo (izmereno na gfortranu 14):
        //   - ako je 0.1 <= |x| < 10^digits, ispisuje se bez eksponenta:
        //       1.00000000   0.500000000   123.456001   100000000.
        //   - inače sa eksponentom, čija je širina 2 za REAL, a 3 za DOUBLE:
        //       9.99900028E-02   1.0000000000000000E-002
        inline std::string formatReal(double value, int digits, int exponentWidth)
        {
            if (std::isnan(value))
                return "NaN";
            if (std::isinf(value))
                return value > 0 ? "Infinity" : "-Infinity";

            // Eksponent posle zaokruživanja na `digits` cifara (9.9999999999 postaje 1.0E+01),
            // pa ga ne računamo preko log10, nego pustimo printf da zaokruži.
            char scientific[64];
            std::snprintf(scientific, sizeof scientific, "%.*e", digits - 1, value);
            char* e = std::strchr(scientific, 'e');
            const int exponent = value == 0.0 ? 0 : std::atoi(e + 1);

            if (exponent >= -1 && exponent < digits)
            {
                // Bez eksponenta: decimala ima koliko preostane do `digits` značajnih cifara.
                const int decimals = digits - 1 - exponent;
                char fixed[64];
                std::snprintf(fixed, sizeof fixed, "%.*f", decimals, value);
                return decimals == 0 ? std::string(fixed) + '.' : fixed; // gfortran: "100000000."
            }

            // Sa eksponentom: mantisa iz printf-a, a eksponent prepisan na Fortran način.
            char exponentText[16];
            std::snprintf(exponentText, sizeof exponentText, "E%c%0*d",
                          exponent < 0 ? '-' : '+', exponentWidth, std::abs(exponent));
            return std::string(scientific, e) + exponentText;
        }

        // Greška u ulazu (kraj ulaza, slovo umesto broja...). gfortran tada završava
        // sa izlaznim kodom 2, pa i mi; Verifier poredi izlazne kodove.
        [[noreturn]] inline void inputError(const std::string& message)
        {
            std::cout.flush();
            std::fprintf(stderr, "f2c: greška pri čitanju: %s\n", message.c_str());
            std::exit(2);
        }
    }

    // -------------------------------------------------------------------------
    // PRINT * / WRITE(*,*)
    // -------------------------------------------------------------------------

    class Print
    {
    public:
        Print() = default;
        Print(const Print&) = delete;            // kopija bi dvaput ispisala kraj linije
        Print& operator=(const Print&) = delete;

        // Kraj naredbe PRINT: kraj linije.
        ~Print() { std::cout << '\n'; }

        Print& operator<<(int value) { return number(std::to_string(value)); }
        Print& operator<<(long value) { return number(std::to_string(value)); }
        Print& operator<<(float value) { return number(detail::formatReal(value, 9, 2)); }
        Print& operator<<(double value) { return number(detail::formatReal(value, 17, 3)); }
        Print& operator<<(bool value) { return number(value ? "T" : "F"); }

        // Literal mora imati svoj overload: bez njega bi se "tekst" (const char*)
        // pretvorio u bool i ispisao kao T, jer je ta konverzija jača od one u std::string.
        Print& operator<<(const char* value) { return text(value); }
        // Važi i za FixedString, koji se sam pretvara u const std::string&.
        Print& operator<<(const std::string& value) { return text(value); }

        // Ceo niz: svaki element kao posebna stavka, redom iz memorije (po kolonama).
        template <typename T, int Rank>
        Print& operator<<(const Array<T, Rank>& array)
        {
            for (std::size_t k = 0; k < array.size(); ++k)
                *this << array.element(k);
            return *this;
        }

    private:
        // gfortran stavlja razmak ispred svake stavke, OSIM između dva stringa:
        //   print *, 'a', 'b', 1, 'c'   →   " ab 1 c"
        Print& number(const std::string& value)
        {
            std::cout << ' ' << value;
            previousWasText_ = false;
            return *this;
        }

        Print& text(const std::string& value)
        {
            std::cout << (previousWasText_ ? "" : " ") << value;
            previousWasText_ = true;
            return *this;
        }

        bool previousWasText_ = false;
    };

    // -------------------------------------------------------------------------
    // READ *
    // -------------------------------------------------------------------------
    //
    // Pravila list-directed čitanja (provereno na gfortranu):
    //   - vrednosti su razdvojene razmacima i/ili jednim zarezom: "1 2", "1,2", "1 , 2";
    //   - ako u liniji nema dovoljno vrednosti, čita se sledeća linija;
    //   - svaki READ počinje od nove linije (ostatak prethodne se preskače);
    //   - dva zareza zaredom ("1,,3") znače "preskoči": promenljiva ostaje nepromenjena;
    //   - '/' završava READ: preostale promenljive ostaju nepromenjene;
    //   - realni brojevi mogu imati 'd' eksponent: 2.5d1;
    //   - LOGICAL: T, F, .true., .false. (bitno je samo prvo slovo posle tačke);
    //   - string: 'pod navodnicima' (može sadržati razmake) ili jedna reč bez navodnika.

    class Read
    {
    public:
        Read() = default;
        Read(const Read&) = delete;
        Read& operator=(const Read&) = delete;

        // INTEGER, REAL i DOUBLE PRECISION. bool je u C++-u takođe "integral", pa je
        // izuzet i ima svoj overload ispod.
        template <typename Number>
            requires((std::integral<Number> || std::floating_point<Number>) && !std::same_as<Number, bool>)
        Read& operator>>(Number& target)
        {
            std::string value;
            if (!nextValue(value))
                return *this;

            // Fortran dozvoljava 2.5d1 i vodeći '+'; std::from_chars ne razume ni jedno.
            std::string normalized = value;
            for (char& c : normalized)
                if (c == 'd' || c == 'D')
                    c = 'e';
            const char* begin = normalized.data();
            const char* end = begin + normalized.size();
            if (begin != end && *begin == '+')
                ++begin;

            const auto [stop, error] = std::from_chars(begin, end, target);
            if (normalized.empty() || error != std::errc() || stop != end)
                badValue(value, std::integral<Number> ? "INTEGER" : "REAL");
            return *this;
        }

        Read& operator>>(bool& target)
        {
            std::string value;
            if (!nextValue(value))
                return *this;

            // ".true." → 't', "T" → 't'
            const std::size_t first = value.starts_with('.') ? 1 : 0;
            const char letter = first < value.size() ? value[first] : '\0';
            if (letter == 't' || letter == 'T')
                target = true;
            else if (letter == 'f' || letter == 'F')
                target = false;
            else
                badValue(value, "LOGICAL");
            return *this;
        }

        // Dodela u FixedString sama dopunjava ili odseca na dužinu promenljive.
        Read& operator>>(FixedString& target)
        {
            std::string value;
            if (nextValue(value))
                target = value;
            return *this;
        }

        // read *, a: čita sve elemente redom iz memorije (po kolonama).
        template <typename T, int Rank>
        Read& operator>>(Array<T, Rank>& array)
        {
            for (std::size_t k = 0; k < array.size(); ++k)
                *this >> array.element(k);
            return *this;
        }

    private:
        static bool isBlank(char c) { return c == ' ' || c == '\t' || c == '\r'; }
        bool atEndOfLine() const { return position_ >= line_.size(); }
        char current() const { return line_[position_]; }

        // Pronađi sledeću vrednost i vrati njen tekst.
        // Vraća false ako promenljivu treba ostaviti nepromenjenu ("1,,3" ili posle '/').
        bool nextValue(std::string& value)
        {
            ++itemNumber_;
            if (terminated_)
                return false;

            // Preskoči razmake; kad se linija završi, uzmi sledeću.
            while (true)
            {
                while (lineLoaded_ && !atEndOfLine() && isBlank(current()))
                    ++position_;
                if (lineLoaded_ && !atEndOfLine())
                    break;
                if (!std::getline(std::cin, line_))
                    detail::inputError("kraj ulaza pre stavke " + std::to_string(itemNumber_));
                lineLoaded_ = true;
                position_ = 0;
            }

            if (current() == ',') // zarez odmah na mestu vrednosti: promenljiva se ne menja
            {
                ++position_;
                return false;
            }
            if (current() == '/')
            {
                terminated_ = true;
                return false;
            }

            value = (current() == '\'' || current() == '"') ? quotedString() : plainToken();

            // Posle vrednosti: preskoči razmake i najviše jedan zarez (separator).
            while (!atEndOfLine() && isBlank(current()))
                ++position_;
            if (!atEndOfLine() && current() == ',')
                ++position_;
            return true;
        }

        // 'it''s'  →  it's   (udvojen navodnik je jedan navodnik)
        std::string quotedString()
        {
            const char quote = line_[position_++];
            std::string value;
            while (!atEndOfLine())
            {
                const char c = line_[position_++];
                if (c != quote)
                    value += c;
                else if (!atEndOfLine() && current() == quote)
                    value += line_[position_++];
                else
                    break; // zatvoren navodnik
            }
            return value;
        }

        // Sve do razmaka, zareza, '/' ili kraja linije.
        std::string plainToken()
        {
            const std::size_t start = position_;
            while (!atEndOfLine() && !isBlank(current()) && current() != ',' && current() != '/')
                ++position_;
            return line_.substr(start, position_ - start);
        }

        [[noreturn]] void badValue(const std::string& value, const char* expectedType)
        {
            detail::inputError("'" + value + "' nije ispravan " + expectedType +
                               " (stavka " + std::to_string(itemNumber_) + ")");
        }

        std::string line_;          // trenutna linija ulaza
        std::size_t position_ = 0;  // dokle smo stigli u liniji
        bool lineLoaded_ = false;   // da li je ovaj READ već uzeo neku liniju
        bool terminated_ = false;   // naišli smo na '/'
        int itemNumber_ = 0;        // redni broj stavke, za poruke o greškama
    };
}

#endif
