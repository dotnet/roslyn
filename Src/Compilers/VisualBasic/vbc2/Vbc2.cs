using Microsoft.CodeAnalysis.BuildTasks;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants.RequestLanguage;

namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
{
    class Vbc2
    {
        static int Main(string[] args)
            => BuildClient.RunWithConsoleOutput(args, VisualBasicCompile, Program.Main);
    }
}
