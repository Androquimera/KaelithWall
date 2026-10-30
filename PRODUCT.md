# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Usuários de Windows 10 e 11 que mantêm coleções próprias de imagens e querem
trocar o wallpaper sem repetição precoce, perda de enquadramento ou um programa
pesado rodando em segundo plano. A primeira comunicação é em português do
Brasil; a vitrine deve também permitir que público internacional compreenda o
produto.

## Product Purpose

O Kwall nasceu porque o gerenciamento de wallpapers do Windows perdia
configurações sem explicação, oferecia pouco controle de enquadramento e criava
sequências reconhecíveis mesmo no modo aleatório. O aplicativo percorre todas as
imagens antes de repetir, preserva a rodada entre reinícios e lembra o
enquadramento manual de cada imagem. Sucesso significa que uma pessoa reconhece
essas frustrações, entende a solução, confia no executável e consegue baixar a
versão atual.

## Positioning

Uma alternativa deliberada ao gerenciamento inconsistente do Windows: um
utilitário local, pequeno e transparente que mantém seu próprio estado, trata a
pasta como uma coleção completa e dá controle real sobre cada imagem. Sem conta,
anúncios ou telemetria; sem repetição antes de esgotar a rodada; com memória de
enquadramento ligada ao conteúdo da imagem.

## Operating Context

O aplicativo roda na bandeja do Windows. A pessoa escolhe uma pasta, ajusta o
intervalo e o modo de exibição, pode avançar ou pausar e, quando necessário,
abre um editor em tela cheia para posicionar o wallpaper. Configurações ficam
em arquivo de texto no perfil do usuário. O executável é distribuído pelas
releases públicas do GitHub.

## Capabilities and Constraints

- Executável portátil em C#/.NET Framework, sem instalador ou dependências
  adicionais.
- Ordem aleatória por rodadas completas, persistente entre reinícios.
- Preencher, ajustar, esticar, centralizar e enquadramento manual por imagem.
- Memória de pastas, suporte a subpastas e orientação EXIF.
- Código aberto sob licença MIT.
- A versão pública atual é 1.2.0; o código 1.2.1 possui 26 verificações e aguarda
  uma próxima release.
- A interface nativa está em português; localização completa em inglês continua
  uma decisão futura e não deve ser prometida pela vitrine.

## Brand Commitments

O nome é Kwall. O logo canônico está em `logo.webp`: três imagens azuis
empilhadas, um sol amarelo e duas setas circulares. A voz é direta, técnica sem
ser fria e sem marketing inflado. A vitrine preserva essa iconografia e não
transforma o produto em uma marca SaaS genérica.

## Evidence on Hand

- Código-fonte público e histórico de releases no GitHub.
- Release 1.2.0 validada com executável, hash SHA-256 e notas detalhadas; build
  local 1.2.1 validado com 26 autotestes.
- Autoteste real com 25 verificações aprovado no build publicado.
- `logo.webp`, ícones derivados, README, changelog e licença MIT.
- Não existem depoimentos, números de usuários ou benchmarks atuais aprovados;
  a vitrine não deve fabricá-los.

## Product Principles

1. Mostrar o mecanismo antes de adjetivar o produto.
2. Reduzir a distância entre entender e baixar.
3. Tratar transparência técnica como prova de confiança.
4. Manter o produto local, simples e sob controle do usuário.
5. Não prometer métricas que o build atual não mediu.

## Accessibility & Inclusion

A vitrine deve funcionar por teclado, respeitar redução de movimento, manter
contraste WCAG AA e ser legível em telas pequenas. Português e inglês devem ser
alternáveis sem esconder a ação principal.
