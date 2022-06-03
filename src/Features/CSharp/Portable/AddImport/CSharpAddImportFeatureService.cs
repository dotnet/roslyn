// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.AddImport.AddImportDiagnosticIds;

namespace Microsoft.CodeAnalysis.CSharp.AddImport
{
    [ExportLanguageService(typeof(IAddImportFeatureService), LanguageNames.CSharp), Shared]
    internal class CSharpAddImportFeatureService : AbstractAddImportFeatureService<SimpleNameSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpAddImportFeatureService()
        {
        }

        protected override bool CanAddImport(SyntaxNode node, bool allowInHiddenRegions, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return node.CanAddUsingDirectives(allowInHiddenRegions, cancellationToken);
        }

        protected override bool CanAddImportForMethod(
            string diagnosticId, ISyntaxFacts syntaxFacts, SyntaxNode node, out SimpleNameSyntax nameNode)
        {
            nameNode = null;

            switch (diagnosticId)
            {
                case CS7036:
                case CS0308:
                case CS0428:
                case CS1061:
                    if (node.IsKind(SyntaxKind.ConditionalAccessExpression, out ConditionalAccessExpressionSyntax conditionalAccess))
                    {
                        node = conditionalAccess.WhenNotNull;
                    }
                    else if (node.IsKind(SyntaxKind.MemberBindingExpression, out MemberBindingExpressionSyntax memberBinding1))
                    {
                        node = memberBinding1.Name;
                    }
                    else if (node.Parent.IsKind(SyntaxKind.CollectionInitializerExpression))
                    {
                        return true;
                    }

                    break;
                case CS0122:
                case CS1501:
                    if (node is SimpleNameSyntax)
                    {
                        break;
                    }
                    else if (node is MemberBindingExpressionSyntax memberBindingExpr)
                    {
                        node = memberBindingExpr.Name;
                    }

                    break;
                case CS1929:
                    var memberAccessName = (node.Parent as MemberAccessExpressionSyntax)?.Name;
                    var conditionalAccessName = (((node.Parent as ConditionalAccessExpressionSyntax)?.WhenNotNull as InvocationExpressionSyntax)?.Expression as MemberBindingExpressionSyntax)?.Name;
                    if (memberAccessName == null && conditionalAccessName == null)
                    {
                        return false;
                    }

                    node = memberAccessName ?? conditionalAccessName;
                    break;

                case CS1503:
                    //// look up its corresponding method name
                    var parent = node.GetAncestor<InvocationExpressionSyntax>();
                    if (parent == null)
                    {
                        return false;
                    }

                    if (parent.Expression is MemberAccessExpressionSyntax method)
                    {
                        node = method.Name;
                    }

                    break;
                case CS1955:
                    break;

                default:
                    return false;
            }

            nameNode = node as SimpleNameSyntax;
            if (!nameNode.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) &&
                !nameNode.IsParentKind(SyntaxKind.MemberBindingExpression))
            {
                return false;
            }

            var memberAccess = nameNode.Parent as MemberAccessExpressionSyntax;
            var memberBinding = nameNode.Parent as MemberBindingExpressionSyntax;
            if (memberAccess.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) ||
                memberAccess.IsParentKind(SyntaxKind.ElementAccessExpression) ||
                memberBinding.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) ||
                memberBinding.IsParentKind(SyntaxKind.ElementAccessExpression))
            {
                return false;
            }

            if (!syntaxFacts.IsNameOfSimpleMemberAccessExpression(node) &&
                !syntaxFacts.IsNameOfMemberBindingExpression(node))
            {
                return false;
            }

            return true;
        }

        protected override bool CanAddImportForDeconstruct(string diagnosticId, SyntaxNode node)
            => diagnosticId == CS8129;

        protected override bool CanAddImportForGetAwaiter(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node)
            => (diagnosticId == CS1061 || // Regular cases
                diagnosticId == CS4036 || // WinRT async interfaces
                diagnosticId == CS1929) && // An extension `GetAwaiter()` is in scope, but for another type
                AncestorOrSelfIsAwaitExpression(syntaxFactsService, node);

        protected override bool CanAddImportForGetEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node)
            => diagnosticId is CS1579 or CS8414;

        protected override bool CanAddImportForGetAsyncEnumerator(string diagnosticId, ISyntaxFacts syntaxFactsService, SyntaxNode node)
            => diagnosticId is CS8411 or CS8415;

        protected override bool CanAddImportForNamespace(string diagnosticId, SyntaxNode node, out SimpleNameSyntax nameNode)
        {
            nameNode = null;
            return false;
        }

        protected override bool CanAddImportForQuery(string diagnosticId, SyntaxNode node)
            => (diagnosticId == CS1935 || // Regular cases
                diagnosticId == CS1929) && // An extension method is in scope, but for another type
                node.AncestorsAndSelf().Any(n => n is QueryExpressionSyntax && !(n.Parent is QueryContinuationSyntax));

        protected override bool CanAddImportForType(string diagnosticId, SyntaxNode node, out SimpleNameSyntax nameNode)
        {
            nameNode = null;
            switch (diagnosticId)
            {
                case CS0103:
                case IDEDiagnosticIds.UnboundIdentifierId:
                case CS0246:
                case CS0305:
                case CS0308:
                case CS0122:
                case CS0307:
                case CS0616:
                case CS1580:
                case CS1581:
                case CS1955:
                case CS0281:
                    break;

                case CS1574:
                case CS1584:
                    if (node is QualifiedCrefSyntax cref)
                    {
                        node = cref.Container;
                    }

                    break;

                default:
                    return false;
            }

            return TryFindStandaloneType(node, out nameNode);
        }

        private static bool TryFindStandaloneType(SyntaxNode node, out SimpleNameSyntax nameNode)
        {
            if (node is QualifiedNameSyntax qn)
            {
                node = GetLeftMostSimpleName(qn);
            }

            nameNode = node as SimpleNameSyntax;
            return nameNode.LooksLikeStandaloneTypeName();
        }

        private static SimpleNameSyntax GetLeftMostSimpleName(QualifiedNameSyntax qn)
        {
            while (qn != null)
            {
                var left = qn.Left;
                if (left is SimpleNameSyntax simpleName)
                {
                    return simpleName;
                }

                qn = left as QualifiedNameSyntax;
            }

            return null;
        }

        protected override ISet<INamespaceSymbol> GetImportNamespacesInScope(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            return semanticModel.GetUsingNamespacesInScope(node);
        }

        protected override ITypeSymbol GetDeconstructInfo(
            SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            return semanticModel.GetTypeInfo(node, cancellationToken).Type;
        }

        protected override ITypeSymbol GetQueryClauseInfo(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var query = node.AncestorsAndSelf().OfType<QueryExpressionSyntax>().First();

            if (InfoBoundSuccessfully(semanticModel.GetQueryClauseInfo(query.FromClause, cancellationToken)))
            {
                return null;
            }

            foreach (var clause in query.Body.Clauses)
            {
                if (InfoBoundSuccessfully(semanticModel.GetQueryClauseInfo(clause, cancellationToken)))
                {
                    return null;
                }
            }

            if (InfoBoundSuccessfully(semanticModel.GetSymbolInfo(query.Body.SelectOrGroup, cancellationToken)))
            {
                return null;
            }

            var fromClause = query.FromClause;
            return semanticModel.GetTypeInfo(fromClause.Expression, cancellationToken).Type;
        }

        private static bool InfoBoundSuccessfully(SymbolInfo symbolInfo)
            => InfoBoundSuccessfully(symbolInfo.Symbol);

        private static bool InfoBoundSuccessfully(QueryClauseInfo semanticInfo)
            => InfoBoundSuccessfully(semanticInfo.OperationInfo);

        private static bool InfoBoundSuccessfully(ISymbol operation)
        {
            operation = operation.GetOriginalUnreducedDefinition();
            return operation != null;
        }

        protected override string GetDescription(IReadOnlyList<string> nameParts)
            => $"using { string.Join(".", nameParts) };";

        protected override (string description, bool hasExistingImport) GetDescription(
            Document document,
            AddImportPlacementOptions options,
            INamespaceOrTypeSymbol namespaceOrTypeSymbol,
            SemanticModel semanticModel,
            SyntaxNode contextNode,
            CancellationToken cancellationToken)
        {
            var root = GetCompilationUnitSyntaxNode(contextNode, cancellationToken);

            // See if this is a reference to a type from a reference that has a specific alias
            // associated with it.  If that extern alias hasn't already been brought into scope
            // then add that one.
            var (externAlias, hasExistingExtern) = GetExternAliasDirective(
                namespaceOrTypeSymbol, semanticModel, contextNode);

            var (usingDirective, hasExistingUsing) = GetUsingDirective(
                document, options, namespaceOrTypeSymbol, semanticModel, root, contextNode);

            var externAliasString = externAlias != null ? $"extern alias {externAlias.Identifier.ValueText};" : null;
            var usingDirectiveString = usingDirective != null ? GetUsingDirectiveString(namespaceOrTypeSymbol) : null;

            if (externAlias == null && usingDirective == null)
            {
                return (null, false);
            }

            if (externAlias != null && !hasExistingExtern)
            {
                return (externAliasString, false);
            }

            if (usingDirective != null && !hasExistingUsing)
            {
                return (usingDirectiveString, false);
            }

            return externAlias != null
                ? (externAliasString, hasExistingExtern)
                : (usingDirectiveString, hasExistingUsing);
        }

        private static string GetUsingDirectiveString(INamespaceOrTypeSymbol namespaceOrTypeSymbol)
        {
            var displayString = namespaceOrTypeSymbol.ToDisplayString();
            return namespaceOrTypeSymbol.IsKind(SymbolKind.Namespace)
                ? $"using {displayString};"
                : $"using static {displayString};";
        }

        protected override async Task<Document> AddImportAsync(
            SyntaxNode contextNode,
            INamespaceOrTypeSymbol namespaceOrTypeSymbol,
            Document document,
            AddImportPlacementOptions options,
            CancellationToken cancellationToken)
        {
            var root = GetCompilationUnitSyntaxNode(contextNode, cancellationToken);
            var newRoot = await AddImportWorkerAsync(document, root, contextNode, namespaceOrTypeSymbol, options, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<CompilationUnitSyntax> AddImportWorkerAsync(
            Document document, CompilationUnitSyntax root, SyntaxNode contextNode, INamespaceOrTypeSymbol namespaceOrTypeSymbol,
            AddImportPlacementOptions options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var (externAliasDirective, hasExistingExtern) = GetExternAliasDirective(
                namespaceOrTypeSymbol, semanticModel, contextNode);

            var (usingDirective, hasExistingUsing) = GetUsingDirective(
                document, options, namespaceOrTypeSymbol, semanticModel, root, contextNode);

            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var newImports);

            if (!hasExistingExtern && externAliasDirective != null)
            {
                newImports.Add(externAliasDirective);
            }

            if (!hasExistingUsing && usingDirective != null)
            {
                newImports.Add(usingDirective);
            }

            if (newImports.Count == 0)
            {
                return root;
            }

            var addImportService = document.GetLanguageService<IAddImportsService>();
            var generator = SyntaxGenerator.GetGenerator(document);
            var newRoot = addImportService.AddImports(
                semanticModel.Compilation, root, contextNode, newImports, generator, options, cancellationToken);
            return (CompilationUnitSyntax)newRoot;
        }

        protected override async Task<Document> AddImportAsync(
            SyntaxNode contextNode, IReadOnlyList<string> namespaceParts,
            Document document, AddImportPlacementOptions options, CancellationToken cancellationToken)
        {
            var root = GetCompilationUnitSyntaxNode(contextNode, cancellationToken);

            var usingDirective = SyntaxFactory.UsingDirective(
                CreateNameSyntax(namespaceParts, namespaceParts.Count - 1));

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var service = document.GetLanguageService<IAddImportsService>();
            var generator = SyntaxGenerator.GetGenerator(document);
            var newRoot = service.AddImport(
                compilation, root, contextNode, usingDirective, generator, options, cancellationToken);

            return document.WithSyntaxRoot(newRoot);
        }

        private NameSyntax CreateNameSyntax(IReadOnlyList<string> namespaceParts, int index)
        {
            var part = namespaceParts[index];
            if (SyntaxFacts.GetKeywordKind(part) != SyntaxKind.None)
            {
                part = "@" + part;
            }

            var namePiece = SyntaxFactory.IdentifierName(part);
            return index == 0
                ? namePiece
                : SyntaxFactory.QualifiedName(CreateNameSyntax(namespaceParts, index - 1), namePiece);
        }

        private static (ExternAliasDirectiveSyntax, bool hasExistingImport) GetExternAliasDirective(
            INamespaceOrTypeSymbol namespaceSymbol,
            SemanticModel semanticModel,
            SyntaxNode contextNode)
        {
            var (val, hasExistingExtern) = GetExternAliasString(namespaceSymbol, semanticModel, contextNode);
            if (val == null)
            {
                return (null, false);
            }

            return (SyntaxFactory.ExternAliasDirective(SyntaxFactory.Identifier(val))
                                 .WithAdditionalAnnotations(Formatter.Annotation),
                    hasExistingExtern);
        }

        private (UsingDirectiveSyntax, bool hasExistingImport) GetUsingDirective(
            Document document,
            AddImportPlacementOptions options,
            INamespaceOrTypeSymbol namespaceOrTypeSymbol,
            SemanticModel semanticModel,
            CompilationUnitSyntax root,
            SyntaxNode contextNode)
        {
            var addImportService = document.GetLanguageService<IAddImportsService>();
            var generator = SyntaxGenerator.GetGenerator(document);

            var nameSyntax = namespaceOrTypeSymbol.GenerateNameSyntax();

            // We need to create our using in two passes.  This is because we need a using
            // directive so we can figure out where to put it.  Then, once we figure out
            // where to put it, we might need to change it a bit (e.g. removing 'global' 
            // from it if necessary).  So we first create a dummy using directive just to
            // determine which container we're going in.  Then we'll use the container to
            // help create the final using.
            var dummyUsing = SyntaxFactory.UsingDirective(nameSyntax);

            var container = addImportService.GetImportContainer(root, contextNode, dummyUsing, options);
            var namespaceToAddTo = container as BaseNamespaceDeclarationSyntax;

            // Replace the alias that GenerateTypeSyntax added if we want this to be looked
            // up off of an extern alias.
            var (externAliasDirective, _) = GetExternAliasDirective(
                namespaceOrTypeSymbol, semanticModel, contextNode);

            var externAlias = externAliasDirective?.Identifier.ValueText;
            if (externAlias != null)
            {
                nameSyntax = AddOrReplaceAlias(nameSyntax, SyntaxFactory.IdentifierName(externAlias));
            }
            else
            {
                // The name we generated will have the global:: alias on it.  We only need
                // that if the name of our symbol is actually ambiguous in this context.
                // If so, keep global:: on it, otherwise remove it.
                //
                // Note: doing this has a couple of benefits.  First, it's easy for us to see
                // if we have an existing using for this with the same syntax.  Second,
                // it's easy to sort usings properly.  If "global::" was attached to the 
                // using directive, then it would make both of those operations more difficult
                // to achieve.
                nameSyntax = RemoveGlobalAliasIfUnnecessary(semanticModel, nameSyntax, namespaceToAddTo);
            }

            var usingDirective = SyntaxFactory.UsingDirective(nameSyntax)
                                              .WithAdditionalAnnotations(Formatter.Annotation);

            usingDirective = namespaceOrTypeSymbol.IsKind(SymbolKind.Namespace)
                ? usingDirective
                : usingDirective.WithStaticKeyword(SyntaxFactory.Token(SyntaxKind.StaticKeyword));

            return (usingDirective, addImportService.HasExistingImport(semanticModel.Compilation, root, contextNode, usingDirective, generator));
        }

        private static NameSyntax RemoveGlobalAliasIfUnnecessary(
            SemanticModel semanticModel,
            NameSyntax nameSyntax,
            BaseNamespaceDeclarationSyntax namespaceToAddTo)
        {
            var aliasQualifiedName = nameSyntax.DescendantNodesAndSelf()
                                               .OfType<AliasQualifiedNameSyntax>()
                                               .FirstOrDefault();
            if (aliasQualifiedName != null)
            {
                var rightOfAliasName = aliasQualifiedName.Name.Identifier.ValueText;
                if (!ConflictsWithExistingMember(semanticModel, namespaceToAddTo, rightOfAliasName))
                {
                    // Strip off the alias.
                    return nameSyntax.ReplaceNode(aliasQualifiedName, aliasQualifiedName.Name);
                }
            }

            return nameSyntax;
        }

        private static bool ConflictsWithExistingMember(
            SemanticModel semanticModel,
            BaseNamespaceDeclarationSyntax namespaceToAddTo,
            string rightOfAliasName)
        {
            if (namespaceToAddTo != null)
            {
                var containingNamespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceToAddTo);

                while (containingNamespaceSymbol != null && !containingNamespaceSymbol.IsGlobalNamespace)
                {
                    if (containingNamespaceSymbol.GetMembers(rightOfAliasName).Any())
                    {
                        // A containing namespace had this name in it.  We need to stay globally qualified.
                        return true;
                    }

                    containingNamespaceSymbol = containingNamespaceSymbol.ContainingNamespace;
                }
            }

            // Didn't conflict with anything.  We should remove the global:: alias.
            return false;
        }

        private NameSyntax AddOrReplaceAlias(
            NameSyntax nameSyntax, IdentifierNameSyntax alias)
        {
            if (nameSyntax is SimpleNameSyntax simpleName)
            {
                return SyntaxFactory.AliasQualifiedName(alias, simpleName);
            }

            if (nameSyntax is QualifiedNameSyntax qualifiedName)
            {
                return qualifiedName.WithLeft(AddOrReplaceAlias(qualifiedName.Left, alias));
            }

            var aliasName = nameSyntax as AliasQualifiedNameSyntax;
            return aliasName.WithAlias(alias);
        }

        private static (string, bool hasExistingImport) GetExternAliasString(
            INamespaceOrTypeSymbol namespaceSymbol,
            SemanticModel semanticModel,
            SyntaxNode contextNode)
        {
            string externAliasString = null;
            var metadataReference = semanticModel.Compilation.GetMetadataReference(namespaceSymbol.ContainingAssembly);
            if (metadataReference == null)
            {
                return (null, false);
            }

            var aliases = metadataReference.Properties.Aliases;
            if (aliases.IsEmpty)
            {
                return (null, false);
            }

            aliases = metadataReference.Properties.Aliases.Where(a => a != MetadataReferenceProperties.GlobalAlias).ToImmutableArray();
            if (!aliases.Any())
            {
                return (null, false);
            }

            // Just default to using the first alias we see for this symbol.
            externAliasString = aliases.First();
            return (externAliasString, HasExistingExternAlias(externAliasString, contextNode));
        }

        private static bool HasExistingExternAlias(string alias, SyntaxNode contextNode)
        {
            foreach (var externAlias in contextNode.GetEnclosingExternAliasDirectives())
            {
                // We already have this extern alias in scope.  No need to add it.
                if (externAlias.Identifier.ValueText == alias)
                {
                    return true;
                }
            }

            return false;
        }

        private static CompilationUnitSyntax GetCompilationUnitSyntaxNode(
            SyntaxNode contextNode, CancellationToken cancellationToken)
        {
            return (CompilationUnitSyntax)contextNode.SyntaxTree.GetRoot(cancellationToken);
        }

        protected override bool IsViableExtensionMethod(IMethodSymbol method, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFacts syntaxFacts, CancellationToken cancellationToken)
        {
            var leftExpression = syntaxFacts.IsMemberAccessExpression(expression)
                ? syntaxFacts.GetExpressionOfMemberAccessExpression(expression)
                : syntaxFacts.GetTargetOfMemberBinding(expression);
            if (leftExpression == null)
            {
                if (expression.IsKind(SyntaxKind.CollectionInitializerExpression))
                {
                    leftExpression = expression.GetAncestor<ObjectCreationExpressionSyntax>();
                }
                else
                {
                    return false;
                }
            }

            var semanticInfo = semanticModel.GetTypeInfo(leftExpression, cancellationToken);
            var leftExpressionType = semanticInfo.Type;

            return IsViableExtensionMethod(method, leftExpressionType);
        }

        protected override bool IsAddMethodContext(SyntaxNode node, SemanticModel semanticModel)
        {
            if (node.Parent.IsKind(SyntaxKind.CollectionInitializerExpression))
            {
                var objectCreationExpressionSyntax = node.GetAncestor<ObjectCreationExpressionSyntax>();
                if (objectCreationExpressionSyntax == null)
                {
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}
