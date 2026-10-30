<p align="center">
  <img src="logo.webp" alt="Kwall logo" width="180">
</p>

<h1 align="center">Kwall</h1>

<p align="center"><strong>Your whole collection. Before a repeat.</strong></p>

<p align="center">
  <a href="README.md">Português</a> ·
  <a href="README.en.md">English</a> ·
  <a href="https://github.com/Androquimera/Kwall/releases/latest"><strong>Download latest release</strong></a>
</p>

<p align="center">
  <img alt="Latest release" src="https://img.shields.io/github/v/release/Androquimera/Kwall?style=flat-square&color=356bc4">
  <img alt="MIT License" src="https://img.shields.io/github/license/Androquimera/Kwall?style=flat-square&color=173a6f">
  <img alt="Windows" src="https://img.shields.io/badge/Windows-10%20%7C%2011-4b86e8?style=flat-square">
</p>

Kwall rotates your Windows wallpaper from a folder you choose. It is a
portable, dependency-free tray application with no installer, account, ads, or
telemetry.

## Why it exists

Kwall was created because Windows wallpaper management would reset
settings without explanation, offered too little control over framing, and
kept returning to recognizable sequences even in shuffle mode. Its answer is
simple: preserve state reliably, give each image its own framing, and treat the
whole folder as one round before repeating anything.

## What makes it different

- **Every image before a repeat.** Random mode shuffles the complete queue and
  finishes the round before starting another one.
- **Rounds survive restarts.** Closing, reopening, or rescanning the folder does
  not discard the remaining sequence.
- **Per-image framing.** Drag a wallpaper into the right position once;
  Kwall remembers it even if the file is renamed, moved, or copied.
- **The previous wallpaper stays visible.** The next image is decoded and
  prepared completely before Windows switches to it.
- **EXIF orientation is respected.** Images rotated by cameras or Explorer are
  displayed in the intended orientation.
- **Local by design.** Settings stay in a plain text file on your computer and
  the application never accesses the internet.

## Features

The tray menu provides Next, Previous, Pause/Resume, open current image in
Explorer, random or sequential order, intervals from 10 seconds to one hour,
screen fit modes, manual framing, recent folders, subfolders, and folder reload.

Supported formats: JPG, JPEG, JFIF, PNG, BMP, GIF, TIF, TIFF, and DIB.

## Install

1. Download the executable from the [latest release](https://github.com/Androquimera/Kwall/releases/latest).
2. Keep it in a permanent folder, such as `C:\Users\<you>\Programs\Kwall`.
3. Run it and choose your wallpaper folder.

To start it with Windows, press `Win + R`, enter `shell:startup`, and place a
shortcut to the executable in that folder.

Configuration is stored at `%APPDATA%\Kwall\config.txt`.

## Build from source

Kwall is a single C# source file compiled with the .NET Framework compiler
already included with Windows. No Visual Studio, SDK, or packages are required.

```sh
./compilar.sh
```

Run the built-in test suite with:

```sh
./Kwall.exe --autoteste
```

The current source passes 26 checks covering folder scanning, natural order,
navigation, persistent random rounds, cross-round spacing, recent folders, EXIF orientation, fit
modes, framing memory, pixel conversion, and concurrent reapplication. Test
mode does not change the user's wallpaper.

## Support the project

Kwall is free and will remain free. If it is useful to you, you can star
the repository, report issues, share it, or support continued development:

**Pix:** `4bb64518-21fb-4b72-b587-ea363ab38aa2`

## License

MIT — use, modify, and distribute it freely. See [LICENSE](LICENSE).
