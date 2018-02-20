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
            var optionSet = context.Options.GetDocumentOptionSetAsync(context.Node.SyntaxTree, context.CancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferReadonly, context.Node.Language);
            if (!option.Value)
            {
                return;
            }

            var typeSymbol = (ITypeSymbol)context.SemanticModel.GetDeclaredSymbol(context.Node);

            var nonReadonlyFieldMembers = PooledHashSet<IFieldSymbol>.GetInstance();
            foreach (var item in typeSymbol.GetMembers())
            {
                if (item is IFieldSymbol symbol &&
                    symbol.DeclaredAccessibility == Accessibility.Private &&
                    !symbol.IsReadOnly &&
                    !symbol.IsConst &&
                    !symbol.IsImplicitlyDeclared &&
                    !IsMutableValueType(symbol.Type))
                {
                    nonReadonlyFieldMembers.Add(symbol);
                }
            }

            var syntaxFactsService = GetSyntaxFactsService();
            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                var typeNode = syntaxReference.SyntaxTree.GetRoot(context.CancellationToken).FindNode(syntaxReference.Span);

                var semanticModelForTree = context.SemanticModel.Compilation.GetSemanticModel(syntaxReference.SyntaxTree);
                RemoveAssignedSymbols(semanticModelForTree, syntaxFactsService, typeNode, nonReadonlyFieldMembers, context.CancellationToken);
            }

            foreach (var symbol in nonReadonlyFieldMembers)
            {
                var diagnostic = Diagnostic.Create(
                    GetDescriptorWithSeverity(option.Notification.Value),
                    symbol.Locations[0]);
                context.ReportDiagnostic(diagnostic);
            }

            nonReadonlyFieldMembers.Free();
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
                        if (syntaxFactsService.IsAnonymousOrLocalFunction(current))
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

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract bool IsWrittenTo(TIdentifierNameSyntax node, SemanticModel model, CancellationToken cancellationToken);
        protected abstract bool IsMemberOfThisInstance(SyntaxNode node);
    }
}
