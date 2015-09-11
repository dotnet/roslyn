// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.EncapsulateField;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.EncapsulateField
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.EncapsulateField), Shared]
    internal class EncapsulateFieldRefactoringProvider : AbstractEncapsulateFieldRefactoringProvider
    {
    }
}
