// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Features.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Workspaces.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures.Text.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Features.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.Features.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Workspaces.Desktop.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Workspaces.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.Implementation.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.VisualBasic.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.CSharp.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.SolutionExplorer.dll")]

[assembly: ProvideRoslynBindingRedirection("System.Reflection.Metadata.dll")]
[assembly: ProvideRoslynBindingRedirection("System.Collections.Immutable.dll")]
[assembly: ProvideRoslynBindingRedirection("Esent.Interop.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Elfie.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.DiaSymReader.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.DiaSymReader.PortablePdb.dll")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Composition.Convention.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Composition.Hosting.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Composition.TypedParts.dll")]

// [assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.AppContext.dll")] - removed because project is not executable.
// [assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Console.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Diagnostics.FileVersionInfo.dll")]
// [assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Diagnostics.Process.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.Compression.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.FileSystem.dll")]
// [assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.FileSystem.DriveInfo.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.FileSystem.Primitives.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.IO.Pipes.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Runtime.InteropServices.RuntimeInformation.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.AccessControl.dll")] - removed because project has no dependency.
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Claims.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Cryptography.Algorithms.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Cryptography.Encoding.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Cryptography.Primitives.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Cryptography.X509Certificates.dll")]
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Security.Principal.Windows.dll")] - removed because project has no dependency.
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Text.Encoding.CodePages.dll")] - removed because project has no dependency.
//[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Threading.Thread.dll")] - removed because project has no dependency.
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Xml.XmlDocument.dll")]
[assembly: ProvideCodeBase(CodeBase = "$PackageFolder$\\System.Xml.XPath.XDocument.dll")]
