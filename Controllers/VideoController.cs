using Microsoft.AspNetCore.Mvc;
using FFMpegCore; // 使用 FFMpegCore
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;

namespace HlsServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VideoController> _logger;

        public VideoController(IWebHostEnvironment environment, ILogger<VideoController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("process")]
        [RequestSizeLimit(524288000)] // 500 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        public async Task<IActionResult> ProcessVideo(IFormFile video)
        {
            if (video == null || video.Length == 0)
            {
                return BadRequest(new { error = "請上傳影片檔案" });
            }

            try
            {
                // 建立資料夾路徑
                var uploadFolderPath = Path.Combine(_environment.ContentRootPath, "uploads");
                var streamFolderPath = Path.Combine(_environment.ContentRootPath, "streams");

                // 如果資料夾不存在，就建立它們
                Directory.CreateDirectory(uploadFolderPath);
                Directory.CreateDirectory(streamFolderPath);

                // 生成唯一的檔案名稱
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var uniqueFileName = $"{timestamp}_{video.FileName}";
                var uploadPath = Path.Combine(uploadFolderPath, uniqueFileName);
                var outputPath = Path.Combine(streamFolderPath, $"output_{timestamp}.m3u8");

                // 保存上傳的檔案
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await video.CopyToAsync(stream);
                }

                // 使用 FFMpegCore 處理影片
                await FFMpegArguments
                    .FromFileInput(uploadPath)
                    .OutputToFile(outputPath, false, options => options
                        .WithCustomArgument("-profile:v baseline")
                        .WithCustomArgument("-level 3.0")
                        .WithCustomArgument("-hls_list_size 0")
                        .WithCustomArgument("-hls_time 10")
                        .WithCustomArgument("-f hls"))
                    .ProcessAsynchronously();

                // 處理完成後刪除上傳的原始檔案
                System.IO.File.Delete(uploadPath);

                return Ok(new
                {
                    success = true,
                    streamUrl = $"/streams/output_{timestamp}.m3u8"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "處理影片時發生錯誤");
                return StatusCode(500, new { error = "處理影片時發生錯誤" });
            }
        }
    }
}