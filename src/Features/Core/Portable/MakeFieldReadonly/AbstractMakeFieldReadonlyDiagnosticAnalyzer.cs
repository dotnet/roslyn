// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.MakeFieldReadonly
{
    internal abstract class AbstractMakeFieldReadonlyDiagnosticAnalyzer<TIdentifierNameSyntax, TConstructorDeclarationSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TIdentifierNameSyntax : SyntaxNode
        where TConstructorDeclarationSyntax : SyntaxNode
    {
        protected AbstractMakeFieldReadonlyDiagnosticAnalyzer()
            : base(
                IDEDiagnosticIds.MakeFieldReadonlyDiagnosticId,
                new LocalizableResourceString(nameof(FeaturesResources.Add_readonly_modifier), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                new LocalizableResourceString(nameof(FeaturesResources.Make_field_readonly), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract bool IsWrittenTo(TIdentifierNameSyntax node, SemanticModel model, CancellationToken cancellationToken);
        protected abstract bool IsMemberOfThisInstance(SyntaxNode node);

        protected abstract void AddCandidateTypesInCompilationUnit(SemanticModel semanticModel, SyntaxNode compilationUnit, PooledHashSet<(ITypeSymbol, SyntaxNode)> candidateTypes, CancellationToken cancellationToken);

        public override bool OpenFileOnly(Workspace workspace) => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

        private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;

            // Early return if user disables the feature
            var optionSet = context.Options.GetDocumentOptionSetAsync(semanticModel.SyntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferReadonly, semanticModel.Language);
            if (!option.Value)
            {
                return;
            }

            var syntaxFactsService = GetSyntaxFactsService();
            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var candidateTypes = PooledHashSet<(ITypeSymbol, SyntaxNode)>.GetInstance();
            AddCandidateTypesInCompilationUnit(semanticModel, root, candidateTypes, cancellationToken);

            var candidateFields = PooledHashSet<IFieldSymbol>.GetInstance();
            foreach (var (typeSymbol, typeSyntax) in candidateTypes)
            {
                AddCandidateFieldsInType(context, typeSymbol, typeSyntax, candidateFields);
            }

            if (candidateFields.Count > 0)
            {
                var analyzedTypes = ArrayBuilder<(ITypeSymbol, SyntaxNode)>.GetInstance(candidateTypes.Count);
                analyzedTypes.AddRange(candidateTypes);
                analyzedTypes.Sort((x, y) => x.Item2.SpanStart - y.Item2.SpanStart);

                // Remove types from the analysis list when they are contained by another type in the list
                for (var i = analyzedTypes.Count - 1; i >= 1; i--)
                {
                    if (analyzedTypes[i - 1].Item2.FullSpan.Contains(analyzedTypes[i].Item2.FullSpan))
                    {
                        analyzedTypes[i] = default;
                    }
                }

                foreach (var (typeSymbol, typeSyntax) in candidateTypes)
                {
                    if (typeSyntax is null)
                    {
                        // Node was removed due to nested scoping
                        continue;
                    }

                    RemoveAssignedSymbols(semanticModel, syntaxFactsService, typeSyntax, candidateFields, cancellationToken);
                }

                foreach (var symbol in candidateFields)
                {
                    var diagnostic = DiagnosticHelper.Create(
                        Descriptor,
                        symbol.Locations[0],
                        option.Notification.Severity,
                        additionalLocations: null,
                        properties: null);
                    context.ReportDiagnostic(diagnostic);
                }

                analyzedTypes.Free();
            }

            candidateTypes.Free();
            candidateFields.Free();
        }

        private void AddCandidateFieldsInType(SemanticModelAnalysisContext context, ITypeSymbol typeSymbol, SyntaxNode typeSyntax, PooledHashSet<IFieldSymbol> candidateFields)
        {
            foreach (var item in typeSymbol.GetMembers())
            {
                if (item is IFieldSymbol symbol &&
                    symbol.DeclaredAccessibility == Accessibility.Private &&
                    !symbol.IsReadOnly &&
                    !symbol.IsConst &&
                    !symbol.IsImplicitlyDeclared &&
                    !IsMutableValueType(symbol.Type))
                {
                    candidateFields.Add(symbol);
                }
            }
        }

        private void RemoveAssignedSymbols(SemanticModel model, ISyntaxFactsService syntaxFactsService, SyntaxNode node, PooledHashSet<IFieldSymbol> unassignedSymbols, CancellationToken cancellationToken)
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
                    var isInAnonymousOrLocalFunction = false;
                    for (var current = descendant.Parent; current != ctorNode; current = current.Parent)
                    {
                        if (syntaxFactsService.IsAnonymousOrLocalFunctionStatement(current))
                        {
                            isInAnonymousOrLocalFunction = true;
                            break;
                        }
                    }

                    if (isInAnonymousOrLocalFunction)
                    {
                        unassignedSymbols.Remove(symbol);
                    }
                    else
                    {
                        var ctorSymbol = model.GetDeclaredSymbol(ctorNode);
                        if (!ctorSymbol.ContainingType.Equals(symbol.ContainingType))
                        {
                            unassignedSymbols.Remove(symbol);
                        }

                        if (!ctorSymbol.IsStatic && symbol.IsStatic)
                        {
                            unassignedSymbols.Remove(symbol);
                        }
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

        private bool IsMutableValueType(ITypeSymbol type)
        {
            if (type.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            foreach (var member in type.GetMembers())
            {
                if (member is IFieldSymbol fieldSymbol &&
                    !(fieldSymbol.IsConst || fieldSymbol.IsReadOnly || fieldSymbol.IsStatic))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
