#load "..\util\test_util.csx"
#load "..\util\runner_util.csx"

InitUtilities();

// Update the repository
string branch = StdoutFrom("git", "rev-parse --abbrev-ref HEAD");
ShellOutVital("git", $"pull origin {branch}");
ShellOutVital(Path.Combine(RoslynDirectory(), "Restore.cmd"), "", workingDirectory: RoslynDirectory());

// Build Roslyn in Release Mode
ShellOutVital("msbuild", "./Roslyn.sln /p:Configuration=Release", workingDirectory: RoslynDirectory());

// Run run_and_report.csx
await RunFile(Path.Combine(MyWorkingDirectory(), "run_and_report.csx"));
