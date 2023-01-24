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
internal class CSharpCodeLensMemberFinder : ICodeLensMemberFinder
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
        var visitor = new CSharpCodeLensVisitor(codeLensNodes.Add);

        visitor.Visit(root);

        return codeLensNodes.ToImmutable();
    }

    private class CSharpCodeLensVisitor : CSharpSyntaxWalker
    {
        private readonly Action<CodeLensMember> _memberFoundAction;

        public CSharpCodeLensVisitor(Action<CodeLensMember> memberFoundAction)
        {
            _memberFoundAction = memberFoundAction;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitClassDeclaration(node);
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitInterfaceDeclaration(node);
        }

        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitEnumDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitPropertyDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitMethodDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitStructDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitConstructorDeclaration(node);
        }

        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            _memberFoundAction(new CodeLensMember(node, node.Identifier.Span));
            base.VisitRecordDeclaration(node);
        }
    }
}
