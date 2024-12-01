// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
namespace Microsoft.CodeAnalysis.CSharp.ConvertTupleToStruct;

[ExtensionOrder(Before = PredefinedCodeRefactoringProviderNames.IntroduceVariable)]
[ExportLanguageService(typeof(IConvertTupleToStructCodeRefactoringProvider), LanguageNames.CSharp)]
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertTupleToStruct), Shared]
internal class CSharpConvertTupleToStructCodeRefactoringProvider :
    AbstractConvertTupleToStructCodeRefactoringProvider<
        ExpressionSyntax,
        NameSyntax,
        IdentifierNameSyntax,
        LiteralExpressionSyntax,
        ObjectCreationExpressionSyntax,
        TupleExpressionSyntax,
        ArgumentSyntax,
        TupleTypeSyntax,
        TypeDeclarationSyntax,
        BaseNamespaceDeclarationSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpConvertTupleToStructCodeRefactoringProvider()
    {
    }

    protected override ArgumentSyntax GetArgumentWithChangedName(ArgumentSyntax argument, string name)
        => argument.WithNameColon(ChangeName(argument.NameColon, name));

    private static NameColonSyntax? ChangeName(NameColonSyntax? nameColon, string name)
    {
        if (nameColon == null)
        {
            return null;
        }

        var newName = SyntaxFactory.IdentifierName(name).WithTriviaFrom(nameColon.Name);
        return nameColon.WithName(newName);
    }
}
