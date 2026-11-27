#ifndef F2C_STOP_HPP
#define F2C_STOP_HPP

// Naredba STOP. Ponašanje je provereno sa gfortranom:
//
//   stop            →  f2c::stop();            ništa ne ispisuje, izlazni kod 0
//   stop 3          →  f2c::stop(3);           "STOP 3" na stderr, izlazni kod 3
//   stop 'poruka'   →  f2c::stop("poruka");    "STOP poruka" na stderr, izlazni kod 0
//
// Verifier poredi izlazne kodove, pa ovo mora da se poklapa.

#include <cstdio>
#include <cstdlib>
#include <iostream>
#include <string>

namespace f2c
{
    // [[noreturn]]: kompajler zna da se funkcija nikad ne vraća (npr. ne traži return posle nje).
    [[noreturn]] inline void stop()
    {
        std::cout.flush();
        std::exit(0);
    }

    [[noreturn]] inline void stop(int code)
    {
        std::cout.flush();
        std::fprintf(stderr, "STOP %d\n", code);
        std::exit(code);
    }

    // Prima i literal: stop("poruka") pravi std::string automatski.
    [[noreturn]] inline void stop(const std::string& message)
    {
        std::cout.flush();
        std::fprintf(stderr, "STOP %s\n", message.c_str());
        std::exit(0);
    }
}

#endif
