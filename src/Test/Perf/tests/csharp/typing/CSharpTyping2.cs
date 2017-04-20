using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Performance.Utilities;
using Roslyn.VisualStudio.IntegrationTests;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Tests.csharp.typing
{
    class CSharpTyping2 : PerfTest
    {
        private const string _rootSuffix = "RoslynPerf";
        private static readonly string _nugetPackagesPath = System.Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
            Path.Combine(System.Environment.GetEnvironmentVariable("UserProfile"), ".nuget", "packages");
        private static readonly string _installerPath = Path.Combine(_nugetPackagesPath, "roslyntools.microsoft.vsixexpinstaller", "0.4.0-beta", "tools", "vsixexpinstaller.exe");

        public override string Name => "CSharp Typing";

        public override string MeasuredProc => null;

        public override bool ProvidesScenarios => false;

        public override string[] GetScenarios() => null;
        public override void Setup()
        {
            DownloadProject("Roslyn-csharp", 1, new ConsoleAndFileLogger());
        }

        private void InstallVsixes()
        {
            var vsix1 = Path.Combine(MyBinaries(), "Vsix", "VisualStudioSetup", "Roslyn.VisualStudio.Setup.vsix");
            var vsix2 = Path.Combine(MyBinaries(), "Vsix", "VisualStudioSetup.Next", "Roslyn.VisualStudio.Setup.Next.vsix");

            var rootSuffixArg = $"/rootsuffix:{_rootSuffix}";

            // First uninstall any vsixes in the hive
            ShellOutVital(_installerPath, $"{rootSuffixArg}, /uninstallAll");

            // Then install the RoslynDeployment.vsix we just built
            ShellOutVital(_installerPath, $"{rootSuffixArg} {vsix1}");
            ShellOutVital(_installerPath, $"{rootSuffixArg} {vsix2}");
        }

        public override void Test()
        {
            var factory = new VisualStudioInstanceFactory();
            var instance = factory.GetNewOrUsedInstance(SharedIntegrationHostFixture.RequiredPackageIds);

            var VisualStudio = instance.Instance;

            VisualStudio.SolutionExplorer.OpenSolution(@"C:\temp\perf\RoslynSolutions\Roslyn-CSharp.sln");
            ETWActions.ForceGC(VisualStudio);
            //VisualStudio.SolutionExplorer.OpenFile(new Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils.Project(@"CSharpCompiler.mod"), "LanguageParser.cs");
            //VisualStudio.Editor.NavigateToSendKeys("LanguageParser.cs");
            VisualStudio.ExecuteCommand("File.OpenFile", @"C:\temp\perf\RoslynSolutions\Source\Compilers\CSharp\Source\Parser\LanguageParser.cs");
            ETWActions.StartETWListener(VisualStudio);
            ETWActions.WaitForSolutionCrawler(VisualStudio);
            ETWActions.ForceGC(VisualStudio);
            VisualStudio.ExecuteCommand("Edit.GoTo", "9524");
            ETWActions.WaitForIdleCPU();
            //ETWActions.WaitForSolutionCrawler(VisualStudio);
            //System.Windows.Forms.MessageBox.Show("Attach debugger!");
            using (DelayTracker.Start(@"C:\typingResults\", @"C:\roslyn-internal\open\binaries\release\dlls\PerformanceTestUtilities\TypingDelayAnalyzer.exe", "typing"))
            {
                VisualStudio.Editor.PlayBackTyping(@"C:\roslyn-internal\Closed\Test\PerformanceTests\Perf\tests\CSharp\TypingInputs\CSharpGoldilocksInput-MultipliedDelay.txt");
            }
            //System.Windows.Forms.MessageBox.Show("Done waiting!");
            ETWActions.StopETWListener(VisualStudio);
        }
    }
}

TestThisPlease(
    new CSharpTyping2(
        "CSharpPerfGoldilocksTypingFullSolutionDiagnosticsMultipliedDelayTemplate.xml",
        new string[] { }));

