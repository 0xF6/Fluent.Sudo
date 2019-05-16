namespace Fluent.sudo.Platforms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Etc;
    using Microsoft.Extensions.Logging;

    public class Linux : ICommandExecuter
    {
        private readonly ILogger<Linux> log;
        public Linux() => log = Log.Get<Linux>();

        public async Task<ExecuteResult> Execute(string cmd, TimeSpan timeoutWait)
        {
            var bin_box = new[] {"/usr/bin/kdesudo", "/usr/bin/pkexec"}.Where(x => x.AsFile().Exists);

            if(!bin_box.Any())
                return new ExecuteResult(ElevateResult.UNK, "", "kdesudo/pkexec not found.", "-1");

            var bin = bin_box.First();

            var command = new List<string>();

            if (bin.Contains("kdesudo"))
            {
                var reason = "needs administrative privileges. Please enter your password.";
                command.Add($"--comment \"{Console.Title} {reason}\"");
                command.Add("--");
            }
            else if (bin.Contains("pkexec"))
                command.Add("--disable-internal-agent");
            command.Add($"\"{cmd}\"");

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo(bin, string.Join(" ", command))
                {
                    RedirectStandardOutput = true, RedirectStandardError = true
                }
            };


            await proc.WaitForExitAsync();

            var stderr = await proc.StandardError.ReadToEndAsync();
            var stdout = await proc.StandardOutput.ReadToEndAsync();

            var result = ElevateResult.UNK;

            if (stderr.Contains("Request dismissed") || stderr.Contains("Command failed"))
                result = ElevateResult.PERMISSION_DENIED;
            else if (stderr.Any())
                result = ElevateResult.ERROR;
            else
                result = ElevateResult.SUCCESS;
            return new ExecuteResult(result, stdout, stderr, proc.ExitCode.ToString());
        }
    }
}