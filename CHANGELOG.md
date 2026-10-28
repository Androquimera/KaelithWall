# Changelog

## 1.0.0 — 28/10/2026

Primeira versão. Substitui o John's Background Switcher, que foi desinstalado
da máquina.

### Adicionado

- Trocador de papel de parede na bandeja do sistema, em arquivo único
  (`KaelithWall.cs`), compilado com o `csc.exe` do .NET Framework 4 que já vem
  no Windows — sem SDK, sem pacote, sem dependência. Exe de 232 KB.
- Menu da bandeja: Próximo, Anterior, Pausar/Retomar, Abrir local da imagem
  (abre o Explorer com o arquivo atual selecionado), ordem aleatória ou em
  sequência, intervalo (10 s a 1 h), ajuste na tela (preencher, ajustar,
  esticar, centralizar), escolher pasta, incluir subpastas, recarregar pasta.
  Clique duplo no ícone avança. O item do topo mostra o nome da imagem e a
  posição no ciclo.
- Configuração em texto simples, em `%APPDATA%\KaelithWall\config.txt`.
- Ícone próprio em dez tamanhos (16 a 256), gerado de `logo.webp` pelo
  `fazer_ico.py`.
- `--autoteste`: 14 verificações da lógica real, sem tocar no papel de parede.
  Sai com 0 se passou, 2 se falhou.

### Desempenho

Medido em 27/10/2026, trocando a cada 10 segundos, na mesma pasta de 185
imagens (1,1 GB), com o `explorer.exe` contado junto porque é ele quem passa a
fazer o trabalho de imagem:

| | processo próprio | explorer.exe | **CPU total** | RAM |
|---|---|---|---|---|
| Background Switcher | 35,33% | 4,27% | **39,60%** | 56,5 MB |
| KaelithWall | 0,02% | 7,66% | **7,67%** | 25,7 MB |

Base: com nada trocando, o `explorer` sozinho gasta 0,59%.

**5,2× menos CPU e menos da metade da RAM**, sem perder função nem qualidade.
O motivo é um só: o app nunca decodifica, escala ou grava imagem. Entrega o
caminho do arquivo original ao Windows (`SPI_SETDESKWALLPAPER`) e quem faz o
trabalho é o próprio Windows, com cache. O Background Switcher fazia o
contrário — carregava o ImageMagick (23 MB só na DLL nativa), redimensionava e
gravava um BMP de 16,6 MB a cada troca, animava uma transição de 33 passos e
repintava o texto dos ícones da área de trabalho a cada 2 minutos, gastando CPU
mesmo sem trocar nada.

`SPI_SETDESKWALLPAPER` leva 1.314 ms no arquivo mais pesado da pasta
(`64.png`, 83,4 MB) e menos de 100 ms nos JPEGs comuns. Esse tempo é do
`explorer`, e a troca sai de uma thread separada, então o menu da bandeja nunca
congela.

### Testes

`--autoteste`: 14/14. Cobre varredura, ordem natural (9 antes de 10, igual ao
Explorer), Próximo, Anterior, volta do ciclo nas duas pontas, embaralhamento
sem perder nem repetir imagem, pausa, intervalo e filtro de extensão.

### Pendente

- Atalhos globais de teclado (o Background Switcher tinha N/P/V/C/S)
- Vários monitores com imagens diferentes
