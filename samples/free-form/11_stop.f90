program test_stop
    implicit none
    integer :: x
    x = -1
    if (x < 0) then
        print *, 'negativno, prekid'
        stop 2
    end if
    print *, 'ovo se ne ispisuje'
end program test_stop
