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

        // 用於儲存已完成任務的最終結果
        private static readonly ConcurrentDictionary<string, object> _jobOutcomes = new();

        // 用於儲存正在進行中任務的即時進度
        private static readonly ConcurrentDictionary<string, int> _jobProgress = new();

        // 用於儲存正在進行中任務的即時進度
        private static readonly ConcurrentDictionary<string, List<ProgressPoint>> _jobProgressPointList = new();

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
            _ = Task.Run(async () =>
            {
                try
                {
                    //  初始化進度為 0
                    _jobProgress[jobId] = 0;

                    //  初始化進度節點集合
                    _jobProgressPointList[jobId] = new List<ProgressPoint>
                    {
                        new ProgressPoint(0, DateTime.Now)
                    };

                    // 先用 FFProbe 取得總時長（秒）
                    double totalSeconds = 0;
                    try
                    {
                        var mediaInfo = await FFProbe.AnalyseAsync(uploadPath);
                        totalSeconds = mediaInfo?.Duration.TotalSeconds ?? 0;
                        if (totalSeconds <= 0)
                        {
                            _logger.LogWarning("無法取得媒體總時長，進度將回傳 -1 表示未知");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "FFProbe 取得時長發生例外，仍會嘗試轉檔但無法計算百分比");
                    }

                    await FFMpegArguments
                        .FromFileInput(uploadPath)
                        .OutputToFile(outputPath, false, options => options
                            .WithCustomArgument("-profile:v baseline")
                            .WithCustomArgument("-level 3.0")
                            .WithCustomArgument("-hls_list_size 0")
                            .WithCustomArgument("-hls_time 10")
                            .WithCustomArgument("-f hls"))
                        .NotifyOnProgress((progress) =>
                        {
                            // 更新即時進度
                            _jobProgress[jobId] = (int)Math.Floor((progress.TotalSeconds / totalSeconds) * 100);

                            // 更新即時進度節點集合
                            _jobProgressPointList[jobId].Add(new ProgressPoint(_jobProgress[jobId], DateTime.Now));

                            _logger.LogInformation($"Job {jobId}: 進度 {progress}%");
                        })
                        .ProcessAsynchronously();

                    // 儲存最終結果
                    _jobOutcomes[jobId] = new { success = true, streamUrl = streamUrl };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Job {jobId}: 處理影片時發生錯誤");
                    // 儲存錯誤結果
                    _jobOutcomes[jobId] = new { success = false, error = "處理影片時發生錯誤" };
                }
                finally
                {
                    // 任務結束後，從進度字典中移除
                    _jobProgress.TryRemove(jobId, out _);
                    
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
            // 檢查任務是否已有最終結果
            if (_jobOutcomes.TryGetValue(jobId, out var outcome))
            {
                _jobOutcomes.TryRemove(jobId, out _); // 取得結果後就移除
                return Ok(outcome);
            }

            // 如果沒有最終結果，則檢查是否有即時進度
            if (_jobProgress.TryGetValue(jobId, out var progress))
            {
                var result = ProgressEstimator.EstimateRemaining(_jobProgressPointList[jobId]);
                if (result.Success)
                {
                    Console.WriteLine($"Remaining: {result.Remaining}, ETA: {result.Eta}, rate(pct/sec)={result.RatePerSecond:F6}");
                }
                else
                {
                    Console.WriteLine($"Cannot estimate: {result.ErrorMessage}");
                }

                return Ok(new { inProgress = true, progress = progress, remaining = ProgressEstimator.TruncateRobust(result.Remaining.ToString() ?? "00:00:00") });
            }

            // 如果連進度都還沒有，代表可能剛開始
            return Ok(new { inProgress = true, progress = 0 });
        }
    }
}