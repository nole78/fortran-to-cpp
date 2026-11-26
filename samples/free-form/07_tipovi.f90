program tipovi
    implicit none
    integer, parameter :: n = 3
    real(8) :: x, y
    real :: r
    double precision :: d
    character(len=20) :: ime
    logical :: ok
    x = 1.5d0
    y = x * 2.0d0 + 1.0d-1
    r = 2.5
    d = 1.0d0 / 3.0d0
    ime = 'Anja'
    ok = .true.
    print *, n, x, y
    print *, r
    print *, d
    print *, ime
    print *, ok
end program tipovi
