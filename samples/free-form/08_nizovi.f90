program nizovi
    implicit none
    integer :: i
    integer, dimension(5) :: v
    real(8) :: a(0:2)
    real(8) :: m(2, 3)
    integer :: j
    do i = 1, 5
        v(i) = i * i
    end do
    print *, v
    print *, v(2:4)
    a(0) = 1.0d0
    a(1) = 2.0d0
    a(2) = a(0) + a(1)
    print *, a(2)
    do j = 1, 3
        do i = 1, 2
            m(i, j) = i + 10 * j
        end do
    end do
    print *, m(2, 3)
end program nizovi
