! Demo runtime-a: mali program koji prolazi kroz sve što runtime podržava u fazi 1.
! Ručni C++ prevod je u main.cpp; run.sh pokreće oba i poredi izlaze.
program demo
  implicit none
  integer :: n, i, j
  real :: v(0:4)                  ! niz sa donjom granicom 0
  real :: m(2, 3)                 ! 2D niz, u memoriji po kolonama
  double precision :: avg
  logical :: ok
  character(len=5) :: name        ! string fiksne dužine

  ! READ: svaki READ kreće od nove linije, a vrednosti mogu da idu preko više linija
  read *, n
  if (n <= 0) then
    print *, 'n mora biti pozitivan'
    stop 1                        ! izlazni kod 1
  end if
  read *, v
  read *, name

  ! 2D niz: ispis ide po kolonama
  do j = 1, 3
    do i = 1, 2
      m(i, j) = 10 * i + j
    end do
  end do
  print *, 'm:', m

  ! Potprogram menja niz preko reference, funkcija ga samo čita
  call scale(v, n)
  print *, 'v * n:', v
  avg = mean(v)
  print *, 'prosek:', avg

  ! REAL ima ~7 cifara, DOUBLE PRECISION ~16
  print *, 'real, double:', 1.0 / 3.0, 1.0d0 / 3.0d0

  ! Operator **
  print *, 'stepeni:', 2**10, 2**(-1), 1.5**2, 2.0**0.5

  ! Stringovi: dopuna razmacima, odsecanje, poređenje bez razmaka na kraju, spajanje
  ok = name == 'Ana'
  print *, 'ime: [', name, '] isto kao Ana: ', ok
  name = 'Aleksandra'
  print *, 'odseceno: [', name, ']'
  print *, 'spojeno: ', 'Fort' // 'ran'

contains

  subroutine scale(x, factor)
    real, intent(inout) :: x(0:4)
    integer, intent(in) :: factor
    integer :: k
    do k = 0, 4
      x(k) = x(k) * factor
    end do
  end subroutine scale

  double precision function mean(x)
    real, intent(in) :: x(0:4)
    integer :: k
    mean = 0.0d0
    do k = 0, 4
      mean = mean + x(k)
    end do
    mean = mean / 5
  end function mean

end program demo
