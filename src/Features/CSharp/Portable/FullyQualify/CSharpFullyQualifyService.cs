// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes.FullyQualify;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FullyQualify;

using static CSharpSyntaxTokens;

[ExportLanguageService(typeof(IFullyQualifyService), LanguageNames.CSharp), Shared]
internal sealed class CSharpFullyQualifyService : AbstractFullyQualifyService<SimpleNameSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpFullyQualifyService()
    {
    }

    protected override bool CanFullyQualify(SyntaxNode node, [NotNullWhen(true)] out SimpleNameSyntax? simpleName)
    {
        simpleName = node as SimpleNameSyntax;
        if (simpleName is null)
            return false;

        if (!simpleName.LooksLikeStandaloneTypeName())
            return false;

        if (!simpleName.CanBeReplacedWithAnyName())
            return false;

        return true;
    }

    protected override async Task<SyntaxNode> ReplaceNodeAsync(SimpleNameSyntax simpleName, string containerName, bool resultingSymbolIsType, CancellationToken cancellationToken)
    {
        var leadingTrivia = simpleName.GetLeadingTrivia();
        var newName = simpleName.WithLeadingTrivia(SyntaxTriviaList.Empty);

        var qualifiedName = SyntaxFactory.QualifiedName(SyntaxFactory.ParseName(containerName), newName)
            .WithLeadingTrivia(leadingTrivia);

        var syntaxTree = simpleName.SyntaxTree;
        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        // If the name is a type that is part of a using directive, eg. "using Math" then we can go further and
        // instead of just changing to "using System.Math", we can make it "using static System.Math" and avoid the
        // CS0138 that would result from the former.  Don't do this for using aliases though as `static` and using
        // aliases cannot be combined.
        if (resultingSymbolIsType &&
            simpleName.Parent is UsingDirectiveSyntax { Alias: null, StaticKeyword.RawKind: 0 } usingDirective)
        {
            var newUsingDirective = usingDirective
                .WithStaticKeyword(StaticKeyword)
                .WithName(qualifiedName);

            return root.ReplaceNode(usingDirective, newUsingDirective);
        }

        return root.ReplaceNode(simpleName, qualifiedName);
    }
}
