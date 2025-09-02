# HLS Video Streaming Server - Demo

A simple web server built with ASP.NET Core that allows users to upload video files, converts them to HLS (HTTP Live Streaming) format using FFmpeg, and provides a web interface to watch the stream.

---

# HLS 影片串流伺服器 - Demo

一個使用 ASP.NET Core 建立的簡易 Web 伺服器，允許使用者上傳影片檔案，並使用 FFmpeg 將其轉換為 HLS (HTTP Live Streaming) 格式，同時提供一個網頁介面來觀看串流。

---

## Features / 主要功能

-   **Video Upload:** Upload video files through a web interface.
-   **HLS Conversion:** Automatically converts uploaded videos into an HLS playlist (`.m3u8`) and video segments (`.ts`).
-   **Real-time Progress:** Track both upload and video processing progress in real-time using API polling.
-   **Web-based Player:** Stream the converted video directly in the browser using `hls.js`.

*   **影片上傳:** 透過網頁介面上傳影片檔案。
*   **HLS 轉檔:** 自動將上傳的影片轉換為 HLS 播放列表 (`.m3u8`) 與影片片段 (`.ts`)。
*   **即時進度:** 透過 API 輪詢來即時追蹤檔案上傳與影片處理的進度。
*   **網頁播放器:** 使用 `hls.js` 直接在瀏覽器中串流播放轉檔後的影片。

---

## Tech Stack / 技術堆疊

-   **Backend:** ASP.NET Core 6.0
-   **Video Processing:** [FFmpeg](https://ffmpeg.org/) (via `FFMpegCore` wrapper)
-   **Frontend:** HTML, CSS, JavaScript
-   **HLS Playback:** `hls.js`

---

## Prerequisites / 環境要求

Before you begin, ensure you have the following installed on your system.
在開始之前，請確保您的系統上已安裝以下軟體。

1.  **.NET 6.0 SDK** (or later)
    -   [Download .NET](https://dotnet.microsoft.com/download)
2.  **FFmpeg**
    -   [Download FFmpeg](https://ffmpeg.org/download.html)
    -   **Important:** After installation, you must add the directory containing `ffmpeg.exe` to your system's `PATH` environment variable so the application can find it.
    -   **重要提示:** 安裝後，您必須將包含 `ffmpeg.exe` 的目錄加到您系統的 `PATH` 環境變數中，應用程式才能找到它。

---

## Setup & Installation / 安裝與設定

Follow these steps to get the project running on your local machine.
請依照以下步驟在您的本機上執行此專案。

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/mike-hsieh-tw/AspNetCore-HLS-Demo.git
    cd HlsServer
    ```

2.  **Restore .NET dependencies:**
    This command downloads and installs all the necessary NuGet packages defined in the `.csproj` file.
    此命令會下載並安裝 `.csproj` 檔案中定義的所有必要的 NuGet 套件。
    ```bash
    dotnet restore
    ```

3.  **Build the project:**
    This command compiles the application and checks for any build errors.
    此命令會編譯應用程式並檢查是否有任何建置錯誤。
    ```bash
    dotnet build
    ```

4.  **Run the application:**
    This command starts the web server.
    此命令會啟動網頁伺服器。
    ```bash
    dotnet run
    ```

By default, the server will be accessible at the URLs specified in the console output (e.g., `https://localhost:7112` and `http://localhost:5259`).
預設情況下，伺服器將在主控台輸出中指定的 URL 上提供服務 (例如 `https://localhost:7112` 和 `http://localhost:5259`)。

---

## Usage / 如何使用

<img width="1003" height="1012" alt="image" src="https://github.com/user-attachments/assets/abab0e9d-365d-4cf0-bc98-605bce0ad773" />

1.  Open your web browser and navigate to the application's URL (e.g., `https://localhost:7112`).
    打開您的網頁瀏覽器並前往應用程式的 URL (例如 `https://localhost:7112`)。

2.  You will see a simple interface with an upload section.
    您會看到一個包含上傳區塊的簡單介面。

3.  Click the file input area to select a video file from your computer.
    點擊檔案選擇區域以從您的電腦中選擇一個影片檔案。

4.  Click the **"Upload and Process"** button.
    點擊 **「上傳並處理」** 按鈕。

5.  The UI will display two progress bars:
    -   **Upload Progress:** A blue bar showing the file upload status.
    -   **Processing Progress:** A green bar showing the HLS conversion status.
    介面上將會顯示兩個進度條：
    -   **上傳進度:** 藍色進度條，顯示檔案上傳狀態。
    -   **處理進度:** 綠色進度條，顯示 HLS 轉檔狀態。

6.  Once processing is complete, the stream URL will be displayed, and the video player will automatically load and play the HLS stream.
    處理完成後，畫面上會顯示串流網址，且影片播放器將自動載入並開始播放 HLS 串流。

---

## Project Structure / 專案結構

-   `HlsServer.csproj`: The main project file, defines dependencies and project properties. / 主要專案檔，定義相依套件與專案屬性。
-   `Program.cs`: The application's entry point, where services and middleware are configured. / 應用程式進入點，設定服務與中介軟體。
-   `/Controllers`: Contains the API controllers. / 包含 API 控制器。
    -   `VideoController.cs`: Handles file uploads (`/api/video/process`) and progress polling (`/api/video/progress/{jobId}`). / 處理檔案上傳與進度輪詢的請求。
-   `/wwwroot`: Contains static frontend files. / 包含靜態前端檔案。
    -   `index.html`: The single-page user interface. / 單頁式使用者介面。
-   `/streams`: (Auto-created) Stores the generated HLS playlists (`.m3u8`) and video segments (`.ts`). / (自動建立) 存放產生的 HLS 播放列表與影片片段。
-   `/uploads`: (Auto-created) Temporarily stores the uploaded video files before they are processed. / (自動建立) 暫存上傳的影片檔案。

---

## License / 授權

This project is licensed under the MIT License.
本專案採用 MIT 授權。
