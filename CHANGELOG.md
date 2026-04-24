# Değişiklik Geçmişi

## [1.3] — 2026-04-24
- Dil ComboBox görünüm hatası düzeltildi (LanguageItem.ToString() yerine DisplayName gösteriliyor)
- HTML'e Çevir: Program Files erişim hatası giderildi, SaveFileDialog ile konum seçimi eklendi
- HTML'e Çevir: SaveFileDialog başlangıç dizini otomatik belirleniyor (MD klasörü yazılabilirse orası, değilse Belgeler)

## [1.2] — 2026-04-24
- HTML'e Çevir butonu eklendi (tema uyumlu Catppuccin CSS ile dışa aktarma)
- Hakkında penceresi eklendi (versiyon, yayıncı bilgisi)
- Ayarlar penceresine Dil seçimi eklendi
- 6 dil desteği: Türkçe, İngilizce, Fransızca, Almanca, İspanyolca, Rusça
- Installer otomatik dil algılama (sistem diline göre settings.json oluşturulur)
- Assembly metadata eklendi (Dosya Özellikleri → Ayrıntılar)

## [1.0] — 2026-04-23
- İlk sürüm
- Gerçek zamanlı Markdown önizleme
- AvalonEdit tabanlı söz dizimi vurgulama (Catppuccin Mocha / Latte)
- Koyu / Açık tema
- Dosya yönetimi (oluştur, yeniden adlandır, içe aktar, sil)
- Özelleştirilebilir editör (font ailesi, renk)
- Inno Setup installer (win-x64, self-contained)
