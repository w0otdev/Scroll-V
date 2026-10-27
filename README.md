<p align="center">
  <img src="Assets/logo.png" width="100" height="100" alt="Scroll-V">
</p>

<h2 align="center">Scroll-V</h2>

<p align="center">
  System-wide smooth scrolling for Windows
</p>

<p align="center">
  <a href="https://github.com/w0otdev/Scroll-V/releases/latest">
    <img src="https://img.shields.io/github/v/release/w0otdev/Scroll-V?style=for-the-badge&color=ffffff&labelColor=eeeeee&logo=github&logoColor=111111" alt="Latest Release">
  </a>
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-ffffff?style=for-the-badge&labelColor=eeeeee&logo=windows&logoColor=111111" alt="Windows 10 / 11">
  <img src="https://img.shields.io/badge/.NET-8.0-ffffff?style=for-the-badge&labelColor=eeeeee&logo=dotnet&logoColor=111111" alt=".NET 8">
  <a href="LICENSE">
    <img src="https://img.shields.io/github/license/w0otdev/Scroll-V?style=for-the-badge&color=ffffff&labelColor=eeeeee" alt="License">
  </a>
  <a href="https://github.com/w0otdev/Scroll-V/stargazers">
    <img src="https://img.shields.io/github/stars/w0otdev/Scroll-V?style=for-the-badge&color=ffffff&labelColor=eeeeee&logo=apachespark&logoColor=111111" alt="Stars">
  </a>
</p>

<p align="center">
  <a href="https://github.com/w0otdev/Scroll-V/releases/latest">Download</a> ·
  <a href="#usage">Usage</a> ·
  <a href="#installation">Installation</a> ·
  <a href="https://w00t.tech">Website</a>
</p>

<p align="center">
  Scroll-V brings fluid 120 FPS scrolling to all Windows applications. Adjust smoothness, speed, momentum, glide, and animation curves, or exclude specific apps with one click.
</p>

<p align="center">
  <b>Free and open source forever.</b>
</p>

<p align="center">
  Scroll-V is an independent project.<br>
  If you use it regularly, you can <a href="https://www.paypal.me/PhuocLe678"><b>support development via PayPal</b></a>.
</p>

<div id="usage"></div>

## Usage

| Action | Result |
| --- | --- |
| Close window | Minimize to system tray (~6 MB RAM) |
| Double-click tray icon | Open Scroll-V settings |
| Right-click tray icon | Toggle smooth scroll or exit |
| `Ctrl + Scroll` | Native scroll (bypasses smooth scroll for zoom) |
| Active Processes (`CONFIGURE`) | Toggle smooth scrolling on or off for specific apps |
| `VI` / `EN` switch | Switch language between Vietnamese and English |

<details>
<summary><strong>Tuning parameters</strong></summary>

| Setting | Description | Range |
| --- | --- | --- |
| Smoothness | Interpolation duration | Very Smooth to Instant |
| Scroll Speed | Distance multiplier per wheel step | 0.5x to 3.0x |
| Momentum | How long scrolling continues after input stops | Low to Very High |
| Glide | Inertia boost on consecutive scroll gestures | Subtle to Maximum |
| Animation | Easing curve | Linear, EaseOutQuad, EaseOutCubic, EaseOutExpo, EaseOutCirc, EaseInOutQuad, Elastic, Back |

</details>

<div id="installation"></div>

## Installation

Scroll-V runs on 64-bit Windows 10 or Windows 11 and requires the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0).

1. Download `Scroll-V-Setup.exe` (installer) or `Scroll-V.exe` (portable) from [Releases](https://github.com/w0otdev/Scroll-V/releases).
2. Run the executable.
3. Optionally enable "Start with Windows" in Settings.

## Star history

Thank you to everyone who has supported Scroll-V since the early releases. Your feedback and GitHub stars help keep the project going.

[![Star History Chart](https://api.star-history.com/svg?repos=w0otdev/scroll-v&type=Date)](https://star-history.com/#w0otdev/scroll-v&Date)

## Other projects by w00tdev

If you like Scroll-V, check out other free and open-source Windows utilities by w00tdev:

- **[V-Notch](https://github.com/w0otdev/V-Notch)** - Desktop status, media controls, synced lyrics, and Dynamic Island companion for Windows (built with C#).
- **[Vertex](https://github.com/w0otdev/Vertex)** - Minimalist radial wheel file converter for Windows (built with Rust).

## License

Scroll-V uses the MIT License. See [LICENSE](LICENSE).

<p align="center">
  Made by <a href="https://w00t.tech">w00tdev</a> ·
  <a href="https://github.com/w0otdev/Scroll-V">GitHub</a> ·
  <a href="https://www.facebook.com/rain.107/">Facebook</a> ·
  <a href="https://www.paypal.me/PhuocLe678">Donate</a>
</p>
