// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;
using Roslyn.VisualStudio.Setup;

[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.EditorFeatures")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Features")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.CSharp.Workspaces")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.EditorFeatures.Text")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Features")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.EditorFeatures")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.Features")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.VisualBasic.Workspaces")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Workspaces.Desktop")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.CodeAnalysis.Workspaces")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.Implementation")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.VisualBasic")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.CSharp")]
[assembly: ProvideRoslynBindingRedirection("Microsoft.VisualStudio.LanguageServices.SolutionExplorer")]

[assembly: ProvideBindingRedirection(
    AssemblyName = "System.Reflection.Metadata",
    OldVersionLowerBound = "1.0.0.0",
    OldVersionUpperBound = "1.0.99.0",
    NewVersion = "1.1.0.0",
    PublicKeyToken = "b03f5f7f11d50a3a",
    GenerateCodeBase = ProvideRoslynBindingRedirectionAttribute.GenerateCodeBase)]