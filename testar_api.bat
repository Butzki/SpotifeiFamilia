@echo off
REM Sobe a SpotifeiFamilia.WebApi (se ainda nao estiver rodando) e testa
REM todos os endpoints em sequencia, usando credenciais ja existentes no
REM banco local (usuario "Pai", conta responsavel, id 8).
REM
REM Requer: dotnet SDK, curl (ja vem no Windows 10/11), MySQL local com o
REM schema/dados de bd.sql aplicados.

title SpotifeiFamilia API - Teste Automatizado
color 0B
setlocal enabledelayedexpansion
set "STEPNUM=0"

set "BASE_URL=https://localhost:7288"
set "WEBAPI_DIR=%~dp0SpotifeiFamilia.WebApi"
set "USUARIO_ID=8"
set "EMAIL=pai@gmail.com"
set "SENHA=paisenha"
set "PLAYLIST_NOME=Playlist de Teste %TIME:~6,2%%TIME:~9,2%%RANDOM%"

cls
echo.
echo    ############################################################
echo    #                                                          #
echo    #             S P O T I F E I   F A M I L I A              #
echo    #                Teste automatizado da API                 #
echo    #                                                          #
echo    ############################################################
echo.

where curl >nul 2>nul
if errorlevel 1 (
    echo    [ERRO] curl nao encontrado no PATH.
    goto FIM
)

call :header "Verificando se a API ja esta no ar"
curl -s -k -o nul -w "%%{http_code}" %BASE_URL%/api/health > "%TEMP%\health_status.txt" 2>nul
set /p HEALTH_STATUS=<"%TEMP%\health_status.txt"

if "%HEALTH_STATUS%"=="200" (
    echo    -^> API ja estava no ar em %BASE_URL%
) else (
    echo    -^> Subindo a API em uma nova janela...
    start "SpotifeiFamilia.WebApi" cmd /k "cd /d %WEBAPI_DIR% && dotnet run --launch-profile https"

    echo    -^> Aguardando %BASE_URL%/api/health responder...
    :WAIT_LOOP
    timeout /t 2 /nobreak >nul
    curl -s -k -o nul -w "%%{http_code}" %BASE_URL%/api/health > "%TEMP%\health_status.txt" 2>nul
    set /p HEALTH_STATUS=<"%TEMP%\health_status.txt"
    if not "!HEALTH_STATUS!"=="200" goto WAIT_LOOP
    echo    -^> API no ar
)

call :header "GET /api/health"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" %BASE_URL%/api/health

call :header "POST /api/autenticacao/login  (%EMAIL%)"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" -X POST %BASE_URL%/api/autenticacao/login ^
    -H "Content-Type: application/json" ^
    -d "{\"email\":\"%EMAIL%\",\"senha\":\"%SENHA%\"}"

curl -s -k -X POST %BASE_URL%/api/autenticacao/login -H "Content-Type: application/json" -d "{\"email\":\"%EMAIL%\",\"senha\":\"%SENHA%\"}" > "%TEMP%\login_response.json"
powershell -NoProfile -Command "(Get-Content -Raw '%TEMP%\login_response.json' | ConvertFrom-Json).token" > "%TEMP%\jwt_token.txt"
set /p JWT_TOKEN=<"%TEMP%\jwt_token.txt"

if not defined JWT_TOKEN (
    echo    [ERRO] Nao foi possivel obter o token JWT. Abortando os proximos passos.
    goto FIM
)
echo    -^> Token JWT obtido, sera usado como Authorization Bearer daqui pra frente.

call :header "GET /api/musicas/%USUARIO_ID%"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" -H "Authorization: Bearer %JWT_TOKEN%" %BASE_URL%/api/musicas/%USUARIO_ID%

call :header "GET /api/musicas/buscar  (usuarioId=%USUARIO_ID%, termo=Chamas)"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" -H "Authorization: Bearer %JWT_TOKEN%" "%BASE_URL%/api/musicas/buscar?usuarioId=%USUARIO_ID%&termo=Chamas"

call :header "GET /api/familia/%USUARIO_ID%/dependentes"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" -H "Authorization: Bearer %JWT_TOKEN%" %BASE_URL%/api/familia/%USUARIO_ID%/dependentes

call :header "GET /api/playlists/%USUARIO_ID%"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" -H "Authorization: Bearer %JWT_TOKEN%" %BASE_URL%/api/playlists/%USUARIO_ID%

call :header "POST /api/playlists  (cria %PLAYLIST_NOME%)"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" -X POST %BASE_URL%/api/playlists ^
    -H "Authorization: Bearer %JWT_TOKEN%" ^
    -H "Content-Type: application/json" ^
    -d "{\"usuarioId\":%USUARIO_ID%,\"nome\":\"%PLAYLIST_NOME%\"}"

call :header "POST /api/musicas/reproduzir  (musicaId=1)"
curl -s -k -w "\n   [HTTP %%{http_code}]\n" -X POST %BASE_URL%/api/musicas/reproduzir ^
    -H "Authorization: Bearer %JWT_TOKEN%" ^
    -H "Content-Type: application/json" ^
    -d "{\"usuarioId\":%USUARIO_ID%,\"musicaId\":1}"

echo.
echo    ############################################################
echo    #                    Testes concluidos                     #
echo    ############################################################
echo.

:FIM
pause
exit /b 0

:header
set /a STEPNUM+=1
echo.
echo    ------------------------------------------------------------
echo     [!STEPNUM!] %~1
echo    ------------------------------------------------------------
goto :eof
