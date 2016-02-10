#load "../util/runner_util.csx"
#load "../util/test_util.csx"
using System.IO;

InitUtilities();

var pathToCsc = Path.Combine(ReleaseCscPath());
var pathToHelloWorld = Path.Combine(MyWorkingDirectory(), "HelloWorld.cs");
var pathToOutput = Path.Combine(MyArtifactsDirectory(), "HelloWorld.exe");

ProcessResult result;

var msToCompile = WalltimeMs(out result,
    () => ShellOut(pathToCsc, pathToHelloWorld + " /out:" + pathToOutput));

if (result.Failed)
{
    LogProcessResult(result);
    return 1;
}

Report("compile duration (ms)", msToCompile);
return 0;
