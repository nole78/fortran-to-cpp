#!/usr/bin/env bash
# Kompajlira main.f90 (gfortran) i main.cpp (g++), pokreće oba na istim ulazima
# i poredi izlaze token po token (razmaci se ignorišu) i izlazne kodove.
#
#   ./run.sh                          svi ulazi iz inputs/
#   ./run.sh inputs/01_tipican.txt    samo jedan
set -u
cd "$(dirname "$0")"
mkdir -p build

gfortran main.f90 -o build/fortran || exit 1
g++ -std=c++20 -Wall -Wextra -I ../../runtime main.cpp -o build/cpp || exit 1

inputs=("$@")
[ ${#inputs[@]} -eq 0 ] && inputs=(inputs/*.txt)

tokens() { tr -s ' \n' '\n\n' | sed '/^$/d'; }
failed=0

for input in "${inputs[@]}"; do
    echo "================ $input ================"
    fortran_out=$(./build/fortran < "$input" 2>build/fortran.err); fortran_code=$?
    cpp_out=$(./build/cpp < "$input" 2>build/cpp.err); cpp_code=$?

    echo "--- Fortran (izlazni kod $fortran_code)"
    echo "$fortran_out"
    [ -s build/fortran.err ] && echo "stderr: $(cat build/fortran.err)"
    echo "--- C++     (izlazni kod $cpp_code)"
    echo "$cpp_out"
    [ -s build/cpp.err ] && echo "stderr: $(cat build/cpp.err)"

    if [ "$(echo "$fortran_out" | tokens)" == "$(echo "$cpp_out" | tokens)" ] && [ "$fortran_code" == "$cpp_code" ]; then
        echo "--- ISTO ✓"
    else
        echo "--- RAZLIKA ✗"
        diff <(echo "$fortran_out" | tokens) <(echo "$cpp_out" | tokens)
        failed=1
    fi
    echo
done
exit $failed
