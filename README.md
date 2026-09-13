<!--
##########################################
#           TikTok Downloader            #
#           Made by Jettcodey             #
#                © 2024                  #
#           DO NOT REMOVE THIS           #
##########################################
-->

# TikTok Keyword Downloader

Bahasa Indonesia | [English](README.en.md)

Fork CLI untuk Windows yang mencari post TikTok berdasarkan keyword, lalu mengunduh video atau foto dari hasil pencarian.

Fork ini mengganti flow WinForms dari proyek upstream dengan prompt terminal yang lebih singkat:

1. Masukkan keyword.
2. Pilih Video atau Foto.
3. Tentukan jumlah post.
4. Selesaikan CAPTCHA atau login di browser jika TikTok memintanya.
5. Tunggu sampai file tersimpan di folder `downloads`.

## Download

Unduh paket Windows x64 dari halaman [Releases](https://github.com/thoriq1520/TikTok-Downloader/releases/latest). Ekstrak ZIP, lalu jalankan `TikTok Downloader.bat`.

Paket release bersifat self-contained dan sudah membawa runtime .NET yang dibutuhkan aplikasi.

## Fitur

- Mencari post TikTok berdasarkan keyword.
- Memilih hasil berupa video atau foto.
- Mengunduh 10 post jika jumlah dikosongkan.
- Menerima jumlah 1 sampai 500 post.
- Membuka Chrome, Edge, atau Brave untuk mengambil hasil pencarian.
- Menyimpan nama file berdasarkan caption.
- Memberi nomor urut pada setiap gambar dalam post foto.
- Menghentikan proses dengan `Ctrl+C`.

Jumlah yang dimasukkan dihitung per post. Satu post foto dapat menghasilkan beberapa file gambar.

## Kebutuhan

- Windows 10 atau versi lebih baru, x64.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) untuk build.
- Google Chrome, Microsoft Edge, atau Brave.
- Koneksi internet.

## Build

Clone fork ini:

```powershell
git clone https://github.com/thoriq1520/TikTok-Downloader.git
cd TikTok-Downloader
```

Build konfigurasi Release:

```powershell
dotnet build ".\src\TikTok Downloader.csproj" -c Release
```

Hasil build berada di:

```text
src\bin\Release\net8.0-windows10.0.17763.0\
```

Folder tersebut berisi executable .NET dan launcher `TikTok Downloader.bat`.

## Menjalankan aplikasi

Cara termudah adalah menjalankan BAT dari folder source:

```powershell
& ".\src\TikTok Downloader.bat"
```

Launcher akan mencari build Release. Jika EXE belum tersedia, launcher menjalankan build terlebih dahulu.

Kamu juga dapat menjalankan launcher langsung dari folder hasil build:

```powershell
& ".\src\bin\Release\net8.0-windows10.0.17763.0\TikTok Downloader.bat"
```

Untuk melihat bantuan:

```powershell
& ".\src\TikTok Downloader.bat" --help
```

Aplikasi akan meminta input berikut:

```text
Keyword pencarian: kucing
Pilih media [1] Video  [2] Foto: 1
Jumlah post [10]: 5
```

Browser terbuka selama pencarian. Jika TikTok menampilkan CAPTCHA atau halaman login, selesaikan di browser tersebut. Aplikasi akan melanjutkan pencarian secara otomatis.

## Lokasi dan nama file

File disimpan relatif terhadap lokasi launcher:

```text
downloads\
└── <keyword>\
    ├── videos\
    │   └── <caption>.mp4
    └── photos\
        ├── <caption>_01.jpg
        └── <caption>_02.jpg
```

Karakter yang tidak valid untuk nama file Windows dihapus dari caption. Jika nama yang sama sudah ada, aplikasi menambahkan nomor urut agar file lama tidak tertimpa.

## Cara kerja

[Microsoft Playwright](https://playwright.dev/dotnet/) membuka halaman pencarian TikTok dan mengumpulkan tautan post sesuai tipe media. Aplikasi meminta metadata serta URL media dari TikWM, lalu mengunduh file dengan `HttpClient`.

Untuk video, aplikasi memilih URL HD jika tersedia. Jika tidak tersedia, aplikasi memakai URL video lain yang diberikan API.

## Batasan

- Struktur halaman TikTok dapat berubah dan membuat pencarian berhenti bekerja.
- TikTok dapat meminta CAPTCHA atau login.
- Proses download bergantung pada layanan TikWM.
- Post yang dihapus, privat, dibatasi wilayah, atau tidak tersedia dapat dilewati.
- Target download dapat tidak tercapai jika hasil pencarian atau API tidak menyediakan cukup post.

Gunakan aplikasi ini hanya untuk konten yang boleh kamu unduh. Patuhi hak cipta, privasi kreator, dan ketentuan layanan platform.

## Pengembangan

Menjalankan langsung dari source:

```powershell
dotnet run --project ".\src\TikTok Downloader.csproj"
```

Build harus selesai tanpa error atau warning:

```powershell
dotnet build ".\src\TikTok Downloader.csproj" -c Release
```

Source flow CLI berada di:

- `src/Program.cs`
- `src/KeywordDownloader.cs`
- `src/TikTok Downloader.bat`

## Repository

- Fork aktif: [thoriq1520/TikTok-Downloader](https://github.com/thoriq1520/TikTok-Downloader)
- Proyek upstream: [Jettcodey/TikTok-Downloader](https://github.com/Jettcodey/TikTok-Downloader)

Perubahan pada fork ini mempertahankan kredit dan lisensi proyek asli.

## Lisensi

Proyek memakai [MIT License](LICENSE.txt). Hak cipta proyek asli tetap milik Jettcodey.
