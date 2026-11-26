PROGRAM Mixed
    IMPLICIT NONE
    INTEGER :: Counter
    Counter = 0
    DO WHILE (Counter .LT. 3)
        Counter = Counter + 1
        PRINT *, Counter
    END DO
    IF (Counter == 3) THEN
        PRINT *, 'ok'
    ENDIF
END PROGRAM Mixed
