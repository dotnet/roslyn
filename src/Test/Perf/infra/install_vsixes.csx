#load "test_util.csx"

var binariesDirectory = new RelativeDirectory().MyBinaries();
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

var installer = Path.Combine(binariesDirectory, "VSIXExpInstaller.exe");

foreach (var vsix in vsixes)
{
    ShellOutVital(installer, $"/rootSuffix:RoslynPerf {vsix}", binariesDirectory, System.Threading.CancellationToken.None);
}


