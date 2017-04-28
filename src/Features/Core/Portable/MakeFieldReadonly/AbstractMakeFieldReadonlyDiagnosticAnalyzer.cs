// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.MakeFieldReadonly
{
    internal abstract class AbstractMakeFieldReadonlyDiagnosticAnalyzer<TIdentifierNameSyntax, TConstructorDeclarationSyntax, TLambdaSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TIdentifierNameSyntax : SyntaxNode
        where TConstructorDeclarationSyntax : SyntaxNode
        where TLambdaSyntax : SyntaxNode
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FeaturesResources.Add_readonly_modifier), FeaturesResources.ResourceManager, typeof(FeaturesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FeaturesResources.Make_field_readonly), WorkspacesResources.ResourceManager, typeof(WorkspacesResources));

        public AbstractMakeFieldReadonlyDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId,
                   s_localizableTitle, s_localizableMessage)
        {
        }

        public override bool OpenFileOnly(Workspace workspace) => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        internal void AnalyzeType(SyntaxNodeAnalysisContext context)
        {
            var typeSymbol = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);

            var nonReadonlyFieldMembers = new HashSet<IFieldSymbol>();
            foreach (var item in typeSymbol.GetMembers())
            {
                if (item is IFieldSymbol symbol &&
                    symbol.DeclaredAccessibility == Accessibility.Private &&
                    !symbol.IsReadOnly &&
                    !symbol.IsImplicitlyDeclared)
                {
                    nonReadonlyFieldMembers.Add(symbol);
                }
            }

            var membersCanBeReadonly = nonReadonlyFieldMembers;
            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                var typeNode = syntaxReference.SyntaxTree.GetRoot(context.CancellationToken).FindNode(syntaxReference.Span);

                var semanticModelForTree = context.SemanticModel.Compilation.GetSemanticModel(syntaxReference.SyntaxTree);
                GetUnassignedSymbols(semanticModelForTree, typeNode, membersCanBeReadonly, context.CancellationToken);
            }

            foreach (var symbol in membersCanBeReadonly)
            {
                var diagnostic = Diagnostic.Create(
                   InfoDescriptor,
                   symbol.Locations[0]);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private void GetUnassignedSymbols(SemanticModel model, SyntaxNode node, HashSet<IFieldSymbol> unassignedSymbols, CancellationToken cancellationToken)
        {
            foreach (var descendant in node.DescendantNodes())
            {
                if (unassignedSymbols.Count == 0)
                {
                    return;
                }

                if (!(descendant is TIdentifierNameSyntax name))
                {
                    continue;
                }

                var symbol = model.GetSymbolInfo(descendant).Symbol as IFieldSymbol;
                if (symbol == null || !unassignedSymbols.Contains(symbol))
                {
                    continue;
                }

                if (!IsMemberOfThisInstance(descendant))
                {
                    unassignedSymbols.Remove(symbol);
                }

                if (IsDescendentOf<TConstructorDeclarationSyntax>(descendant, out var ctorNode))
                {
                    var ctorSymbol = model.GetDeclaredSymbol(ctorNode);
                    if (!ctorSymbol.IsStatic && symbol.IsStatic)
                    {
                        unassignedSymbols.Remove(symbol);
                    }

                    if (descendant.FirstAncestorOrSelf<TLambdaSyntax>() != null)
                    {
                        unassignedSymbols.Remove(symbol);
                    }

                    // assignments in the ctor don't matter other than the static modifiers and lambdas point checked above
                    continue;
                }
                
                if (IsWrittenTo(name, model, cancellationToken))
                {
                    unassignedSymbols.Remove(symbol);
                }
            }
        }
        
        private bool IsDescendentOf<T>(SyntaxNode node, out T ctor) where T : SyntaxNode
        {
            ctor = node.FirstAncestorOrSelf<T>();
            return ctor != null;
        }

        protected abstract bool IsWrittenTo(TIdentifierNameSyntax node, SemanticModel model, CancellationToken cancellationToken);
        protected abstract bool IsMemberOfThisInstance(SyntaxNode node);
    }
}
