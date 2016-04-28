#load "test_util.csx"

var binaries = new RelativeDirectory().MyBinaries();
var vsixes = new[]
{
    "Roslyn.VisualStudio.Setup.vsix",
    "Roslyn.VisualStudio.Test.Setup.vsix",
    "Microsoft.VisualStudio.VsInteractiveWindow.vsix",
    "Roslyn.VisualStudio.InteractiveComponents.vsix",
    "Roslyn.VisualStudio.Setup.Interactive.vsix",
    "Roslyn.Compilers.Extension.vsix",
    "Microsoft.VisualStudio.LanguageServices.Telemetry.vsix"
};

var installer = Path.Combine(binaries, "VSIXExpInstaller.exe");

foreach (var vsix in vsixes)
{
    ShellOutVital(installer, $"/rootSuffix:RoslynPerf {vsix}", binaries, System.Threading.CancellationToken.None);
}


