<p align="center">
  <img src="logo.webp" alt="Logo do Kwall" width="180">
</p>

<h1 align="center">Kwall</h1>

<p align="center"><strong>Sua coleção inteira. Antes de repetir.</strong></p>

<p align="center">
  <a href="README.md">Português</a> ·
  <a href="README.en.md">English</a> ·
  <a href="https://github.com/Androquimera/Kwall/releases/latest"><strong>Baixar versão atual</strong></a>
</p>

<p align="center">
  <img alt="Última versão" src="https://img.shields.io/github/v/release/Androquimera/Kwall?style=flat-square&color=356bc4">
  <img alt="Licença MIT" src="https://img.shields.io/github/license/Androquimera/Kwall?style=flat-square&color=173a6f">
  <img alt="Windows" src="https://img.shields.io/badge/Windows-10%20%7C%2011-4b86e8?style=flat-square">
</p>

Troca o papel de parede do Windows a partir de uma pasta sua, sem pesar na
máquina.

Um executável portátil, sem instalador, sem dependência e sem serviço em
segundo plano. Fica como um ícone na bandeja do sistema enquanto trabalha.

## Por que existe

O Kwall nasceu porque o gerenciamento de wallpapers do Windows resetava
configurações sem explicação, oferecia pouco controle sobre o enquadramento e
acabava voltando a sequências reconhecíveis mesmo no modo aleatório. A proposta
é simples: manter o estado de forma confiável, dar controle por imagem e tratar
a pasta inteira como uma rodada antes de repetir qualquer arquivo.

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
- **Ajustar manualmente** — abre a imagem em tela cheia para você arrastar o
  enquadramento e escolher exatamente qual parte ficará visível; **Aplicar**
  salva a posição daquela imagem e **Cancelar** não muda nada
- **Escolher pasta**, **pastas recentes**, **incluir subpastas** e
  **recarregar pasta**

O item do topo do menu mostra o nome da imagem atual e a posição no ciclo,
assim: `paisagem.jpg   (38/185)`.

No modo aleatório ele embaralha a lista inteira e percorre até o fim antes de
repetir qualquer imagem, em vez de sortear uma por vez. Você vê todas as suas
imagens, e nenhuma aparece duas vezes antes das outras aparecerem uma. A rodada
continua do ponto em que parou mesmo se o aplicativo for fechado e aberto de
novo.

Formatos: JPG, JPEG, JFIF, PNG, BMP, GIF, TIF, TIFF e DIB.

O modo **Preencher** parte do mesmo enquadramento central mostrado no editor
manual. Quando você ajusta uma imagem, o Kwall lembra a posição só para
ela e reutiliza o enquadramento nas próximas vezes, mesmo se o arquivo for
renomeado, movido ou copiado para outra pasta.

---

## Como troca as imagens

A próxima imagem é decodificada e gravada em um BMP completo numa thread
separada antes da troca. O wallpaper anterior continua aplicado durante essa
preparação. Arquivos ilegíveis são rejeitados antes de alterar o wallpaper.
Dois arquivos de cache em `%LOCALAPPDATA%\Kwall` evitam sobrescrever
a imagem aplicada. Isso usa memória e disco durante a troca; as medições de
consumo da implementação anterior precisam ser refeitas.

A conversão mantém as dimensões reais em pixels — imagens marcadas como 72 DPI
não são ampliadas em 33,33% — e respeita a orientação EXIF gravada pela câmera.

---

## Instalar

1. Baixe o executável da [versão mais recente](https://github.com/Androquimera/Kwall/releases/latest)
2. Coloque numa pasta que você vá manter, por exemplo
   `C:\Users\<você>\Programas\Kwall`
3. Execute. Na primeira vez ele pergunta qual pasta de imagens usar.

Não tem instalador e não escreve nada no registro além da própria configuração
de papel de parede do Windows. Para desinstalar, feche pelo menu (**Sair**) e
apague a pasta.

**Para iniciar junto com o Windows:** aperte `Win + R`, digite `shell:startup`,
e coloque um atalho do executável na pasta que abrir.

### Onde ficam as configurações

Em `%APPDATA%\Kwall\config.txt`, um arquivo de texto comum, uma opção por
linha. Dá para editar à mão se preferir.

---

## Compilar do código

O Kwall é **um único arquivo de C#**, e compila com o compilador que já
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
./Kwall.exe --autoteste
```

São 26 verificações da lógica de verdade — varredura da pasta, ordenação
natural, navegação, rodada aleatória persistente, pausa, intervalo, filtro de
extensão, pastas recentes, memória de enquadramento pela identidade da imagem,
orientação EXIF, modos de ajuste, janela entre rodadas, enquadramento, cópia de pixels e reaplicação
concorrente. Sai com código 0 se passou e 2 se falhou, e **não mexe no seu papel
de parede** enquanto testa.

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

O Kwall é **gratuito e vai continuar gratuito**. Sem anúncio, sem versão
paga, sem telemetria, sem conta para criar. Ele não acessa a internet.

Eu escrevo esses programas sozinho, e qualquer ajuda faz diferença real aqui.
Se o Kwall te poupou tempo ou irritação, e você puder e quiser
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
