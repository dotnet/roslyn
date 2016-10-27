﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Scripting.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Scripting.dll")]
// [assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.Scripting.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.InteractiveEditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.InteractiveEditorFeatures.dll")]
// [assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.InteractiveEditorFeatures.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.InteractiveFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.InteractiveServices.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.CSharp.Repl.dll")]
// [assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.VisualBasic.Repl.dll")]

[assembly: ProvideRoslynBindingRedirection("InteractiveHost.exe")]

//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.AppContext.dll")] - removed because project is not executable.
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Console.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Diagnostics.FileVersionInfo.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Diagnostics.Process.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.Compression.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.FileSystem.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.FileSystem.DriveInfo.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.FileSystem.Primitives.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.Pipes.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Runtime.InteropServices.RuntimeInformation.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.AccessControl.dll")] - removed because project has no dependency.
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Claims.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Cryptography.Algorithms.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Security.Cryptography.Encoding.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Security.Cryptography.Primitives.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Security.Cryptography.X509Certificates.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Principal.Windows.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Text.Encoding.CodePages.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Threading.Thread.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Xml.XmlDocument.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Xml.XPath.XDocument.dll")]
