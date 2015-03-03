using Microsoft.CodeAnalysis.BuildTasks;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants.RequestLanguage;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    class Csc2
    {
        static int Main(string[] args)
            => BuildClient.RunWithConsoleOutput(args, CSharpCompile, Program.Main);
    }
}
