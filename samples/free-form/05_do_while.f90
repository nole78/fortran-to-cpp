program test_while
    implicit none
    integer :: n, steps
    n = 27
    steps = 0
    do while (n /= 1)
        if (mod(n, 2) == 0) then
            n = n / 2
        else
            n = 3 * n + 1
        end if
        steps = steps + 1
    end do
    print *, 'koraka:', steps
end program test_while
