program test_if
    implicit none
    integer :: x
    x = 5
    if (x > 10) then
        print *, 'veliko'
    else if (x > 3) then
        print *, 'srednje'
    else
        print *, 'malo'
    end if
    if (x == 5) print *, 'jednako pet'
    if (x /= 5) print *, 'ovo se ne ispisuje'
end program test_if
