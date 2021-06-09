@echo off
set /P server=Server: 
set /P database=Database: 
set /P query=Query: 
set /P file=File: 
sqlcmd -S %server% -d %database% -E -Q "%query%" -o "%file%" -s"|" -W
pause