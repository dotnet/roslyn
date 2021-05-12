// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens.Tagger;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeLens
{
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(Microsoft.VisualStudio.Language.CodeLens.ICodeLensTag))]
    [VisualStudio.Utilities.ContentType("CSharp")]
    internal class CSharpCodeLensViewTaggerProvider : AbstractCodeLensViewTaggerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpCodeLensViewTaggerProvider(
            IThreadingContext threadingContext,
            IAsynchronousOperationListener asyncListener)
            : base(threadingContext, asyncListener)
        {
        }

        protected override ImmutableArray<CodeLensNodeInfo> ComputeNodeInfo(
            SyntaxNode root, TextSpan span, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<CodeLensNodeInfo>.GetInstance(out var result);
            var visitor = new CSharpCodeLensVisitor(result, span, cancellationToken);
            visitor.Visit(root);
            return result.ToImmutable();
        }

        private class CSharpCodeLensVisitor : CSharpSyntaxVisitor
        {
            private readonly CancellationToken _cancellationToken;
            private readonly ArrayBuilder<CodeLensNodeInfo> _result;
            private readonly TextSpan _span;

            public CSharpCodeLensVisitor(ArrayBuilder<CodeLensNodeInfo> result, TextSpan span, CancellationToken cancellationToken)
            {
                _result = result;
                _span = span;
                _cancellationToken = cancellationToken;
            }

            public override void VisitCompilationUnit(CompilationUnitSyntax node)
            {
                foreach (var child in node.Members)
                    this.Visit(child);
            }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                foreach (var child in node.Members)
                    this.Visit(child);
            }

            private void VisitTypeDeclaration(TypeDeclarationSyntax node)
            {
                _result.Add(new CodeLensNodeInfo(node, node.Identifier, GetDescription(node), CodeElementKinds.Type));
                foreach (var child in node.Members)
                    this.Visit(child);
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
                => VisitTypeDeclaration(node);

            public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
                => VisitTypeDeclaration(node);

            public override void VisitStructDeclaration(StructDeclarationSyntax node)
                => VisitTypeDeclaration(node);

            public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
                => VisitTypeDeclaration(node);

            public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
                => _result.Add(new CodeLensNodeInfo(node, node.Identifier, GetDescription(node), CodeElementKinds.Type));

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
                => _result.Add(new CodeLensNodeInfo(node, node.Identifier, GetDescription(node), CodeElementKinds.Method));

            public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
                => _result.Add(new CodeLensNodeInfo(node, node.Identifier, GetDescription(node), CodeElementKinds.Method));

            public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
                => _result.Add(new CodeLensNodeInfo(node, node.TildeToken, GetDescription(node), CodeElementKinds.Method));

            public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
                => _result.Add(new CodeLensNodeInfo(node, node.OperatorKeyword, GetDescription(node), CodeElementKinds.Method));

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
                => _result.Add(new CodeLensNodeInfo(node, node.Identifier, GetDescription(node), CodeElementKinds.Property));

            public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
                => _result.Add(new CodeLensNodeInfo(node, node.ThisKeyword, GetDescription(node), CodeElementKinds.Property));

            public override void Visit(SyntaxNode? node)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (node != null && node.Span.IntersectsWith(_span))
                    base.Visit(node);
            }

            private static string GetDescription(SyntaxNode node)
            {
                // elementDescription was never set, set it here
                var name = GetName(node);
                var parent = node.Parent;
                if (parent != null)
                {
                    var parentName = GetName(parent);

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(name))
                    {
                        // prepend the parents name 
                        name = $"{parentName}.{name}";
                    }
                }

                return name;
            }

            private static string NodeToString(SyntaxNode node)
                => node.ConvertToSingleLine().ToString();

            private static string GetName(SyntaxNode node)
            {
                return node switch
                {
                    NamespaceDeclarationSyntax namespaceDecl => NodeToString(namespaceDecl.Name),
                    BaseTypeDeclarationSyntax typeDecl => typeDecl.Identifier.ValueText,
                    PropertyDeclarationSyntax propNode =>
                        propNode.ExplicitInterfaceSpecifier != null
                            ? $"({NodeToString(propNode.ExplicitInterfaceSpecifier.Name)}.{propNode.Identifier.ValueText})"
                            : propNode.Identifier.ValueText,
                    MethodDeclarationSyntax method => method.Identifier.ValueText,
                    OperatorDeclarationSyntax operatorNode => $"operator {operatorNode.OperatorToken}",
                    IndexerDeclarationSyntax indexer =>
                        indexer.ExplicitInterfaceSpecifier != null
                            ? $"({NodeToString(indexer.ExplicitInterfaceSpecifier.Name)}.{indexer.ThisKeyword})"
                            : indexer.ThisKeyword.ToString(),
                    ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                    DestructorDeclarationSyntax destructor => $"~{destructor.Identifier.ValueText}",
                    _ => string.Empty,
                };
            }
        }
    }
}
