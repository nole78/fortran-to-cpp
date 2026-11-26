program funkcije
    implicit none
    integer :: k
    k = fakt(5)
    print *, 'fakt(5) =', k
    print *, kvadrat(1.5d0)
contains
    integer function fakt(n)
        integer, intent(in) :: n
        integer :: i
        fakt = 1
        do i = 2, n
            fakt = fakt * i
        end do
    end function fakt

    function kvadrat(x) result(r)
        real(8), intent(in) :: x
        real(8) :: r
        r = x * x
        return
    end function kvadrat
end program funkcije
