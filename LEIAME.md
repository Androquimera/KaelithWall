# KaelithWall

Trocador de papel de parede leve, para substituir o John's Background Switcher.

## O que ele faz

Lê uma pasta de imagens e vai trocando o papel de parede. Pelo menu do ícone na
bandeja:

- **Próximo** / **Anterior** (clique duplo no ícone também avança)
- **Pausar** / **Retomar**
- **Abrir local da imagem** — abre a pasta no Explorer com o arquivo atual já selecionado
- **Ordem aleatória** ou **ordem em sequência**
- **Intervalo** — 10 s, 30 s, 1 min, 5 min, 10 min, 30 min, 1 h
- **Ajuste na tela** — preencher, ajustar, esticar, centralizar
- **Escolher pasta**, **incluir subpastas**, **recarregar pasta**

O item de cima do menu mostra o nome da imagem atual e a posição no ciclo
(`54.jpg   (38/185)`).

## Por que ele é leve

Medido em 27/10/2026, trocando a cada 10 segundos, na mesma pasta de 185
imagens (1,1 GB):

| | processo próprio | explorer.exe | **total de CPU** | RAM do processo |
|---|---|---|---|---|
| Background Switcher | 35,33% | 4,27% | **39,60%** | 56,5 MB |
| KaelithWall | 0,02% | 7,66% | **7,67%** | 25,7 MB |

(nada trocando, o `explorer` sozinho já gasta 0,59%)

**5,2× menos CPU e menos da metade da RAM.** O motivo é um só: o KaelithWall
nunca toca em pixel nenhum. Ele entrega o caminho do arquivo original ao
Windows (`SystemParametersInfo`/`SPI_SETDESKWALLPAPER`) e quem decodifica,
escala e desenha é o próprio Windows, no processo do `explorer`, com o código
otimizado que ele já usa e com cache.

O Background Switcher fazia o contrário: decodificava o JPEG (os seus chegam a
83 MB), redimensionava, compunha e gravava um BMP de 16,6 MB em
`AppData\Local` a cada troca, além de animar uma transição de 33 passos e
repintar o texto dos ícones da área de trabalho a cada 2 minutos — esse último
gastava CPU até sem trocar wallpaper.

### O custo que sobrou

A troca em si é feita fora da linha principal do app, então o menu da bandeja
nunca congela. Medido: `SPI_SETDESKWALLPAPER` leva **1.314 ms** no arquivo mais
pesado da pasta (`64.png`, 83,4 MB) e **menos de 100 ms** nos JPEGs comuns. Esse
tempo é do `explorer`, não do KaelithWall, e aparece como o papel de parede
levando um instante a mais para surgir. Nada foi reduzido de qualidade para
isso: a imagem entregue é o arquivo original.

## Onde ficam as coisas

- Fonte: `KaelithWall.cs` (arquivo único)
- Configuração: `%APPDATA%\KaelithWall\config.txt` (texto simples, chave=valor)
- Relatório do autoteste: `autoteste.txt`, ao lado do exe
- Instalação: `C:\Users\user\Desktop\trabalhos\SOFTWARES\KaelithWall`
  (no SSD, para não arrastar o logon), com atalho na pasta Inicializar e na
  área de trabalho

## O ícone

`logo.webp` é a arte original. O `fazer_ico.py` gera dois arquivos a partir
dela:

- `KaelithWall.ico` — dez tamanhos (16 a 256), vai no exe como ícone de arquivo
- `bandeja.ico` — só 16 a 48, embutido como recurso para o ícone da bandeja

Dois cuidados que valem na hora de mexer nisso:

- **Alfa premultiplicado.** Reduzir RGBA direto mistura o preto invisível do
  fundo transparente na média e deixa franja escura em volta das molduras
  brancas. O script reduz no modo `RGBa` do Pillow, que é premultiplicado.
- **Abaixo de 24 px o desenho muda.** A seta circular tem traço fino demais
  para sobreviver a 16 pixels: vira ruído cinza e come o espaço da pilha de
  fotos. Nesses tamanhos o ícone mostra só a pilha, ocupando o quadro inteiro.
  Não é perda de qualidade, é o desenho certo para o tamanho.

Para conferir, gere a prévia e olhe o pixel real ampliado, nunca o ícone no
tamanho final:

```sh
python fazer_ico.py logo.webp KaelithWall.ico
python fazer_ico.py logo.webp bandeja.ico 16,20,24,32,48
```

## Compilar

```sh
./compilar.sh
```

Usa o `csc.exe` do .NET Framework 4 que já vem no Windows — nada para instalar.
Pelo Bash, não pelo PowerShell (o PowerShell 5.1 aborta a compilação ao ver a
saída de erro do compilador, mesmo quando ela é só aviso).

## Testar

```sh
./KaelithWall.exe --autoteste
```

14 verificações da lógica real (varredura, ordem natural, próximo, anterior,
volta do ciclo nas duas pontas, embaralhamento sem perder nem repetir imagem,
pausa, intervalo, arquivo atual em disco, filtro de extensão). Sai com 0 se
passou, 2 se falhou, e não mexe no papel de parede.

## Ainda não tem

- Atalhos globais de teclado (o Background Switcher tinha N/P/V/C/S)
- Vários monitores com imagens diferentes
- Não está na inicialização do Windows ainda
