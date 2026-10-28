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

### Corrigido

- O texto do ícone na bandeja agora respeita o limite de 63 caracteres do
  Windows, inclusive com nomes de arquivo longos.
- A próxima imagem é decodificada e preparada por completo em um BMP de cache
  antes da troca. O wallpaper anterior permanece visível enquanto isso.
- O estado da imagem atual só é salvo depois que o Windows confirma que aplicou
  o novo wallpaper.
- O autoteste não altera mais a configuração do usuário.

### Desempenho

A preparação ocorre numa thread separada para manter o menu da bandeja
responsivo. Dois arquivos BMP são alternados em `%LOCALAPPDATA%\KaelithWall`,
evitando sobrescrever o arquivo que o Windows ainda está usando.

As medições anteriores pertenciam à implementação que entregava a imagem
original diretamente ao Windows e não representam esta versão. CPU, memória e
tempo de troca ainda precisam ser medidos novamente com o preparo em BMP.

### Testes

`--autoteste`: 14/14. Cobre varredura, ordem natural, Próximo, Anterior, volta
do ciclo nas duas pontas, embaralhamento sem perder nem repetir imagem, pausa,
intervalo e filtro de extensão.

### Pendente

- Atalhos globais de teclado
- Vários monitores com imagens diferentes
