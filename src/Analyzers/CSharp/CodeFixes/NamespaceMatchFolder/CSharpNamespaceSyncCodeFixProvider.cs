// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.NamespaceMatchFolder;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.NamespaceMatchFolder
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ChangeNamespaceToMatchFolder), Shared]
    internal sealed class CSharpChangeNamespaceToMatchFolderCodeFixProvider : AbstractChangeNamespaceToMatchFolderCodeFixProvider
    {

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpChangeNamespaceToMatchFolderCodeFixProvider()
        {
        }

        protected override string Title => CSharpAnalyzersResources.Namespace_does_not_match_folder_structure;
    }
}
