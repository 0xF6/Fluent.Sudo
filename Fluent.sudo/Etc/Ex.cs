namespace Fluent.sudo.Etc
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Ex
    {
        public static Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<object>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(null);
            if (cancellationToken != default(CancellationToken))
                cancellationToken.Register(tcs.SetCanceled);
            return tcs.Task;
        }

        public static FileInfo AsFile(this string str) => new FileInfo(str);

        public static T When<T>(this FileInfo info, Func<FileInfo, bool> condition, Func<FileInfo, T> actor) 
            => condition(info) ? actor(info) : default;
        /// <summary>
        /// Escape text
        /// </summary>
        public static string E(this string s, char c) => $"{c}{s}{c}";
        public static string ReadAll(this FileInfo info) => File.ReadAllText(info.FullName);
    }


}