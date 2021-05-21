﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.PasteTracking;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddMissingImports
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddMissingImports), Shared]
    internal class CSharpAddMissingImportsRefactoringProvider : AbstractAddMissingImportsRefactoringProvider
    {
        protected override string CodeActionTitle => CSharpFeaturesResources.Add_missing_usings;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpAddMissingImportsRefactoringProvider(IPasteTrackingService pasteTrackingService)
            : base(pasteTrackingService)
        {
        }
    }
}
