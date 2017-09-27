// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.IO.Compression;
using System.Net;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Roslyn.Test.Performance.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.CSharp
{
    [Collection(nameof(SharedIntegrationHostFixture2))]
    public class CSharpETW : AbstractIntegrationTest
    {
        private string TempDirectory = @"C:\temp\perf";

        public CSharpETW(VisualStudioInstanceFactory instanceFactory)
            : base(instanceFactory)
        {
        }

        [Fact]
        public void ETWTyping()
        {
            DownloadProject("Roslyn-csharp", 1, new ConsoleAndFileLogger());

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
            //System.Threading.Thread.Sleep(10000);
        }


        private void Verify(string word, string expectedKeyword)
        {
            VisualStudio.Editor.PlaceCaret(word, charsOffset: -1);
            Assert.Contains(expectedKeyword, VisualStudio.Editor.GetF1Keyword());
        }

        private void DownloadProject(string name, int version, ILogger logger)
        {
            var zipFileName = $"{name}.{version}.zip";
            var zipPath = Path.Combine(TempDirectory, zipFileName);
            // If we've already downloaded the zip, assume that it
            // has been downloaded *and* extracted.
            if (File.Exists(zipPath))
            {
                logger.Log($"Didn't download and extract {zipFileName} because one already exists at {zipPath}.");
                return;
            }

            // Remove all .zip files that were downloaded before.
            foreach (var path in Directory.EnumerateFiles(TempDirectory, $"{name}.*.zip"))
            {
                logger.Log($"Removing old zip {path}");
                File.Delete(path);
            }

            // Download zip file to temp directory
            var downloadTarget = $"https://dotnetci.blob.core.windows.net/roslyn-perf/{zipFileName}";
            logger.Log($"Downloading {downloadTarget}");
            var client = new WebClient();
            client.DownloadFile(downloadTarget, zipPath);
            logger.Log($"Done Downloading");

            // Extract to temp directory
            logger.Log($"Extracting {zipPath} to {TempDirectory}");
            ZipFile.ExtractToDirectory(zipPath, TempDirectory);
            logger.Log($"Done Extracting");
        }
    }
}