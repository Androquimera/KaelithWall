---
name: Kwall
description: Uma coleção inteira em movimento, sob controle do usuário.
colors:
  blue-deep: "#173a6f"
  blue-ink: "#0e284f"
  blue-action: "#356bc4"
  blue-bright: "#4b86e8"
  blue-sky: "#6ca0ee"
  blue-mountain: "#214b86"
  blue-ridge: "#17345f"
  blue-night: "#102d57"
  blue-slope: "#2e65b7"
  blue-aurora: "#10284d"
  teal-aurora: "#73d4c5"
  rose-dusk: "#cd6d79"
  text-on-blue: "#c8d9ef"
  sun: "#f6ca69"
  paper: "#f4f7fb"
  white: "#ffffff"
  ink: "#0d1b2a"
  muted: "#52657b"
  line: "#cfdaea"
typography:
  display:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "clamp(3.65rem, 6.7vw, 6rem)"
    fontWeight: 700
    lineHeight: 0.93
    letterSpacing: "-0.035em"
  headline:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "clamp(2.7rem, 5vw, 5.4rem)"
    fontWeight: 700
    lineHeight: 0.98
    letterSpacing: "-0.035em"
  origin-headline:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "clamp(2.8rem, 5vw, 5.2rem)"
    fontWeight: 700
    lineHeight: 0.98
  final-headline:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "clamp(2.5rem, 4.5vw, 4.8rem)"
    fontWeight: 700
    lineHeight: 0.98
  mobile-display:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "clamp(3rem, 15vw, 4.4rem)"
    fontWeight: 700
    lineHeight: 0.93
  mobile-headline:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "clamp(2.45rem, 12vw, 3.7rem)"
    fontWeight: 700
    lineHeight: 0.98
  title:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "1.28rem"
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: "-0.015em"
  brand:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "1.18rem"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: "-0.02em"
  lede:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: "clamp(1.05rem, 1.5vw, 1.25rem)"
    fontWeight: 400
    lineHeight: 1.55
  body:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: "17px"
    fontWeight: 400
    lineHeight: 1.55
  body-compact:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: "16px"
    fontWeight: 400
    lineHeight: 1.55
  story-body:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: "1.12rem"
    fontWeight: 400
    lineHeight: 1.55
  navigation:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: ".88rem"
    fontWeight: 650
    lineHeight: 1.2
  label:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: ".73rem"
    fontWeight: 700
    lineHeight: 1.2
    letterSpacing: ".13em"
  caption:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: ".7rem"
    fontWeight: 500
    lineHeight: 1.2
  small:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: ".83rem"
    fontWeight: 400
    lineHeight: 1.4
  toggle:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: ".72rem"
    fontWeight: 800
    lineHeight: 1.2
  metric:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "clamp(2.2rem, 5vw, 4.6rem)"
    fontWeight: 800
    lineHeight: 1
  metric-small:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "2.25rem"
    fontWeight: 800
    lineHeight: 1
  counter:
    fontFamily: "Bahnschrift, Arial Narrow, Segoe UI, sans-serif"
    fontSize: "1.8rem"
    fontWeight: 760
    lineHeight: 1
  icon:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: "1.7rem"
    fontWeight: 400
    lineHeight: 1
  editor-icon:
    fontFamily: "Segoe UI Variable, Segoe UI, Arial, sans-serif"
    fontSize: "2.5rem"
    fontWeight: 400
    lineHeight: 1
rounded:
  skip: "8px"
  control: "10px"
  panel: "12px"
  wallpaper: "13px"
  mobile-photo: "14px"
  stack: "18px"
  photo: "22px"
  round: "50%"
  pill: "999px"
spacing:
  xs: "8px"
  sm: "16px"
  md: "32px"
  lg: "80px"
components:
  button-primary:
    backgroundColor: "{colors.blue-action}"
    textColor: "{colors.white}"
    rounded: "{rounded.control}"
    padding: "12px 22px"
    height: "58px"
---

# Design System: Kwall

## Overview

**Creative North Star: "A pilha que nunca para"**

O sistema nasce do próprio logo: fotografias físicas empilhadas, passando de
uma posição para outra dentro de uma órbita contínua. A composição deve parecer
um desktop vivo, não uma página de startup. A pilha de imagens reaparece como
rodada, editor e chamada final; cada seção mostra um estado do mesmo mecanismo.

**Key Characteristics:**

- Azul de utilitário Windows, branco luminoso e um único sol amarelo.
- Fotografias com bordas brancas largas, sobreposição e sombra estrutural.
- Tipografia nativa do Windows, precisa e sem dependência de fontes externas.
- Movimento contínuo e reversível, sempre dispensável com redução de movimento.

## Colors

A paleta estende as cores comprovadas do logo. Azul profundo ancora confiança,
azul vivo conduz ações, branco frio mantém leitura e amarelo aparece somente
como sinal de foco ou ciclo concluído. Os tons `sky`, `mountain`, `ridge`,
`night`, `slope`, `aurora`, `teal` e `rose` pertencem apenas às paisagens
ilustradas: eles dão variedade aos wallpapers sem virar cores de interface.

**The One Sun Rule.** O amarelo nunca vira fundo decorativo; aparece uma vez por
composição para marcar o ponto de atenção.

## Typography

Bahnschrift conduz títulos e números por sua construção industrial ligada ao
Windows; Segoe UI Variable conduz texto e controles. A hierarquia usa escala e
peso, não caixa alta espalhada nem monoespaçada como fantasia técnica.

## Layout

O objeto protagonista ocupa aproximadamente metade do primeiro viewport. As
seções seguintes alternam demonstrações visuais e texto, invertendo a direção
para manter o ritmo. Em telas pequenas, tudo passa para uma coluna, preservando
a imagem antes de sua explicação. O ritmo alterna demonstração densa e respiro
amplo.

## Elevation & Depth

Sombras existem apenas para explicar a ordem física das fotografias. Controles
e blocos de texto permanecem planos. A profundidade nasce de deslocamento,
oclusão e bordas brancas; nenhum brilho neon ou vidro decorativo.

## Shapes

Fotografias usam cantos arredondados médios e moldura branca generosa. Botões
herdam um raio menor e firme. Setas e órbitas usam curvas largas do logo; demais
elementos evitam pílulas e formas genéricas.

## Components

### Buttons

O botão primário é azul, retangular e firme, com sombra deslocada azul-escura
que se comprime quando pressionado. O foco usa contorno amarelo visível. A ação
secundária é texto simples com uma seta e deslocamento horizontal no hover.

### Cards / Containers

Não existe um cartão genérico. Fotografias usam moldura branca e sombra física;
blocos de conteúdo usam somente espaço e mudança de superfície. A seção técnica
é um campo azul contínuo dividido por regras discretas.

### Navigation

A navegação é leve e direta. Links recebem apenas sublinhado fino no hover; o
seletor PT/EN é um interruptor pequeno que mantém os dois idiomas visíveis.

### Photo Stack

A pilha é o componente-assinatura: três imagens 16:10, moldura branca, rotações
individuais e uma órbita azul. Seus estados afastam a fila, isolam o editor e
reagrupam a coleção sem esconder a ação principal.

## Do's and Don'ts

### Do:

- **Do** fazer a pilha demonstrar a rodada completa e o enquadramento.
- **Do** usar o logo e a release real como prova, não decoração.
- **Do** preservar leitura, teclado e redução de movimento.

### Don't:

- **Don't** organizar a página como uma grade de cartões de recursos iguais.
- **Don't** inventar métricas, depoimentos ou compatibilidade não comprovada.
- **Don't** diluir o azul do logo em gradientes genéricos ou efeitos de vidro.
