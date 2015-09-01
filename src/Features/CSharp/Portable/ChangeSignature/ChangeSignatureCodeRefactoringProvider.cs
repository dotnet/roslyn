// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ChangeSignature
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ChangeSignature), Shared]
    internal class ChangeSignatureCodeRefactoringProvider : AbstractChangeSignatureCodeRefactoringProvider
    {
    }
}
