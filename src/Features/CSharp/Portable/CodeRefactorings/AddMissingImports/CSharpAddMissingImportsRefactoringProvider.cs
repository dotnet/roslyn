// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
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
        public CSharpAddMissingImportsRefactoringProvider(IPasteTrackingService pasteTrackingService)
            : base(pasteTrackingService)
        {
        }
    }
}
