// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.AddImport;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SymbolSearch;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport.AddImportDiagnosticIds;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddImport
{

    internal static class AddImportDiagnosticIds
    {
        /// <summary>
        /// name does not exist in context
        /// </summary>
        public const string CS0103 = nameof(CS0103);

        /// <summary>
        /// type or namespace could not be found
        /// </summary>
        public const string CS0246 = nameof(CS0246);

        /// <summary>
        /// wrong number of type args
        /// </summary>
        public const string CS0305 = nameof(CS0305);

        /// <summary>
        /// type does not contain a definition of method or extension method
        /// </summary>
        public const string CS1061 = nameof(CS1061);

        /// <summary>
        /// cannot find implementation of query pattern
        /// </summary>
        public const string CS1935 = nameof(CS1935);

        /// <summary>
        /// The non-generic type 'A' cannot be used with type arguments
        /// </summary>
        public const string CS0308 = nameof(CS0308);

        /// <summary>
        /// 'A' is inaccessible due to its protection level
        /// </summary>
        public const string CS0122 = nameof(CS0122);

        /// <summary>
        /// The using alias 'A' cannot be used with type arguments
        /// </summary>
        public const string CS0307 = nameof(CS0307);

        /// <summary>
        /// 'A' is not an attribute class
        /// </summary>
        public const string CS0616 = nameof(CS0616);

        /// <summary>
        ///  No overload for method 'X' takes 'N' arguments
        /// </summary>
        public const string CS1501 = nameof(CS1501);

        /// <summary>
        /// cannot convert from 'int' to 'string'
        /// </summary>
        public const string CS1503 = nameof(CS1503);

        /// <summary>
        /// XML comment on 'construct' has syntactically incorrect cref attribute 'name'
        /// </summary>
        public const string CS1574 = nameof(CS1574);

        /// <summary>
        /// Invalid type for parameter 'parameter number' in XML comment cref attribute
        /// </summary>
        public const string CS1580 = nameof(CS1580);

        /// <summary>
        /// Invalid return type in XML comment cref attribute
        /// </summary>
        public const string CS1581 = nameof(CS1581);

        /// <summary>
        /// XML comment has syntactically incorrect cref attribute
        /// </summary>
        public const string CS1584 = nameof(CS1584);

        /// <summary>
        /// Type 'X' does not contain a valid extension method accepting 'Y'
        /// </summary>
        public const string CS1929 = nameof(CS1929);

        /// <summary>
        /// Cannot convert method group 'X' to non-delegate type 'Y'. Did you intend to invoke the method?
        /// </summary>
        public const string CS0428 = nameof(CS0428);

        /// <summary>
        ///  There is no argument given that corresponds to the required formal parameter 'X' of 'Y'
        /// </summary>
        public const string CS7036 = nameof(CS7036);

        public static ImmutableArray<string> FixableTypeIds =
            ImmutableArray.Create(
                CS0103,
                CS0246,
                CS0305,
                CS0308,
                CS0122,
                CS0307,
                CS0616,
                CS1580,
                CS1581);

        public static ImmutableArray<string> FixableDiagnosticIds =
            FixableTypeIds.Concat(ImmutableArray.Create(
                    CS1061,
                    CS1935,
                    CS1501,
                    CS1503,
                    CS1574,
                    CS1584,
                    CS1929,
                    CS0428,
                    CS7036));
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddUsingOrImport), Shared]
    internal class CSharpAddImportCodeFixProvider : AbstractAddImportCodeFixProvider<SimpleNameSyntax>
    {
        public override ImmutableArray<string> FixableDiagnosticIds => AddImportDiagnosticIds.FixableDiagnosticIds;

        public CSharpAddImportCodeFixProvider()
        {
        }

        /// <summary>For testing purposes only (so that tests can pass in mock values)</summary> 
        internal CSharpAddImportCodeFixProvider(
            IPackageInstallerService installerService,
            ISymbolSearchService symbolSearchService)
            : base(installerService, symbolSearchService)
        {
        }

        protected override bool CanAddImport(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            return node.CanAddUsingDirectives(cancellationToken);
        }

        protected override bool CanAddImportForMethod(
            Diagnostic diagnostic, ISyntaxFactsService syntaxFacts, SyntaxNode node, out SimpleNameSyntax nameNode)
        {
            nameNode = null;

            switch (diagnostic.Id)
            {
                case CS7036:
                case CS0428:
                case CS1061:
                    if (node.IsKind(SyntaxKind.ConditionalAccessExpression))
                    {
                        node = (node as ConditionalAccessExpressionSyntax).WhenNotNull;
                    }
                    else if (node.IsKind(SyntaxKind.MemberBindingExpression))
                    {
                        node = (node as MemberBindingExpressionSyntax).Name;
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
                    else if (node is MemberBindingExpressionSyntax)
                    {
                        node = (node as MemberBindingExpressionSyntax).Name;
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

                    var method = parent.Expression as MemberAccessExpressionSyntax;
                    if (method != null)
                    {
                        node = method.Name;
                    }

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

            if (!syntaxFacts.IsMemberAccessExpressionName(node))
            {
                return false;
            }

            return true;
        }

        protected override bool CanAddImportForNamespace(Diagnostic diagnostic, SyntaxNode node, out SimpleNameSyntax nameNode)
        {
            nameNode = null;
            return false;
        }

        protected override bool CanAddImportForQuery(Diagnostic diagnostic, SyntaxNode node)
        {
            if (diagnostic.Id != CS1935)
            {
                return false;
            }

            return node.AncestorsAndSelf().Any(n => n is QueryExpressionSyntax && !(n.Parent is QueryContinuationSyntax));
        }

        protected override bool CanAddImportForType(Diagnostic diagnostic, SyntaxNode node, out SimpleNameSyntax nameNode)
        {
            nameNode = null;
            switch (diagnostic.Id)
            {
                case CS0103:
                case CS0246:
                case CS0305:
                case CS0308:
                case CS0122:
                case CS0307:
                case CS0616:
                case CS1580:
                case CS1581:
                    break;

                case CS1574:
                case CS1584:
                    var cref = node as QualifiedCrefSyntax;
                    if (cref != null)
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
            var qn = node as QualifiedNameSyntax;
            if (qn != null)
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
                var simpleName = left as SimpleNameSyntax;
                if (simpleName != null)
                {
                    return simpleName;
                }

                qn = left as QualifiedNameSyntax;
            }

            return null;
        }

        protected override ISet<INamespaceSymbol> GetNamespacesInScope(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            return semanticModel.GetUsingNamespacesInScope(node);
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

        private bool InfoBoundSuccessfully(SymbolInfo symbolInfo)
        {
            return InfoBoundSuccessfully(symbolInfo.Symbol);
        }

        private bool InfoBoundSuccessfully(QueryClauseInfo semanticInfo)
        {
            return InfoBoundSuccessfully(semanticInfo.OperationInfo);
        }

        private static bool InfoBoundSuccessfully(ISymbol operation)
        {
            operation = operation.GetOriginalUnreducedDefinition();
            return operation != null;
        }

        protected override string GetDescription(IReadOnlyList<string> nameParts)
        {
            return $"using { string.Join(".", nameParts) };";
        }

        protected override string GetDescription(
            INamespaceOrTypeSymbol namespaceSymbol, SemanticModel semanticModel, SyntaxNode contextNode, bool checkForExistingUsing)
        {
            var root = GetCompilationUnitSyntaxNode(contextNode);

            // No localization necessary
            string externAliasString;
            if (TryGetExternAliasString(namespaceSymbol, semanticModel, root, out externAliasString))
            {
                return $"extern alias {externAliasString};";
            }

            string namespaceString;
            if (TryGetNamespaceString(namespaceSymbol, root, false, null, checkForExistingUsing, out namespaceString))
            {
                return $"using {namespaceString};";
            }

            string staticNamespaceString;
            if (TryGetStaticNamespaceString(namespaceSymbol, root, false, null, out staticNamespaceString))
            {
                return $"using static {staticNamespaceString};";
            }

            // We can't find a string to show to the user, we should not show this codefix.
            return null;
        }

        protected override async Task<Document> AddImportAsync(
            SyntaxNode contextNode,
            INamespaceOrTypeSymbol namespaceOrTypeSymbol,
            Document document,
            bool placeSystemNamespaceFirst,
            CancellationToken cancellationToken)
        {
            var root = GetCompilationUnitSyntaxNode(contextNode, cancellationToken);
            var newRoot = await AddImportWorkerAsync(document, root, contextNode, namespaceOrTypeSymbol, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<CompilationUnitSyntax> AddImportWorkerAsync(
            Document document, CompilationUnitSyntax root, SyntaxNode contextNode,
            INamespaceOrTypeSymbol namespaceOrTypeSymbol, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var simpleUsingDirective = GetUsingDirective(root, namespaceOrTypeSymbol, semanticModel, fullyQualify: false);
            var externAliasUsingDirective = GetExternAliasUsingDirective(root, namespaceOrTypeSymbol, semanticModel);

            if (externAliasUsingDirective != null)
            {
                root = root.AddExterns(
                    externAliasUsingDirective
                        .WithAdditionalAnnotations(Formatter.Annotation));
            }

            if (simpleUsingDirective == null)
            {
                return root;
            }

            // Because of the way usings can be nested inside of namespace declarations,
            // we need to check if the usings must be fully qualified so as not to be
            // ambiguous with the containing namespace. 
            if (UsingsAreContainedInNamespace(contextNode))
            {
                // When we add usings we try and place them, as best we can, where the user
                // wants them according to their settings.  This means we can't just add the fully-
                // qualified usings and expect the simplifier to take care of it, the usings have to be
                // simplified before we attempt to add them to the document.
                // You might be tempted to think that we could call 
                //   AddUsings -> Simplifier -> SortUsings
                // But this will clobber the users using settings without asking.  Instead we create a new
                // Document and check if our using can be simplified.  Worst case we need to back out the 
                // fully qualified change and reapply with the simple name.
                var fullyQualifiedUsingDirective = GetUsingDirective(root, namespaceOrTypeSymbol, semanticModel, fullyQualify: true);
                var newRoot = root.AddUsingDirective(
                                fullyQualifiedUsingDirective, contextNode, placeSystemNamespaceFirst,
                                Formatter.Annotation);
                var newUsing = newRoot
                    .DescendantNodes().OfType<UsingDirectiveSyntax>()
                    .Where(uds => uds.IsEquivalentTo(fullyQualifiedUsingDirective, topLevel: true))
                    .Single();
                newRoot = newRoot.TrackNodes(newUsing);
                var documentWithSyntaxRoot = document.WithSyntaxRoot(newRoot);
                var simplifiedDocument = await Simplifier.ReduceAsync(documentWithSyntaxRoot, newUsing.Span, cancellationToken: cancellationToken).ConfigureAwait(false);

                newRoot = (CompilationUnitSyntax)await simplifiedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var simplifiedUsing = newRoot.GetCurrentNode(newUsing);
                if (simplifiedUsing.Name.IsEquivalentTo(newUsing.Name, topLevel: true))
                {
                    // Not fully qualifying the using causes to refer to a different namespace so we need to keep it as is.
                    return (CompilationUnitSyntax)await documentWithSyntaxRoot.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // It does not matter if it is fully qualified or simple so lets return the simple name.
                    return root.AddUsingDirective(
                        simplifiedUsing.WithoutAnnotations(), contextNode, placeSystemNamespaceFirst,
                        Formatter.Annotation);
                }
            }
            else
            {
                // simple form
                return root.AddUsingDirective(
                    simpleUsingDirective, contextNode, placeSystemNamespaceFirst,
                    Formatter.Annotation);
            }
        }

        protected override Task<Document> AddImportAsync(
            SyntaxNode contextNode, IReadOnlyList<string> namespaceParts, Document document, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
        {
            var root = GetCompilationUnitSyntaxNode(contextNode, cancellationToken);

            // Suppress diagnostics on the import we create.  Because we only get here when we are 
            // adding a nuget package, it is certainly the case that in the preview this will not
            // bind properly.  It will look silly to show such an error, so we just suppress things.
            var simpleUsingDirective = SyntaxFactory.UsingDirective(
                CreateNameSyntax(namespaceParts, namespaceParts.Count - 1)).WithAdditionalAnnotations(
                    SuppressDiagnosticsAnnotation.Create());

            // If we have an existing using with this name then don't bother adding this new using.
            if (root.Usings.Any(u => u.IsEquivalentTo(simpleUsingDirective, topLevel: false)))
            {
                return Task.FromResult(document);
            }

            var newRoot = root.AddUsingDirective(
                simpleUsingDirective, contextNode, placeSystemNamespaceFirst,
                Formatter.Annotation);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
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
                ? (NameSyntax)namePiece
                : SyntaxFactory.QualifiedName(CreateNameSyntax(namespaceParts, index - 1), namePiece);
        }

        private static ExternAliasDirectiveSyntax GetExternAliasUsingDirective(CompilationUnitSyntax root, INamespaceOrTypeSymbol namespaceSymbol, SemanticModel semanticModel)
        {
            string externAliasString;
            if (TryGetExternAliasString(namespaceSymbol, semanticModel, root, out externAliasString))
            {
                return SyntaxFactory.ExternAliasDirective(SyntaxFactory.Identifier(externAliasString));
            }

            return null;
        }

        private UsingDirectiveSyntax GetUsingDirective(CompilationUnitSyntax root, INamespaceOrTypeSymbol namespaceSymbol, SemanticModel semanticModel, bool fullyQualify)
        {
            if (namespaceSymbol is INamespaceSymbol)
            {
                string namespaceString;
                string externAliasString;
                TryGetExternAliasString(namespaceSymbol, semanticModel, root, out externAliasString);
                if (externAliasString != null)
                {
                    if (TryGetNamespaceString(namespaceSymbol, root, false, externAliasString, 
                        checkForExistingUsing: true, namespaceString: out namespaceString))
                    {
                        return SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceString).WithAdditionalAnnotations(Simplifier.Annotation));
                    }

                    return null;
                }

                if (TryGetNamespaceString(namespaceSymbol, root, fullyQualify, null,
                    checkForExistingUsing: true, namespaceString: out namespaceString))
                {
                    return SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceString).WithAdditionalAnnotations(Simplifier.Annotation));
                }
            }

            if (namespaceSymbol is ITypeSymbol)
            {
                string staticNamespaceString;
                if (TryGetStaticNamespaceString(namespaceSymbol, root, fullyQualify, null, out staticNamespaceString))
                {
                    return SyntaxFactory.UsingDirective(
                        SyntaxFactory.Token(SyntaxKind.UsingKeyword),
                        SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                        null,
                        SyntaxFactory.ParseName(staticNamespaceString).WithAdditionalAnnotations(Simplifier.Annotation),
                        SyntaxFactory.Token(SyntaxKind.SemicolonToken));
                }
            }

            return null;
        }

        private bool UsingsAreContainedInNamespace(SyntaxNode contextNode)
        {
            return contextNode.GetAncestor<NamespaceDeclarationSyntax>()?.DescendantNodes().OfType<UsingDirectiveSyntax>().FirstOrDefault() != null;
        }

        private static bool TryGetExternAliasString(INamespaceOrTypeSymbol namespaceSymbol, SemanticModel semanticModel, CompilationUnitSyntax root, out string externAliasString)
        {
            externAliasString = null;
            var metadataReference = semanticModel.Compilation.GetMetadataReference(namespaceSymbol.ContainingAssembly);
            if (metadataReference == null)
            {
                return false;
            }

            var aliases = metadataReference.Properties.Aliases;
            if (aliases.IsEmpty)
            {
                return false;
            }

            aliases = metadataReference.Properties.Aliases.Where(a => a != MetadataReferenceProperties.GlobalAlias).ToImmutableArray();
            if (!aliases.Any())
            {
                return false;
            }

            externAliasString = aliases.First();
            return ShouldAddExternAlias(aliases, root);
        }

        private static bool TryGetNamespaceString(
            INamespaceOrTypeSymbol namespaceSymbol, CompilationUnitSyntax root, bool fullyQualify, string alias,
            bool checkForExistingUsing, out string namespaceString)
        {
            if (namespaceSymbol is ITypeSymbol)
            {
                namespaceString = null;
                return false;
            }

            namespaceString = fullyQualify
                    ? namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : namespaceSymbol.ToDisplayString();

            if (alias != null)
            {
                namespaceString = alias + "::" + namespaceString;
            }

            return checkForExistingUsing
                ? ShouldAddUsing(namespaceString, root)
                : true;
        }

        private static bool TryGetStaticNamespaceString(INamespaceOrTypeSymbol namespaceSymbol, CompilationUnitSyntax root, bool fullyQualify, string alias, out string namespaceString)
        {
            if (namespaceSymbol is INamespaceSymbol)
            {
                namespaceString = null;
                return false;
            }

            namespaceString = fullyQualify
                    ? namespaceSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : namespaceSymbol.ToDisplayString();

            if (alias != null)
            {
                namespaceString = alias + "::" + namespaceString;
            }

            return ShouldAddStaticUsing(namespaceString, root);
        }

        private static bool ShouldAddExternAlias(ImmutableArray<string> aliases, CompilationUnitSyntax root)
        {
            var identifiers = root.DescendantNodes().OfType<ExternAliasDirectiveSyntax>().Select(e => e.Identifier.ToString());
            var externAliases = aliases.Where(a => identifiers.Contains(a));
            return !externAliases.Any();
        }

        private static bool ShouldAddUsing(string usingDirective, CompilationUnitSyntax root)
        {
            var simpleUsings = root.Usings.Where(u => u.StaticKeyword.IsKind(SyntaxKind.None));
            return !simpleUsings.Any(u => u.Name.ToString() == usingDirective);
        }

        private static bool ShouldAddStaticUsing(string usingDirective, CompilationUnitSyntax root)
        {
            var staticUsings = root.Usings.Where(u => u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword));
            return !staticUsings.Any(u => u.Name.ToString() == usingDirective);
        }

        private static CompilationUnitSyntax GetCompilationUnitSyntaxNode(SyntaxNode contextNode, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (CompilationUnitSyntax)contextNode.SyntaxTree.GetRoot(cancellationToken);
        }

        protected override bool IsViableExtensionMethod(IMethodSymbol method, SyntaxNode expression, SemanticModel semanticModel, ISyntaxFactsService syntaxFacts, CancellationToken cancellationToken)
        {
            var leftExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(expression);
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

        internal override bool IsAddMethodContext(SyntaxNode node, SemanticModel semanticModel)
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
