# MD Oluşturucu

**.NET 8 WPF** tabanlı, hafif ve hızlı bir Markdown ve HTML editörü. Gerçek zamanlı önizleme, söz dizimi vurgulama ve HTML dışa aktarma desteğiyle masaüstünde kesintisiz yazı deneyimi sunar.

🌐 **[mdoluşturucusu.okaser.com](https://xn--mdoluturucusu-mtc.okaser.com:8080/)**

---

## ⬇️ İndir

**[MDOlusturucu_v2.1_Setup.exe](https://xn--mdoluturucusu-mtc.okaser.com:8080/downloads/MDOlusturucu_v2.1_Setup.exe)**  
Windows 10/11 (x64) · .NET 8 dahil · Kurulum gerektirir

---

## Özellikler

- **Markdown & HTML editörü** — Tek uygulamada hem `.md` hem `.html` dosyaları düzenleyin
- **Gerçek zamanlı önizleme** — Markdown ve HTML çıktısını yazarken anlık görün
- **Söz dizimi vurgulama** — AvalonEdit tabanlı, Catppuccin renk paleti (Mocha / Latte)
- **HTML format araç çubuğu** — B / I / U / S / H1-H3 / P / code / pre / a / img / hr / ul / ol
- **HTML dışa aktarma** — Tema uyumlu modern CSS ile tek tıkla `.html` dosyası
- **Koyu / Açık tema** — Catppuccin Mocha & Latte
- **6 dil desteği** — Türkçe, İngilizce, Fransızca, Almanca, İspanyolca, Rusça
- **Dosya yönetimi** — Oluştur, yeniden adlandır, içe aktar, sil
- **Özelleştirilebilir editör** — Font ailesi ve renk seçimi
- **Sağ tık bağlam menüsü** — Kes / Kopyala / Yapıştır / Geri Al / Yeniden Yap (tema uyumlu)

## Gereksinimler

- Windows 10/11 (x64)
- .NET 8 Runtime *(installer içinde paketlidir — ayrıca yüklemeniz gerekmez)*

## Kurulum

1. [Son sürümü indir](https://xn--mdoluturucusu-mtc.okaser.com:8080/downloads/MDOlusturucu_v2.0_Setup.exe)
2. `MDOlusturucu_v2.0_Setup.exe` dosyasını çalıştırın
3. Kurulum sırasında sistem diliniz otomatik algılanır

## Kaynaktan Derleme

Gereksinimler: **.NET 8 SDK**, **Inno Setup 6**

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o publish/v2.0

& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" setup.iss
```

## Kullanılan Teknolojiler

| Paket | Amaç |
|-------|------|
| [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) | Gelişmiş metin editörü |
| [Markdig](https://github.com/xoofx/markdig) | Markdown → HTML dönüşümü |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM altyapısı |

## Dil Desteği

Uygulama arayüzü 6 dilde mevcuttur. Dil, kurulum sırasında sistem ayarınıza göre otomatik seçilir; Ayarlar menüsünden sonradan değiştirilebilir.

| Kod | Dil |
|-----|-----|
| `tr` | Türkçe |
| `en` | English |
| `fr` | Français |
| `de` | Deutsch |
| `es` | Español |
| `ru` | Русский |

## Lisans

© 2026 [OKASER](https://okaser.com) — MIT Lisansı
