using Microsoft.AspNetCore.Mvc;
using FFMpegCore;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace HlsServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VideoController> _logger;

        private static readonly ConcurrentDictionary<string, object> _jobOutcomes = new();

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

            var uploadFolderPath = Path.Combine(_environment.ContentRootPath, "uploads");
            var streamFolderPath = Path.Combine(_environment.ContentRootPath, "streams");
            Directory.CreateDirectory(uploadFolderPath);
            Directory.CreateDirectory(streamFolderPath);

            var jobId = Guid.NewGuid().ToString();
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var uniqueFileName = $"{timestamp}_{video.FileName}";
            var uploadPath = Path.Combine(uploadFolderPath, uniqueFileName);
            var outputPath = Path.Combine(streamFolderPath, $"output_{timestamp}.m3u8");
            var streamUrl = $"/streams/output_{timestamp}.m3u8";

            using (var stream = new FileStream(uploadPath, FileMode.Create))
            {
                await video.CopyToAsync(stream);
            }

            // Run FFMpeg in the background
            Task.Run(async () =>
            {
                try
                {
                    await FFMpegArguments
                        .FromFileInput(uploadPath)
                        .OutputToFile(outputPath, false, options => options
                            .WithCustomArgument("-profile:v baseline")
                            .WithCustomArgument("-level 3.0")
                            .WithCustomArgument("-hls_list_size 0")
                            .WithCustomArgument("-hls_time 10")
                            .WithCustomArgument("-f hls"))
                        .ProcessAsynchronously();

                    _jobOutcomes[jobId] = new { success = true, streamUrl = streamUrl };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Job {jobId}: 處理影片時發生錯誤");
                    _jobOutcomes[jobId] = new { success = false, error = "處理影片時發生錯誤" };
                }
                finally
                {
                    if (System.IO.File.Exists(uploadPath))
                    {
                        System.IO.File.Delete(uploadPath);
                    }
                }
            });

            return Accepted(new { jobId });
        }

        [HttpGet("progress/{jobId}")]
        public IActionResult GetProgress(string jobId)
        {
            if (_jobOutcomes.TryGetValue(jobId, out var outcome))
            {
                _jobOutcomes.TryRemove(jobId, out _);
                return Ok(outcome);
            }

            // Job is still running, but we don't have percentage. 
            // Return a generic "in progress" status.
            return Ok(new { inProgress = true });
        }
    }
}