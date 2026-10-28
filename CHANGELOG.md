# Changelog

## 1.0.0 — 28/10/2026

Primeira versão.

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
- Ordem em sequência usa `StrCmpLogicalW`, a mesma comparação do Explorer:
  `9.jpg` vem antes de `10.jpg`.
- Ordem aleatória embaralha a lista inteira e percorre até o fim antes de
  repetir, em vez de sortear uma imagem por vez.
- Configuração em texto simples, em `%APPDATA%\KaelithWall\config.txt`.
- Ícone próprio em dez tamanhos (16 a 256), gerado de `logo.webp` pelo
  `fazer_ico.py`, com redução em alfa premultiplicado e desenho simplificado
  abaixo de 24 px.
- `--autoteste`: 14 verificações da lógica real, sem tocar no papel de parede.
  Sai com 0 se passou, 2 se falhou.

### Desempenho

Medido em 27/10/2026, trocando a cada 10 segundos, numa pasta de 185 imagens
(1,1 GB), no Windows 10:

| | processo próprio | explorer.exe | total |
|---|---|---|---|
| CPU | 0,02% | 7,66% | **7,67% de um núcleo** |
| RAM | 25,7 MB | — | — |

Base de comparação: com nada trocando papel de parede, o `explorer.exe`
sozinho já gasta 0,59%.

O custo é baixo porque o app nunca decodifica, escala ou grava imagem. Ele
entrega o caminho do arquivo original ao Windows (`SPI_SETDESKWALLPAPER`) e
quem faz o trabalho é o próprio Windows, com cache. Nenhuma biblioteca de
imagem é carregada e nenhum arquivo intermediário é gravado em disco.

`SPI_SETDESKWALLPAPER` leva 1.314 ms no arquivo mais pesado testado
(PNG de 83,4 MB) e menos de 100 ms nos JPEGs comuns. Esse tempo é do
`explorer`, e a troca sai de uma thread separada, então o menu da bandeja
nunca congela.

### Testes

`--autoteste`: 14/14. Cobre varredura, ordem natural, Próximo, Anterior, volta
do ciclo nas duas pontas, embaralhamento sem perder nem repetir imagem, pausa,
intervalo e filtro de extensão.

### Pendente

- Atalhos globais de teclado
- Vários monitores com imagens diferentes
