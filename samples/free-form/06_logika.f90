program logika
    implicit none
    logical :: p, q
    integer :: x
    x = 4
    p = .true.
    q = .false.
    print *, p .and. q
    print *, p .or. q
    print *, .not. p .and. q        ! (.not. p) .and. q
    print *, p .eqv. q
    print *, p .neqv. q
    print *, x .gt. 3 .and. x .lt. 10
    print *, x >= 4, x <= 3, x == 4, x /= 4
end program logika
