namespace SudoTest
{
    using System.Threading.Tasks;
    using Fluent.sudo;
    using static System.Console;
    using static ObjectDumper;

    internal class Program
    {
        public static async Task Main(string[] args)
        {
            var result = await Sudo.Exec("echo test");

            WriteLine(Dump(result));
        }
    }
}
