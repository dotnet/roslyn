// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.AddDebuggerDisplay;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.AddDebuggerDisplay;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddDebuggerDisplay), Shared]
internal sealed class CSharpAddDebuggerDisplayCodeRefactoringProvider
    : AbstractAddDebuggerDisplayCodeRefactoringProvider<
        TypeDeclarationSyntax,
        MethodDeclarationSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpAddDebuggerDisplayCodeRefactoringProvider()
    {
    }

    protected override bool CanNameofAccessNonPublicMembersFromAttributeArgument => true;

    protected override bool SupportsConstantInterpolatedStrings(Document document)
        => document.Project.ParseOptions!.LanguageVersion().HasConstantInterpolatedStrings();
}
