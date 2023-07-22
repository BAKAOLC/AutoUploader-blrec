using System.Diagnostics;
using System.Text;
using AutoUploader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

Console.Title = "Auto Uploader";
DateTime launchTime = DateTime.Now;
Logger mainLogger = CoreLogger.GetLogger("Main");

AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    DateTime now = DateTime.Now;
    StringBuilder sb = new StringBuilder()
                      .AppendLine("！！！程序发生未捕获异常，程序即将崩溃！！！")
                      .AppendLine($"启动于 {launchTime:yyyy-MM-dd HH:mm:ss.ffff}")
                      .AppendLine($"崩溃于 {now:yyyy-MM-dd HH:mm:ss.ffff}")
                      .Append($"工作时长 {now - launchTime}");
    mainLogger.Fatal(args.ExceptionObject as Exception, sb.ToString());
};

Process currentProcess = Process.GetCurrentProcess();
_ = new Mutex(true, currentProcess.ProcessName, out bool isFirst);
if (isFirst)
{
    mainLogger.Info("程序启动");
    await BeginService().ConfigureAwait(true);
    mainLogger.Info("程序结束");
    CoreLogger.Flush();
}
else
{
    Console.WriteLine("请勿重复启动程序实例，以免发生异常");
}

if (!Console.IsInputRedirected)
{
    Console.WriteLine("程序主逻辑已结束，按任意键结束程序");
    Console.ReadKey(true);
}
else
{
    Console.WriteLine("程序主逻辑已结束");
}

static Task BeginService()
{
    IHostBuilder hb = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
                          .ConfigureLogging(builder =>
                           {
                               builder.SetMinimumLevel(LogLevel.None);
                               builder.AddNLog();
                           })
                          .ConfigureHostConfiguration(hostConfig =>
                           {
                               hostConfig.SetBasePath(Directory.GetCurrentDirectory());
                               hostConfig.AddJsonFile("config.json", true);
                               hostConfig.AddEnvironmentVariables("AUTO_UPLOADER_");
                               hostConfig.AddCommandLine(Environment.GetCommandLineArgs());
                           })
                          .ConfigureServices(service => { service.AddHostedService<WebHookServerService>(); });
    return hb.RunConsoleAsync();
}
