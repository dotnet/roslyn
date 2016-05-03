// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#r "./../../Roslyn.Test.Performance.Utilities.dll"

// IsVerbose()
#load "../util/test_util.csx"

using System.IO;
using Roslyn.Test.Performance.Utilities;

TestUtilities.InitUtilities();
var directoryUtil = new RelativeDirectory();
var logger = new ConsoleAndFileLogger();

var binariesDirectory = directoryUtil.MyBinaries();
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
    TestUtilities.ShellOutVital(installer, $"/rootSuffix:RoslynPerf {vsix}", IsVerbose(), logger, binariesDirectory);
}