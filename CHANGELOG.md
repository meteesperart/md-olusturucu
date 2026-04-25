# Değişiklik Geçmişi

## [2.1] — 2026-04-24
- HTML'e Çevir: dışa aktarılan dosya sol panele eklendi (seçim korunur)

## [2.0] — 2026-04-24
- HTML dosyası desteği: sol panelde .html dosyaları listelenir, seçilir ve düzenlenir
- HTML editöründe gerçek zamanlı önizleme (tema uyumlu, koyu/açık)
- HTML format araç çubuğu: B / I / U / S / H1 / H2 / H3 / P / code / pre / a / img / hr / ul / ol
- Özel HtmlColorizer: etiket adları, öznitelik adları ve değerleri Catppuccin renk paleti ile vurgulanır
- Sağ tık bağlam menüsü: Kes / Kopyala / Yapıştır / Tümünü Seç / Geri Al / Yeniden Yap (tema uyumlu)
- Hakkında penceresine Sürüm Geçmişi bölümü eklendi
- Dosya seçiminde sol panel titremesi giderildi (HTML araç çubuğu sabit yükseklik)
- Telif hakkı güncellendi: © 2026 OKASER

## [1.4] — 2026-04-24
- Uygulama ikonu arka planı transparan yapıldı (kare görünüm giderildi)
- Varsayılan MD dosyaları dizini Program Files'dan Documents\MD Oluşturucu'ya taşındı (UAC sorunu giderildi)
- Demo dosyaları kurulumda kullanıcının Documents\MD Oluşturucu klasörüne kopyalanıyor

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
