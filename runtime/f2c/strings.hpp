#ifndef F2C_STRINGS_HPP
#define F2C_STRINGS_HPP

// Fortran stringovi fiksne dužine: f2c::FixedString.
//
// CHARACTER(len=5) :: s ima UVEK tačno 5 znakova:
//   s = 'ab'         →  "ab   "  (dopunjava se razmacima)
//   s = 'abcdefg'    →  "abcde"  (višak se odseca)
//   'ab' == 'ab   '  je tačno    (kraći string se pri poređenju dopuni razmacima)
//
// Primeri prevoda:
//   character(len=5) :: s     →  f2c::FixedString s(5);
//   character :: c            →  f2c::FixedString c(1);
//   s = 'ab'                  →  s = "ab";
//   s == t, s < t, s /= 'x'   →  s == t, s < t, s != "x"   (operatori rade direktno)
//   s // t                    →  f2c::concat(s, t)
//
// Dužina se pamti u toku rada (ne kao template parametar), jer dummy argument
// character(len=*) dobija dužinu od stvarnog argumenta: character(len=*) :: s → FixedString& s.

#include <algorithm>
#include <compare>
#include <cstddef>
#include <string>

namespace f2c
{
    // Fortran poređenje: kraći string se dopuni razmacima do dužine dužeg,
    // pa se porede znak po znak. Vraća <0, 0 ili >0, kao std::strcmp.
    inline int compare(const std::string& left, const std::string& right)
    {
        const std::size_t length = std::max(left.size(), right.size());
        for (std::size_t i = 0; i < length; ++i)
        {
            const unsigned char a = i < left.size() ? static_cast<unsigned char>(left[i]) : ' ';
            const unsigned char b = i < right.size() ? static_cast<unsigned char>(right[i]) : ' ';
            if (a != b)
                return a < b ? -1 : 1;
        }
        return 0;
    }

    // s // t: rezultat je dužine len(s) + len(t), zajedno sa razmacima na kraju s.
    inline std::string concat(const std::string& left, const std::string& right)
    {
        return left + right;
    }

    class FixedString
    {
    public:
        // character(len=n) :: s  →  n razmaka
        explicit FixedString(std::size_t length) : text_(length, ' ') {}

        // character(len=n) :: s = 'abc'
        FixedString(std::size_t length, const std::string& initial) : text_(length, ' ')
        {
            assign(initial);
        }

        FixedString(const FixedString&) = default;

        // Dodela NE menja dužinu levog stringa. Podrazumevana dodela bi je menjala
        // (kopirala bi i dužinu desnog), pa je napisana ručno.
        FixedString& operator=(const FixedString& other)
        {
            assign(other.text_);
            return *this;
        }

        // Prima i literal: s = "ab" pravi std::string automatski.
        FixedString& operator=(const std::string& value)
        {
            assign(value);
            return *this;
        }

        std::size_t length() const { return text_.size(); }

        // Sadržaj sa svim razmacima na kraju, kao što ga Fortran ispisuje.
        const std::string& str() const { return text_; }

        // FixedString može da se prosledi svuda gde se traži const std::string&
        // (compare, concat, Print...).
        operator const std::string&() const { return text_; }

        // Poređenje po Fortran pravilu. Iz == i <=> C++20 sam izvodi !=, <, <=, >, >=,
        // i obrnut redosled ("ab" == s), pa ne pišemo svih 12 operatora.
        friend bool operator==(const FixedString& a, const FixedString& b) { return compare(a.text_, b.text_) == 0; }
        friend bool operator==(const FixedString& a, const std::string& b) { return compare(a.text_, b) == 0; }
        friend std::strong_ordering operator<=>(const FixedString& a, const FixedString& b) { return compare(a.text_, b.text_) <=> 0; }
        friend std::strong_ordering operator<=>(const FixedString& a, const std::string& b) { return compare(a.text_, b) <=> 0; }

    private:
        // Upiši vrednost u postojeću dužinu: dopuni razmacima ili odseci višak.
        void assign(const std::string& value)
        {
            const std::size_t length = text_.size();
            text_ = value.substr(0, length);
            text_.resize(length, ' ');
        }

        std::string text_;
    };
}

#endif
