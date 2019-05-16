namespace Fluent.sudo.Platforms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Text;
    using System.Threading.Tasks;
    using Etc;
    using Microsoft.Extensions.Logging;
    using MoreLinq;

    public class Windows : ICommandExecuter
    {
        private readonly ILogger<Windows> log;
        public Windows() => log = Log.Get<Windows>();

        public async Task<ExecuteResult> Execute(string cmd, TimeSpan timeoutWait)
        {
            var tmp = Path.GetTempPath();
            var uid = Guid.NewGuid().ToString();
            var instanceID = Path.Combine(tmp, uid);
            var cwd = Directory.GetCurrentDirectory();


            using (log.BeginScope("info"))
            {
                log.LogInformation($"Temp Dir    : {tmp}");
                log.LogInformation($"UID Instance: {uid}");
                log.LogInformation($"Current Dir : {cwd}");
            }

            var execute = Path.Combine(instanceID, "execute.bat");
            var command = Path.Combine(instanceID, "command.bat");

            var stdout = Path.Combine(instanceID, "_.stdout");
            var stderr = Path.Combine(instanceID, "_.stderr");
            var status = Path.Combine(instanceID, "_.status");

            void cleanUp()
            {
                Directory.GetFiles(instanceID).Pipe(File.Delete).ForEach(x => log.LogTrace($"Remove '{x}'."));
                Directory.Delete(instanceID, true);
            }
            Directory.CreateDirectory(instanceID);


            void WriteExecute()
            {
                var build = new StringBuilder();

                build.AppendLine("@echo off");
                build.AppendLine($"call \"{command}\" > \"{stdout}\" 2> \"{stderr}\"");
                build.AppendLine($"(echo %ERRORLEVEL%) > \"{status}\"");

                File.WriteAllText(execute, build.ToString());
            }
            void WriteCommand()
            {
                var build = new StringBuilder();

                build.AppendLine("@echo off");
                build.AppendLine($"chcp 65001>nul");
                build.AppendLine($"cd /d \"{cwd}\"");
                build.AppendLine(cmd);

                File.WriteAllText(command, build.ToString());
            }


            async Task<ElevateResult> Elevate()
            {
                var build = new List<string>();
                
                build.Add("powershell.exe");
                build.Add("Start-Process");
                build.Add("-FilePath");
                build.Add($"{execute}");
                build.Add("-WindowStyle hidden");
                build.Add("-Verb runAs");


                var info = new ProcessStartInfo("cmd", $"/c \"{string.Join(" ", build)}\"")
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                var proc = new Process { StartInfo = info };
                proc.Start();

                await proc.WaitForExitAsync();
                var err = await proc.StandardError.ReadToEndAsync();
                var @out = await proc.StandardOutput.ReadToEndAsync();

                if (err.Contains("canceled by the user"))
                    return ElevateResult.PERMISSION_DENIED;
                if (!err.Any()) return ElevateResult.SUCCESS;

                File.WriteAllText(stderr, err);
                return ElevateResult.ERROR;
            }



            WriteExecute();
            WriteCommand();

            var elevateStatus = await Elevate();

            async Task WaitForStatus()
            {
                var info = new FileInfo(status);
                var startDate = DateTimeOffset.UtcNow;

                while (!info.Exists || info.Length <= 2)
                {
                    await Task.Delay(300);
                    var outInfo = new FileInfo(stdout);
                    if (!outInfo.Exists)
                    {
                        elevateStatus = ElevateResult.PERMISSION_DENIED;
                        break;
                    }

                    if (DateTimeOffset.UtcNow - startDate > timeoutWait)
                    {
                        elevateStatus = ElevateResult.TIMEOUT;
                        break;
                    }
                    info = new FileInfo(status);
                }
            }



            await WaitForStatus();


            Expression<Func<string, string>> getData =
                s => s.AsFile().When(x => x.Exists, x => x.ReadAll());

            var exp = getData.Compile();

            var result = new ExecuteResult(elevateStatus,
                exp(stdout),
                exp(stderr),
                exp(status));



            cleanUp();
            return result;
        }
    }

    public class ExecuteResult
    {
        public ExecuteResult(ElevateResult status, string stdout, string stderr, string code)
        {
            this.Code = code;
            this.Status = status;
            this.Stderr = stderr;
            this.Stdout = stdout;
        }
        public ElevateResult Status { get; set; }
        public string Code { get; set; }
        public string Stdout { get; set; }
        public string Stderr { get; set; }
    }

    public enum ElevateResult
    {
        UNK,
        PERMISSION_DENIED,
        ERROR,
        TIMEOUT,
        SUCCESS
    }
}