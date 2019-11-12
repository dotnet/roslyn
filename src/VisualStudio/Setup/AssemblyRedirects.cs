// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.EditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.EditorFeatures.Wpf.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Features.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Scripting.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Workspaces.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures.Text.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures.Wpf.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Features.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.InteractiveHost.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.LanguageServer.Protocol.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Scripting.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.Features.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.Workspaces.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Workspaces.Desktop.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Workspaces.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Workspaces.MSBuild.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.CodeLens.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.Implementation.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.VisualBasic.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.CSharp.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.LiveShare.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.SolutionExplorer.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.Xaml.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.Razor.RemoteClient.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.Apex.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.CodeLens.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.Debugger.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.FSharp.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.IntelliTrace.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.Razor.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.TypeScript.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.ExternalAccess.Xamarin.Remote.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Elfie.dll")]

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Remote.Razor.ServiceHub.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Remote.ServiceHub.dll")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Remote.Workspaces.dll")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\SQLitePCLRaw.batteries_green.DLL")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\SQLitePCLRaw.batteries_v2.DLL")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\SQLitePCLRaw.core.DLL")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\SQLitePCLRaw.provider.e_sqlite3.DLL")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Composition.Convention.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Composition.Hosting.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\System.Composition.TypedParts.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Humanizer.dll")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.VisualStudio.LanguageServer.Protocol.dll")]
[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.VisualStudio.LanguageServer.Protocol.Extensions.dll")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\Microsoft.CodeAnalysis.AnalyzerUtilities.dll")]

[assembly: ProvideCodeBase(CodeBase = @"$PackageFolder$\ICSharpCode.Decompiler.dll")]

[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.VisualStudio.CallHierarchy.Package.Definitions",
    GenerateCodeBase = false,
    PublicKeyToken = "31BF3856AD364E35",
    OldVersionLowerBound = "14.0.0.0",
    OldVersionUpperBound = "14.9.9.9",
    NewVersion = "15.0.0.0")]
