// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.MatchFolderAndNamespace;

[Export(typeof(CSharpChangeNamespaceToMatchFolderCodeFixProvider))]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ChangeNamespaceToMatchFolder), Shared]
internal class CSharpChangeNamespaceToMatchFolderCodeFixProvider : AbstractChangeNamespaceToMatchFolderCodeFixProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpChangeNamespaceToMatchFolderCodeFixProvider()
    {
    }
}
