# Changelog

## 1.1.1 — 30/10/2026

### Corrigido

- A ordem aleatória agora mantém uma rodada verificável: todas as imagens da
  pasta são exibidas uma vez antes que qualquer uma possa se repetir.
- A fila embaralhada e o conjunto de imagens já exibidas sobrevivem ao reinício
  do aplicativo e às novas varreduras da pasta. Arquivos adicionados entram na
  rodada atual sem jogar fora a sequência restante; arquivos removidos deixam
  de bloquear o avanço.
- O embaralhamento usa números aleatórios criptográficos sem viés de módulo,
  evitando que novas execuções com sementes próximas produzam sequências
  parecidas.
- A posição manual passou a ser associada ao conteúdo da imagem, além do
  caminho. Renomear, mover ou copiar o mesmo arquivo não perde mais o
  enquadramento salvo.
- O estado da rodada só marca uma imagem como exibida depois que ela é aplicada
  com sucesso pelo Windows.

### Alterado

- Configurações antigas de posição são migradas automaticamente para a nova
  identidade por conteúdo, sem apagar a compatibilidade pelo caminho.
- A bateria `--autoteste` passou de 20 para 25 verificações, incluindo rodada
  completa sem repetição, retomada após reinício e enquadramento preservado
  entre caminhos diferentes.

## 1.1.0 — 28/10/2026

### Adicionado

- Editor manual de enquadramento em tela cheia: arraste a imagem e escolha
  **Aplicar** ou **Cancelar** antes de alterar o wallpaper.
- Posição de enquadramento persistente por imagem.
- Menu de pastas recentes na bandeja, sem duplicatas e com a pasta mais recente
  no topo.

### Alterado

- **Preencher** agora compõe o wallpaper já na resolução da tela, centralizado
  pelo mesmo cálculo usado no editor manual.
- A bateria `--autoteste` passou de 14 para 20 verificações.

### Corrigido

- Imagens marcadas como 72 DPI não são mais ampliadas em 33,33% durante a
  conversão; a cópia agora preserva as dimensões reais em pixels.
- A orientação EXIF é aplicada antes do enquadramento e da conversão.
- Uma solicitação para reaplicar o wallpaper feita durante outra troca não é
  mais descartada: o último pedido fica pendente e roda ao final.

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
