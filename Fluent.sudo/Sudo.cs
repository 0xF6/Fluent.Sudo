namespace Fluent.sudo
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using Platforms;

    public static class Sudo
    {
        public static async Task<ExecuteResult> Exec(string cmd)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return await new Windows().Execute(cmd, TimeoutWait, TimeoutExecute);
            return null;
        }

        public static TimeSpan TimeoutWait { get; set; } = TimeSpan.FromSeconds(10);
        public static TimeSpan TimeoutExecute { get; set; } = TimeSpan.FromSeconds(20);
    }
}
