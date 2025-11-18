// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InheritanceMargin;

[ExportLanguageService(typeof(IInheritanceMarginService), LanguageNames.CSharp), Shared]
internal sealed class CSharpInheritanceMarginService : AbstractInheritanceMarginService
{
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    [ImportingConstructor]
    public CSharpInheritanceMarginService()
    {
    }

    protected override string GlobalImportsTitle => CSharpFeaturesResources.Global_using_directives;

    protected override ImmutableArray<SyntaxNode> GetMembers(IEnumerable<SyntaxNode> nodesToSearch)
    {
        var typeDeclarationNodes = nodesToSearch.OfType<TypeDeclarationSyntax>();

        using var _ = PooledObjects.ArrayBuilder<SyntaxNode>.GetInstance(out var builder);
        foreach (var typeDeclarationNode in typeDeclarationNodes)
        {
            // 1. Add the type declaration node.(e.g. class, struct etc..)
            // Use its identifier's position as the line number, since we want the margin to be placed with the identifier
            builder.Add(typeDeclarationNode);

            // 2. Add type members inside this type declaration.
            foreach (var member in typeDeclarationNode.Members)
            {
                if (member.Kind() is
                        SyntaxKind.MethodDeclaration or
                        SyntaxKind.PropertyDeclaration or
                        SyntaxKind.EventDeclaration or
                        SyntaxKind.IndexerDeclaration or
                        SyntaxKind.OperatorDeclaration or
                        SyntaxKind.ConversionOperatorDeclaration)
                {
                    builder.Add(member);
                }

                // For multiple events that declared in the same EventFieldDeclaration,
                // add all VariableDeclarators
                if (member is EventFieldDeclarationSyntax eventFieldDeclarationNode)
                {
                    builder.AddRange(eventFieldDeclarationNode.Declaration.Variables);
                }
            }
        }

        return builder.ToImmutableArray();
    }

    protected override SyntaxToken GetDeclarationToken(SyntaxNode declarationNode)
        => declarationNode switch
        {
            MethodDeclarationSyntax methodDeclarationNode => methodDeclarationNode.Identifier,
            PropertyDeclarationSyntax propertyDeclarationNode => propertyDeclarationNode.Identifier,
            EventDeclarationSyntax eventDeclarationNode => eventDeclarationNode.Identifier,
            VariableDeclaratorSyntax variableDeclaratorNode => variableDeclaratorNode.Identifier,
            TypeDeclarationSyntax baseTypeDeclarationNode => baseTypeDeclarationNode.Identifier,
            IndexerDeclarationSyntax indexerDeclarationNode => indexerDeclarationNode.ThisKeyword,
            OperatorDeclarationSyntax operatorDeclarationNode => operatorDeclarationNode.OperatorToken,
            ConversionOperatorDeclarationSyntax conversionOperatorDeclarationNode => conversionOperatorDeclarationNode.Type.GetFirstToken(),
            // Shouldn't reach here since the input declaration nodes are coming from GetMembers() method above
            _ => throw ExceptionUtilities.UnexpectedValue(declarationNode),
        };
}
