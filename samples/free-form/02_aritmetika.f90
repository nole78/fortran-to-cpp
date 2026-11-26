! Prioritet operatora, celobrojno deljenje, stepenovanje, unarni minus
program aritmetika
    implicit none
    integer :: a, b, c
    a = 7
    b = 2
    c = a / b                  ! celobrojno deljenje: 3
    print *, c
    print *, a - b - 3         ! (7-2)-3 = 2
    print *, 2 ** 3 ** 2       ! 2**(3**2) = 512
    print *, -a ** 2           ! -(7**2) = -49
    print *, -a * b            ! -(7*2) = -14
    print *, a + b * 3         ! 13
    print *, (a + b) * 3       ! 27
end program aritmetika
