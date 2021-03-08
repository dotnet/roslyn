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
using Microsoft.CodeAnalysis.InheritanceChainMargin;
using Microsoft.VisualStudio.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InheritanceChainMargin
{
    [ExportLanguageService(typeof(IInheritanceChainService), LanguageNames.CSharp), Shared]
    internal class InheritanceChainService : AbstractInheritanceChainService<TypeDeclarationSyntax>
    {
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        [ImportingConstructor]
        public InheritanceChainService()
        {
        }

        protected override ImmutableArray<TypeDeclarationSyntax> GetDeclarationNodes(SyntaxNode root)
            => root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToImmutableArray();

        protected override ImmutableArray<SyntaxNode> GetMembers(TypeDeclarationSyntax typeDeclarationNode)
        {
            var members = typeDeclarationNode.Members;
            using var _ = PooledObjects.ArrayBuilder<SyntaxNode>.GetInstance(out var builder);
            foreach(var member in members)
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

            return builder.ToImmutableArray();
        }

        protected override ImmutableArray<int> GetMemberIdentifiersPosition(TypeDeclarationSyntax typeDeclarationNode)
        {
            using var _ = PooledObjects.ArrayBuilder<int>.GetInstance(out var builder);
            builder.Add(typeDeclarationNode.Identifier.SpanStart);
            var members = typeDeclarationNode.Members;
            foreach(var member in members)
            {
                if (member is MethodDeclarationSyntax memberDeclarationNode)
                {
                    builder.Add(memberDeclarationNode.Identifier.SpanStart);
                }

                if (member is PropertyDeclarationSyntax propertyDeclarationNode)
                {
                    builder.Add(propertyDeclarationNode.Identifier.SpanStart);
                }

                if (member is IndexerDeclarationSyntax indexerDeclarationNode)
                {
                    builder.Add(indexerDeclarationNode.ThisKeyword.SpanStart);
                }

                if (member is EventDeclarationSyntax eventDeclarationNode)
                {
                    builder.Add(eventDeclarationNode.Identifier.SpanStart);
                }

                // For multiple events that declared in the same EventFieldDeclaration,
                // add all VariableDeclarators
                if (member is EventFieldDeclarationSyntax eventFieldDeclarationNode)
                {
                    foreach (var variable in eventFieldDeclarationNode.Declaration.Variables)
                    {
                        builder.Add(variable.Identifier.SpanStart);
                    }
                }
            }

            return builder.ToImmutableArray();
        }
    }
}
