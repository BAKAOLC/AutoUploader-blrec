using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NLog;

namespace AutoUploader
{
    public static class WebHookHandler
    {
        private static readonly Logger Logger = CoreLogger.GetLogger("WebHook");
        private static readonly List<string> UploadFiles = new();

        private static IEnumerable<string> EventList { get; } = new[]
        {
            "LiveBeganEvent",
            "LiveEndedEvent",
            "RoomChangeEvent",
            "RecordingStartedEvent",
            "RecordingFinishedEvent",
            "RecordingCancelledEvent",
            "VideoFileCreatedEvent",
            "VideoFileCompletedEvent",
            "DanmakuFileCreatedEvent",
            "DanmakuFileCompletedEvent",
            "RawDanmakuFileCreatedEvent",
            "RawDanmakuFileCompletedEvent",
            "VideoPostprocessingCompletedEvent",
            "SpaceNoEnoughEvent",
            "Error"
        };

        //to Regex
        private static Regex FilePathRegex =>
            new(
                @"【(?<uname>.+)】(?<title>.+) \((?<year>\d+)年(?<month>\d+)月(?<day>\d+)日(?<hour>\d+)时(?<minute>\d+)分(?<second>\d+)秒\)",
                RegexOptions.Compiled);

        public static bool Handle(string body)
        {
            JObject json = JObject.Parse(body);
            Logger.Info("received: {Data}", json.ToString());
            string? eventType = json.Value<string>("type");
            if (!EventList.Contains(eventType))
            {
                return false;
            }

            switch (eventType)
            {
                case "VideoPostprocessingCompletedEvent":
                    long roomId = json["data"]!.Value<long>("room_id");
                    string filePath = json["data"]!.Value<string>("path")!;
                    Match match = FilePathRegex.Match(filePath);
                    if (!match.Success)
                    {
                        return false;
                    }

                    string author = match.Groups["uname"].Value;
                    string title = match.Groups["title"].Value;
                    string year = match.Groups["year"].Value;
                    string month = match.Groups["month"].Value;
                    string day = match.Groups["day"].Value;
                    string hour = match.Groups["hour"].Value;
                    string minute = match.Groups["minute"].Value;
                    string second = match.Groups["second"].Value;
                    string uploadTitle = $"【{author}】{title} {year}{month}{day}{hour}{minute}{second}";
                    string cover = Path.Combine(Path.GetDirectoryName(filePath)!,
                                                Path.GetFileNameWithoutExtension(filePath) + ".jpg");
                    UploadVideo(roomId, author, uploadTitle, filePath, cover);
                    break;
            }

            return true;
        }

        private static void UploadVideo(long roomId, string author, string title, string file, string cover)
        {
            lock (UploadFiles)
            {
                if (UploadFiles.Contains(file))
                {
                    Logger.Warn("文件已在上传队列中，跳过");
                    return;
                }
                UploadFiles.Add(file);
            }

            string guid = Guid.NewGuid().ToString();
            Logger.Info("发起视频上传任务({Guid}) [等待1分钟以避免blrec的多次调用]", guid);
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                Logger logger = CoreLogger.GetLogger($"UploadTask-{guid}");
                logger.Info("房间号：{RoomId}", roomId);
                logger.Info("用户：{Author}", author);
                logger.Info("标题：{Title}", title);
                logger.Info("封面：{Cover}", cover);
                logger.Info("文件：{File}", file);
                try
                {
                    Process process = new();
                    process.StartInfo.FileName = "biliup";
                    process.StartInfo.Arguments =
                        $@"upload --title ""{title}"" --tid 65 --cover ""{cover}"" --desc ""直播间地址：https://live.bilibili.com/{roomId}"" --tag ""{author},直播回放,虚拟主播,vup,vtuber"" ""{file}""";
                    process.StartInfo.UseShellExecute        =  false;
                    process.StartInfo.RedirectStandardOutput =  true;
                    process.StartInfo.RedirectStandardError  =  true;
                    process.OutputDataReceived               += (_, args) => logger.Info(args.Data);
                    process.ErrorDataReceived                += (_, args) => logger.Error(args.Data);
                    logger.Info("开始上传");
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    logger.Info("上传任务结束");
                }
                catch (Exception e)
                {
                    logger.Error(e, "上传任务失败");
                }

                lock (UploadFiles)
                {
                    UploadFiles.Remove(file);
                }
            });
        }
    }
}
