# MD Oluşturucu

**.NET 8 WPF** tabanlı, hafif ve hızlı bir Markdown editörü. Gerçek zamanlı önizleme, söz dizimi vurgulama ve HTML dışa aktarma desteğiyle masaüstünde kesintisiz yazı deneyimi sunar.

🌐 **[mdoluşturucusu.okaser.com](https://xn--mdoluturucusu-mtc.okaser.com:8080/)**

---

## Özellikler

- **Gerçek zamanlı önizleme** — Markdown'u yazarken HTML çıktısını anlık görün
- **Söz dizimi vurgulama** — AvalonEdit tabanlı, Catppuccin renk paleti
- **HTML dışa aktarma** — Tema uyumlu (Mocha / Latte) modern CSS ile tek tıkla `.html` dosyası
- **Koyu / Açık tema** — Catppuccin Mocha & Latte
- **6 dil desteği** — Türkçe, İngilizce, Fransızca, Almanca, İspanyolca, Rusça
- **Dosya yönetimi** — Oluştur, yeniden adlandır, içe aktar, sil
- **Özelleştirilebilir editör** — Font ailesi ve renk seçimi

## Gereksinimler

- Windows 10/11 (x64)
- .NET 8 Runtime *(installer içinde paketlidir — ayrıca yüklemeniz gerekmez)*

## Kurulum

**İndir ve çalıştır:**

1. [Son sürümü indir](https://xn--mdoluturucusu-mtc.okaser.com:8080/downloads/MDOlusturucu_v1.3_Setup.exe)
2. `MDOlusturucu_v1.3_Setup.exe` dosyasını çalıştırın
3. Kurulum sırasında sistem diliniz otomatik algılanır

## Kaynaktan Derleme

Gereksinimler: **.NET 8 SDK**, **Inno Setup 6**

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:PublishReadyToRun=true `
  -o publish/v1.3

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

© 2025 [OKASER](https://okaser.com) — MIT Lisansı
