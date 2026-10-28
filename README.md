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

## Como troca as imagens

A próxima imagem é decodificada e gravada em um BMP completo numa thread
separada antes da troca. O wallpaper anterior continua aplicado durante essa
preparação. Arquivos ilegíveis são rejeitados antes de alterar o wallpaper.
Dois arquivos de cache em `%LOCALAPPDATA%\KaelithWall` evitam sobrescrever
a imagem aplicada. Isso usa memória e disco durante a troca; as medições de
consumo da implementação anterior precisam ser refeitas.

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

**Pix:** `4bb64518-21fb-4b72-b587-ea363ab38aa2`

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
