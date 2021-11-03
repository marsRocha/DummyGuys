@echo off
set loopCount=5
:loop
start D:\Unity\Projects\BeanGuys\BuildGame_Multiplayer\BeanGuys.exe
set /a loopCount=%loopCount%-1
if %loopCount%==0 GOTO:EOF
timeout /t 1
GOTO :loop