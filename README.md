# KaelithWall

Troca o papel de parede do Windows a partir de uma pasta sua, sem pesar na
máquina.

Um executável de 232 KB, sem instalador, sem dependência, sem serviço em
segundo plano. Fica como um ícone na bandeja do sistema e consome **0,02% de
um núcleo** de processador enquanto trabalha.

---

## O que faz

Você aponta uma pasta de imagens. Ele vai trocando o papel de parede, no
intervalo que você escolher.

Pelo menu do ícone na bandeja:

- **Próximo** e **Anterior** — clique duplo no ícone também avança
- **Pausar** e **Retomar**
- **Abrir local da imagem** — abre a pasta no Explorer com o arquivo que está
  na tela já selecionado, para você renomear, mover ou apagar na hora
- **Ordem aleatória** ou **ordem em sequência** — a sequência usa a mesma
  ordenação do Explorer, então `9.jpg` vem antes de `10.jpg`
- **Intervalo** — de 10 segundos a 1 hora
- **Ajuste na tela** — preencher, ajustar, esticar ou centralizar
- **Escolher pasta**, **incluir subpastas** e **recarregar pasta**

O item do topo do menu mostra o nome da imagem atual e a posição no ciclo,
assim: `paisagem.jpg   (38/185)`.

No modo aleatório ele embaralha a lista inteira e percorre até o fim antes de
repetir qualquer imagem, em vez de sortear uma por vez. Você vê todas as suas
imagens, e nenhuma aparece duas vezes antes das outras aparecerem uma.

Formatos: JPG, JPEG, JFIF, PNG, BMP, GIF, TIF, TIFF e DIB.

---

## Por que ele é leve

Essa é a ideia inteira do projeto, e cabe em uma frase:

> **O KaelithWall nunca toca em pixel nenhum.**

Um trocador de papel de parede parece exigir trabalho de imagem: abrir o
arquivo, decodificar, redimensionar para a tela, desenhar. É aí que esses
programas gastam processador e memória, e é por isso que costumam engasgar
quando a pasta tem fotos grandes.

Só que o Windows **já sabe fazer isso**, e faz melhor: tem o código otimizado,
roda dentro do `explorer.exe` e guarda o resultado em cache. Então o
KaelithWall não compete com ele. Ele entrega o caminho do arquivo original
através de uma única chamada do sistema, `SystemParametersInfo` com
`SPI_SETDESKWALLPAPER`, e sai da frente.

O efeito prático:

| | processo do KaelithWall | explorer.exe | total |
|---|---|---|---|
| **CPU**, trocando a cada 10 s | 0,02% | 7,66% | **7,67% de um núcleo** |
| **RAM** | 25,7 MB | — | — |

Medido numa pasta real de 185 imagens somando 1,1 GB, no Windows 10. Para
comparar, o `explorer.exe` sozinho, sem nada trocando papel de parede, já gasta
0,59%.

Nenhuma biblioteca de imagem é carregada. Nenhum arquivo intermediário é
gravado em disco. A imagem que vai para a tela é o seu arquivo original, do
jeito que está — nada é recomprimido nem reduzido.

### O outro lado da moeda

A troca em si passa a custar tempo do `explorer`: medido, **1.314 ms** no
arquivo mais pesado que testei, um PNG de 83 MB, e **menos de 100 ms** nos
JPEGs comuns. Isso aparece como o papel de parede levando um instante a mais
para surgir quando a imagem é enorme.

Por isso a troca sai de uma thread separada: o menu da bandeja responde na
hora, mesmo enquanto o Windows ainda está processando a imagem anterior.

### E o resto do tempo?

Zero. Não existe laço nenhum rodando, nem verificação periódica. O programa
dorme na fila de mensagens do Windows e só acorda quando o temporizador
dispara ou quando você clica. A diferença entre *esperar* e *ficar conferindo
se já é hora* é a diferença entre 0,02% e vários por cento de processador —
para sempre, em segundo plano, enquanto o computador estiver ligado.

---

## Instalar

1. Baixe o `KaelithWall.exe`
2. Coloque numa pasta que você vá manter, por exemplo
   `C:\Users\<você>\Programas\KaelithWall`
3. Execute. Na primeira vez ele pergunta qual pasta de imagens usar.

Não tem instalador e não escreve nada no registro além da própria configuração
de papel de parede do Windows. Para desinstalar, feche pelo menu (**Sair**) e
apague a pasta.

**Para iniciar junto com o Windows:** aperte `Win + R`, digite `shell:startup`,
e coloque um atalho do executável na pasta que abrir.

### Onde ficam as configurações

Em `%APPDATA%\KaelithWall\config.txt`, um arquivo de texto comum, uma opção por
linha. Dá para editar à mão se preferir.

---

## Compilar do código

O KaelithWall é **um único arquivo de C#**, e compila com o compilador que já
vem dentro do Windows, em `C:\Windows\Microsoft.NET\Framework64\v4.0.30319`.
Não precisa instalar Visual Studio, nem SDK, nem pacote nenhum:

```sh
./compilar.sh
```

Rode pelo Bash (o Git Bash serve), não pelo PowerShell: o PowerShell 5.1 trata
qualquer saída de erro de um executável como falha e aborta a compilação mesmo
quando ela deu certo.

### Testar

O próprio executável carrega a bateria de testes dentro dele:

```sh
./KaelithWall.exe --autoteste
```

São 14 verificações da lógica de verdade — varredura da pasta, ordenação
natural, próximo, anterior, volta do ciclo nas duas pontas, embaralhamento sem
perder nem repetir imagem, pausa, intervalo, filtro de extensão. Sai com código
0 se passou e 2 se falhou, e **não mexe no seu papel de parede** enquanto
testa.

### O ícone

`logo.webp` é a arte original. O `fazer_ico.py` gera os `.ico` a partir dela.
Duas coisas ali não são óbvias e vale deixar registrado:

- **A redução é feita em alfa premultiplicado.** Reduzir uma imagem RGBA
  diretamente mistura o preto invisível do fundo transparente na média, e
  aparece uma franja escura em volta das partes claras do desenho.
- **Abaixo de 24 pixels o desenho muda.** A seta circular do logo tem traço
  fino demais para sobreviver a 16 pixels: vira ruído e ainda rouba espaço do
  que identifica o app. Nesse tamanho o ícone mostra só a pilha de fotos,
  ocupando o quadro inteiro. Isso não é perda de qualidade, é o desenho certo
  para o tamanho.

---

## Gostou? Me ajuda a continuar

O KaelithWall é **gratuito e vai continuar gratuito**. Sem anúncio, sem versão
paga, sem telemetria, sem conta para criar. Ele não acessa a internet.

Eu escrevo esses programas sozinho, e qualquer ajuda faz diferença real aqui.
Se o KaelithWall te poupou tempo ou irritação, e você puder e quiser
contribuir:

<!-- Preencha com a sua chave antes de publicar -->
**Pix:** `SUA_CHAVE_PIX_AQUI`

Se não puder, tudo bem — sério. Nesse caso ajuda do mesmo jeito:

- deixar uma ⭐ no repositório
- contar para alguém que também sofre com isso
- abrir uma issue quando algo quebrar ou faltar

---

## O que ainda não tem

- Atalhos globais de teclado para passar imagem sem abrir o menu
- Imagens diferentes em cada monitor, num setup de vários monitores

---

## Licença

MIT — use, modifique e distribua à vontade. Veja [LICENSE](LICENSE).
