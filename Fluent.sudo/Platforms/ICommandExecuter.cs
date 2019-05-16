namespace Fluent.sudo.Platforms
{
    using System;
    using System.Threading.Tasks;

    public interface ICommandExecuter
    {
        Task<ExecuteResult> Execute(string cmd, TimeSpan timeoutWait);
    }
}