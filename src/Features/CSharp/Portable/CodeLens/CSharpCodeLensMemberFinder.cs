// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeLens;

[ExportLanguageService(typeof(ICodeLensMemberFinder), LanguageNames.CSharp), Shared]
internal sealed class CSharpCodeLensMemberFinder : ICodeLensMemberFinder
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCodeLensMemberFinder()
    {
    }

    public async Task<ImmutableArray<CodeLensMember>> GetCodeLensMembersAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<CodeLensMember>.GetInstance(out var codeLensNodes);
        var visitor = new CSharpCodeLensVisitor(codeLensNodes);

        visitor.Visit(root);

        return codeLensNodes.ToImmutable();
    }

    private sealed class CSharpCodeLensVisitor : CSharpSyntaxWalker
    {
        private readonly ArrayBuilder<CodeLensMember> _memberBuilder;

        public CSharpCodeLensVisitor(ArrayBuilder<CodeLensMember> memberBuilder)
        {
            _memberBuilder = memberBuilder;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
            base.VisitClassDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
            base.VisitStructDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            _memberBuilder.Add(new CodeLensMember(node, node.Identifier.Span));
            base.VisitRecordDeclaration(node);
        }
    }
}
