// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MoveDeclarationNearReference;

namespace Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference), Shared]
    [ExtensionOrder(After = PredefinedCodeRefactoringProviderNames.InlineTemporary)]
    class CSharpMoveDeclarationNearReferenceCodeRefactoringProvider : AbstractMoveDeclarationNearReferenceCodeRefactoringProvider<LocalDeclarationStatementSyntax>
    {
    }
}
