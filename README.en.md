<!--
##########################################
#           TikTok Downloader            #
#           Made by Jettcodey             #
#                © 2024                  #
#           DO NOT REMOVE THIS           #
##########################################
-->

# TikTok Keyword Downloader

[Bahasa Indonesia](README.md) | English

A Windows CLI fork that searches TikTok posts by keyword, then downloads videos or photos from the search results.

This fork replaces the upstream WinForms flow with a shorter terminal prompt:

1. Enter a keyword.
2. Select Video or Photo.
3. Set the number of posts.
4. Complete a CAPTCHA or sign in through the browser if TikTok asks you to.
5. Wait until the files are saved in the `downloads` folder.

## Download

Download the Windows x64 package from the [Releases](https://github.com/thoriq1520/TikTok-Downloader/releases/latest) page. Extract the ZIP file, then run `TikTok Downloader.bat`.

The release package is self-contained and includes the .NET runtime required by the application.

## Features

- Searches TikTok posts by keyword.
- Lets you choose video or photo results.
- Downloads 10 posts when the amount is left blank.
- Accepts an amount from 1 to 500 posts.
- Opens Chrome, Edge, or Brave to collect search results.
- Names downloaded files from their captions.
- Adds an image sequence number to files from photo posts.
- Stops the process when you press `Ctrl+C`.

The requested amount is counted by post. One photo post can produce several image files.

## Requirements

- Windows 10 or later, x64.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for building from source.
- Google Chrome, Microsoft Edge, or Brave.
- An internet connection.

## Build

Clone this fork:

```powershell
git clone https://github.com/thoriq1520/TikTok-Downloader.git
cd TikTok-Downloader
```

Build the Release configuration:

```powershell
dotnet build ".\src\TikTok Downloader.csproj" -c Release
```

The build output is located at:

```text
src\bin\Release\net8.0-windows10.0.17763.0\
```

This folder contains the .NET executable and the `TikTok Downloader.bat` launcher.

## Running the application

The simplest option is to run the BAT launcher from the source folder:

```powershell
& ".\src\TikTok Downloader.bat"
```

The launcher looks for a Release build. If the EXE is not available, it builds the application first.

You can also run the launcher directly from the build output folder:

```powershell
& ".\src\bin\Release\net8.0-windows10.0.17763.0\TikTok Downloader.bat"
```

To display help:

```powershell
& ".\src\TikTok Downloader.bat" --help
```

The application asks for the following input:

```text
Keyword pencarian: cats
Pilih media [1] Video  [2] Foto: 1
Jumlah post [10]: 5
```

The browser stays open during the search. If TikTok displays a CAPTCHA or sign-in page, complete it in that browser. The application continues the search automatically.

## Download location and filenames

Files are saved relative to the launcher location:

```text
downloads\
└── <keyword>\
    ├── videos\
    │   └── <caption>.mp4
    └── photos\
        ├── <caption>_01.jpg
        └── <caption>_02.jpg
```

Characters that are invalid in Windows filenames are removed from the caption. If the same filename already exists, the application adds a sequence number so the existing file is not overwritten.

## How it works

[Microsoft Playwright](https://playwright.dev/dotnet/) opens the TikTok search page and collects post links that match the selected media type. The application requests metadata and media URLs from TikWM, then downloads the files with `HttpClient`.

For videos, the application prefers an HD URL when one is available. Otherwise, it uses another video URL provided by the API.

## Limitations

- Changes to the TikTok page structure may stop search from working.
- TikTok may require a CAPTCHA or sign-in.
- Downloads depend on the TikWM service.
- Deleted, private, region-restricted, or unavailable posts may be skipped.
- The requested download count may not be reached when the search results or API do not provide enough posts.

Only use this application for content you are allowed to download. Respect copyright, creator privacy, and the platform's terms of service.

## Development

Run directly from source:

```powershell
dotnet run --project ".\src\TikTok Downloader.csproj"
```

The build should finish without errors or warnings:

```powershell
dotnet build ".\src\TikTok Downloader.csproj" -c Release
```

The CLI flow source is located in:

- `src/Program.cs`
- `src/KeywordDownloader.cs`
- `src/TikTok Downloader.bat`

## Repositories

- Active fork: [thoriq1520/TikTok-Downloader](https://github.com/thoriq1520/TikTok-Downloader)
- Upstream project: [Jettcodey/TikTok-Downloader](https://github.com/Jettcodey/TikTok-Downloader)

This fork preserves the attribution and license of the original project.

## License

This project uses the [MIT License](LICENSE.txt). Copyright for the original project remains with Jettcodey.
