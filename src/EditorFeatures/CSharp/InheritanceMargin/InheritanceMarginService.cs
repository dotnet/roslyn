// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.InheritanceMargin;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InheritanceMargin
{
    [ExportLanguageService(typeof(IInheritanceMarginService), LanguageNames.CSharp), Shared]
    internal class InheritanceMarginService : AbstractInheritanceMarginService
    {
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public InheritanceMarginService()
        {
        }

        protected override ImmutableArray<SyntaxNode> GetMembers(SyntaxNode root)
        {
            var typeDeclarationNodes = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToImmutableArray();

            using var _ = PooledObjects.ArrayBuilder<SyntaxNode>.GetInstance(out var builder);
            builder.AddRange(typeDeclarationNodes);

            foreach (var typeDeclarationNode in typeDeclarationNodes)
            {
                var members = typeDeclarationNode.Members;
                foreach (var member in members)
                {
                    if (member.IsKind(SyntaxKind.MethodDeclaration)
                        || member.IsKind(SyntaxKind.PropertyDeclaration)
                        || member.IsKind(SyntaxKind.IndexerDeclaration)
                        || member.IsKind(SyntaxKind.EventDeclaration))
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
    }
}
