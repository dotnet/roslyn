﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using static Roslyn.Test.Performance.Utilities.TestUtilities;

namespace Roslyn.Test.Performance.Utilities
{
    public abstract class VsPerfTest : PerfTest
    {
        private const string _rootSuffix = "RoslynPerf";

        private readonly ILogger _logger;
        private readonly string _testTemplateName;
        private readonly string _testName;
        private readonly string _zipFileToDownload;
        private readonly int _zipFileVersion;
        private readonly string _solutionToTest;
        private readonly string _benchviewUploadName;
        private readonly string[] _scenarios;
        private static readonly string _nugetPackagesPath = System.Environment.GetEnvironmentVariable("NUGET_PACKAGES") ??
            Path.Combine(System.Environment.GetEnvironmentVariable("UserProfile"), ".nuget", "packages");
        private static readonly string _installerPath = Path.Combine(_nugetPackagesPath, "roslyntools.microsoft.vsixexpinstaller", "0.4.0-beta", "tools", "vsixexpinstaller.exe");

        public VsPerfTest(
            string testTemplateName,
            string testName,
            string solutionToTest,
            string benchviewUploadName,
            string[] scenarios,
            string zipFileToDownload = "RoslynSolutions",
            int zipFileVersion = 2) : base()
        {
            _testTemplateName = testTemplateName;
            _testName = testName;
            _zipFileToDownload = zipFileToDownload;
            _zipFileVersion = zipFileVersion;
            _solutionToTest = solutionToTest;
            _benchviewUploadName = benchviewUploadName;
            _scenarios = scenarios;
            _logger = new ConsoleAndFileLogger();
        }

        public override void Setup()
        {
            // Download test zip
            var dir = Path.Combine(Path.GetDirectoryName(MyWorkingDirectory), "csharp");
            DownloadProject(_zipFileToDownload, version: _zipFileVersion, logger: _logger);

            // Create log directory
            var logDirectory = Path.Combine(TempDirectory, _testName);
            Directory.CreateDirectory(logDirectory);
            Directory.CreateDirectory(Path.Combine(logDirectory, "PerfResults"));

            // Read the test template and replace with actual solution path.
            var taoTestFileTemplatePath = Path.Combine(dir, _testTemplateName);
            var template = File.ReadAllText(taoTestFileTemplatePath);
            var finalTest = template.Replace("ReplaceWithActualSolutionPath", Path.Combine(TempDirectory, _zipFileToDownload, _solutionToTest));
            finalTest = finalTest.Replace("ReplaceWithPerfLogDirectory", logDirectory);

            // Perform test specific string replacements
            finalTest = GetFinalTestSource(finalTest, dir);

            // Write the final test into the test file.
            File.WriteAllText(Path.Combine(TempDirectory, _testTemplateName), finalTest);

            // Install Roslyn VSIXes into the perf hive.
            InstallVsixes();
        }

        protected virtual string GetFinalTestSource(string testTemplateWithReplacedSolutionPath, string testFolderDirectory)
            => testTemplateWithReplacedSolutionPath;

        protected void InstallVsixes()
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
            var args = $"{Path.Combine(TempDirectory, _testTemplateName)} -perf -host:vs -roslynonly -rootsuffix:{_rootSuffix}";
            ShellOutVital(TaoPath, args, TempDirectory);

            var logDirectory = Path.Combine(TempDirectory, _testName, "PerfResults");
            var xcopyArgs = $"{logDirectory} {Path.Combine(MyBinaries(), "..", "..", "ToArchive")} /s /i /y";
            ShellOutVital("xcopy", xcopyArgs);

            foreach (var xml in Directory.EnumerateFiles(logDirectory, "*.xml"))
            {
                Benchview.UploadBenchviewReport(xml, _benchviewUploadName);
            }

            _logger.Flush();
        }

        public override int Iterations => 1;
        public override string Name => _testTemplateName.Substring(0, _testTemplateName.IndexOf("Template.xml"));
        public override string MeasuredProc => "devenv";
        public override bool ProvidesScenarios => false;
        public override string[] GetScenarios()
        {
            throw new System.NotImplementedException();
        }

        public override ITraceManager GetTraceManager()
        {
            return TraceManagerFactory.NoOpTraceManager();
        }
    }
}
