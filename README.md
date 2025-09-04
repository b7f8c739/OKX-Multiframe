# OKX Multichart

A lightweight **multi-frame browser** built with **.NET 6 WinForms** and
**WebView2**, designed for OKX charts and web3 dashboards.\
It opens 3 horizontal frames by default, remembers window size and
last-visited URLs, and runs fully portable.

------------------------------------------------------------------------

## ✨ Features

-   **3 horizontal WebView2 frames** (multi-chart view)
-   **Dark mode title bar**
-   **Portable profiles** (`.settings.json` beside the .exe)
-   **Saves window size and last visited URLs** on exit
-   **Embedded WebView2Loader.dll** → no external dependencies
-   **Custom app icon support**

------------------------------------------------------------------------

## 📦 Build

1.  Install **.NET 6 SDK** (or newer).

2.  Clone/download this project.

3.  Restore packages:

    ``` bash
    dotnet restore
    ```

4.  Build & run in debug:

    ``` bash
    dotnet build
    dotnet run
    ```

5.  Publish as a **single portable exe**:

    ``` bash
    dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
    ```

Output will be in:

    bin\Release\net6.0-windows\win-x64\publish\

------------------------------------------------------------------------

## 🚀 Usage

-   Launch `OKXMultichart.exe`
-   On first close, a file `OKXMultichart.settings.json` will be created
    beside the exe
-   Edit/delete this file to reset window size or saved URLs
-   A folder `OKXMultichart.WebView2Cache` will also be created beside
    the exe (stores cookies/cache)

------------------------------------------------------------------------

## 📂 Files

-   `OKXMultichart.cs` → Main source file\
-   `OKXMultichart.csproj` → Project definition\
-   `app.ico` → Application icon\
-   `OKXMultichart.settings.json` → User settings (created at runtime)\
-   `WebView2Loader.dll` → Extracted automatically on first run

------------------------------------------------------------------------

## ⚠️ Notes

-   Requires **Windows 10+** (WebView2 only runs on Windows).
-   Linux/macOS support would require Avalonia/CEF port.\
-   Portable build runs without installation --- just copy the exe
    folder.

------------------------------------------------------------------------

Made with ❤️ by Lumo AI, ChatGPT, VSC GPT-o4.1.
