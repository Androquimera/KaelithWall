# Converte a logo em .ico com todos os tamanhos que o Windows usa.
#
# Dois cuidados que fazem diferenca na qualidade:
#  1) redimensionar em alfa PREMULTIPLICADO (modo 'RGBa' do Pillow). Sem isso,
#     o preto invisivel do fundo transparente entra na media e deixa uma
#     franja escura em volta das molduras brancas.
#  2) escrever as entradas pequenas como BMP 32 bits, que e o formato que o
#     System.Drawing do .NET le sem reclamar; so a de 256 vai em PNG, para o
#     arquivo nao ficar gigante.
import io
import struct
import sys

from PIL import Image, ImageFilter

origem, destino = sys.argv[1], sys.argv[2]
# Terceiro argumento opcional: lista de tamanhos. O exe leva o jogo completo
# como icone de arquivo, e uma copia so com os tamanhos de bandeja por dentro
# -- gravar os dez duas vezes engordaria o exe em 380 KB a toa.
TAMANHOS = ([int(t) for t in sys.argv[3].split(",")] if len(sys.argv) > 3
            else [16, 20, 24, 32, 40, 48, 64, 96, 128, 256])

base = Image.open(origem).convert("RGBA")

# Recorta a sobra transparente para a arte ocupar o quadro inteiro.
caixa = base.getbbox()
if caixa:
    base = base.crop(caixa)
lado = max(base.size)
quadro = Image.new("RGBA", (lado, lado), (0, 0, 0, 0))
quadro.paste(base, ((lado - base.width) // 2, (lado - base.height) // 2))
print("arte recortada para %dx%d" % (lado, lado))

premultiplicado = quadro.convert("RGBa")

# Variante para os tamanhos minusculos. A seta circular tem traco fino demais
# para sobreviver a 16 ou 20 pixels: vira ruido cinza em volta e come o espaco
# da pilha de fotos, que e o que identifica o app. Entao abaixo de 24 px o
# icone mostra so a pilha, ocupando o quadro inteiro. Nao e perda de qualidade
# e sim o desenho certo para o tamanho -- as molduras brancas e o sol dourado
# continuam legiveis, que e o que se reconhece na bandeja.
mascara_molduras = quadro.copy()
larguras = mascara_molduras.load()
for y in range(mascara_molduras.height):
    for x in range(mascara_molduras.width):
        r, g, b, a = larguras[x, y]
        branco = a > 128 and min(r, g, b) > 200
        larguras[x, y] = (255, 255, 255, 255) if branco else (0, 0, 0, 0)
caixa_pilha = mascara_molduras.getbbox()
margem = max(2, int(lado * 0.012))
caixa_pilha = (
    max(0, caixa_pilha[0] - margem),
    max(0, caixa_pilha[1] - margem),
    min(lado, caixa_pilha[2] + margem),
    min(lado, caixa_pilha[3] + margem),
)
pilha = quadro.crop(caixa_pilha)
lado_pilha = max(pilha.size)
quadro_pilha = Image.new("RGBA", (lado_pilha, lado_pilha), (0, 0, 0, 0))
quadro_pilha.paste(pilha, ((lado_pilha - pilha.width) // 2, (lado_pilha - pilha.height) // 2))
premultiplicado_pilha = quadro_pilha.convert("RGBa")
print("pilha isolada em %s -> %dx%d" % (caixa_pilha, lado_pilha, lado_pilha))


def reduzir(tamanho):
    fonte = premultiplicado_pilha if tamanho < 24 else premultiplicado
    img = fonte.resize((tamanho, tamanho), Image.LANCZOS).convert("RGBA")
    if tamanho <= 32:
        # Nos tamanhos minusculos a reducao achata o contraste; uma mascara de
        # nitidez leve devolve a silhueta sem inventar detalhe.
        img = img.filter(ImageFilter.UnsharpMask(radius=0.6, percent=55, threshold=0))
    return img


def entrada_bmp(img):
    larg, alt = img.size
    cabecalho = struct.pack(
        "<IiiHHIIiiII", 40, larg, alt * 2, 1, 32, 0, larg * alt * 4, 0, 0, 0, 0
    )
    pixels = bytearray()
    for y in range(alt - 1, -1, -1):          # BMP guarda de baixo para cima
        for x in range(larg):
            r, g, b, a = img.getpixel((x, y))
            pixels += bytes((b, g, r, a))     # e em BGRA
    linha_mascara = ((larg + 31) // 32) * 4   # mascara AND, zerada
    return cabecalho + bytes(pixels) + bytes(linha_mascara * alt)


def entrada_png(img):
    buf = io.BytesIO()
    img.save(buf, format="PNG", optimize=True)
    return buf.getvalue()


blocos = []
for t in TAMANHOS:
    img = reduzir(t)
    dados = entrada_png(img) if t == 256 else entrada_bmp(img)
    blocos.append((t, dados))
    print("  %3dx%-3d  %6d bytes  %s" % (t, t, len(dados), "PNG" if t == 256 else "BMP"))

deslocamento = 6 + 16 * len(blocos)
diretorio = b""
corpo = b""
for t, dados in blocos:
    diretorio += struct.pack(
        "<BBBBHHII", t % 256, t % 256, 0, 0, 1, 32, len(dados), deslocamento
    )
    corpo += dados
    deslocamento += len(dados)

with open(destino, "wb") as f:
    f.write(struct.pack("<HHH", 0, 1, len(blocos)) + diretorio + corpo)

print("gravado: %s (%d bytes)" % (destino, 6 + 16 * len(blocos) + len(corpo)))

# Confere lendo de volta.
verificar = Image.open(destino)
print("tamanhos lidos de volta:", sorted(verificar.ico.sizes()))
