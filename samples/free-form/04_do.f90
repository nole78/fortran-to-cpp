program petlje
    implicit none
    integer :: i, s
    s = 0
    do i = 1, 10
        s = s + i
    end do
    print *, 'suma 1..10 =', s

    do i = 10, 1, -3
        print *, i
    end do

    do i = 1, 100
        if (mod(i, 2) == 0) cycle
        if (i > 7) exit
        print *, 'neparan', i
    end do
    print *, 'i posle petlje =', i
end program petlje
