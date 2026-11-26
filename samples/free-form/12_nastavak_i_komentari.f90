! Komentar na pocetku
program nastavak   ! komentar na kraju reda
    implicit none
    integer :: a, b, c
    a = 1; b = 2          ! vise naredbi u jednom redu
    c = a + &
        b + &             ! komentar posle nastavka
        3
    print *, 'zbir =', c, &
             'gotovo'
end program nastavak
