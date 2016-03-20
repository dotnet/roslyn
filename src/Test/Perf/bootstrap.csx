#load "./util/test_util.csx"

using System.IO;

InitUtilities();

ShellOutVital("msbuild", "./Roslyn.sln /p:Configuration=Release", workingDirectory: RoslynDirectory());

string from = BinReleaseDirectory();
string to = Path.Combine(BinDirectory(), "PerfBootstrap");
System.IO.Directory.Move(from, to);
