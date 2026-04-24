@echo off
echo [1/2] Uygulama yayinlaniyor...
dotnet publish MDOlusturucu.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=false ^
  -o bin\publish

echo [2/2] Inno Setup ile kurulum olusturuluyor...
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss

echo.
echo Tamamlandi! installer\ klasorunu kontrol edin.
pause
