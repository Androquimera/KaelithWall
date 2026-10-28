#!/bin/sh
# Compila o KaelithWall com o csc.exe que ja vem no Windows (.NET Framework 4).
# Sem SDK, sem pacote, sem dependencia.
#
# Rode pelo Bash, nao pelo PowerShell: o PowerShell 5.1 transforma a saida de
# erro de executaveis em NativeCommandError e aborta compilacao que deu certo.
set -e
cd "$(dirname "$0")"

CSC="/c/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"

# O exe nao pode ser reescrito enquanto o app esta rodando.
if tasklist //FI "IMAGENAME eq KaelithWall.exe" 2>/dev/null | grep -qi KaelithWall; then
    echo "KaelithWall esta rodando -- feche pela bandeja (Sair) antes de compilar."
    exit 1
fi

# KaelithWall.ico sai de logo.webp com o fazer_ico.py (dez tamanhos, alfa
# premultiplicado). -win32icon poe o icone no arquivo, -resource poe uma copia
# dentro do exe para o icone da bandeja sair nitido em qualquer escala de tela.
"$CSC" -nologo -target:winexe -optimize+ -platform:anycpu \
    -out:KaelithWall.exe \
    -win32icon:KaelithWall.ico \
    -resource:bandeja.ico,KaelithWall.icone \
    -reference:System.dll \
    -reference:System.Drawing.dll \
    -reference:System.Windows.Forms.dll \
    -reference:System.Core.dll \
    KaelithWall.cs

echo "Compilado: $(du -h KaelithWall.exe | cut -f1)"

echo
echo "Autoteste:"
./KaelithWall.exe --autoteste
