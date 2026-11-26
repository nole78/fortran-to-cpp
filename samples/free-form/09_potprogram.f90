program glavni
    implicit none
    integer :: a, b
    a = 3
    b = 4
    call zameni(a, b)
    print *, a, b
    call pozdrav
contains
    subroutine zameni(x, y)
        integer, intent(inout) :: x, y
        integer :: t
        t = x
        x = y
        y = t
    end subroutine zameni

    subroutine pozdrav
        print *, 'pozdrav iz potprograma'
    end subroutine pozdrav
end program glavni
