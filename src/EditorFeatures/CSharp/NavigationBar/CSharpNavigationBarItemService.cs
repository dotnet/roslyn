// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Extensibility.NavigationBar;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.NavigationBar
{
    [ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.CSharp), Shared]
    internal class CSharpNavigationBarItemService : AbstractNavigationBarItemService
    {
        private static readonly SymbolDisplayFormat s_typeFormat =
            SymbolDisplayFormat.CSharpErrorMessageFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeVariance);

        private static readonly SymbolDisplayFormat s_memberFormat =
            new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters |
                               SymbolDisplayMemberOptions.IncludeExplicitInterface,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                                  SymbolDisplayParameterOptions.IncludeName |
                                  SymbolDisplayParameterOptions.IncludeDefaultValue |
                                  SymbolDisplayParameterOptions.IncludeParamsRefOut,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                                      SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral |
                                      SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        [ImportingConstructor]
        public CSharpNavigationBarItemService()
        {
        }

        public override async Task<IList<NavigationBarItem>> GetItemsAsync(Document document, CancellationToken cancellationToken)
        {
            var typesInFile = await GetTypesInFileAsync(document, cancellationToken).ConfigureAwait(false);
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return GetMembersInTypes(tree, typesInFile, cancellationToken);
        }

        private IList<NavigationBarItem> GetMembersInTypes(
            SyntaxTree tree, IEnumerable<INamedTypeSymbol> types, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.NavigationBar_ItemService_GetMembersInTypes_CSharp, cancellationToken))
            {
                var typeSymbolIndexProvider = new NavigationBarSymbolIdIndexProvider(caseSensitive: true);
                var items = new List<NavigationBarItem>();

                foreach (var type in types)
                {
                    var memberSymbolIndexProvider = new NavigationBarSymbolIdIndexProvider(caseSensitive: true);

                    var memberItems = new List<NavigationBarItem>();
                    foreach (var member in type.GetMembers())
                    {
                        if (member.IsImplicitlyDeclared ||
                            member.Kind == SymbolKind.NamedType ||
                            IsAccessor(member))
                        {
                            continue;
                        }

                        var method = member as IMethodSymbol;
                        if (method is { PartialImplementationPart: { } })
                        {
                            memberItems.Add(CreateItemForMember(
                                method,
                                memberSymbolIndexProvider.GetIndexForSymbolId(method.GetSymbolKey()),
                                tree,
                                cancellationToken));

                            memberItems.Add(CreateItemForMember(
                                method.PartialImplementationPart,
                                memberSymbolIndexProvider.GetIndexForSymbolId(method.PartialImplementationPart.GetSymbolKey()),
                                tree,
                                cancellationToken));
                        }
                        else
                        {
                            Debug.Assert(method == null || method.PartialDefinitionPart == null, "NavBar expected GetMembers to return partial method definition parts but the implementation part was returned.");

                            memberItems.Add(CreateItemForMember(
                                member,
                                memberSymbolIndexProvider.GetIndexForSymbolId(member.GetSymbolKey()),
                                tree,
                                cancellationToken));
                        }
                    }

                    memberItems.Sort((x, y) =>
                    {
                        var textComparison = x.Text.CompareTo(y.Text);
                        return textComparison != 0 ? textComparison : x.Grayed.CompareTo(y.Grayed);
                    });

                    var symbolId = type.GetSymbolKey();
                    items.Add(new NavigationBarSymbolItem(
                        text: type.ToDisplayString(s_typeFormat),
                        glyph: type.GetGlyph(),
                        indent: 0,
                        spans: GetSpansInDocument(type, tree, cancellationToken),
                        navigationSymbolId: symbolId,
                        navigationSymbolIndex: typeSymbolIndexProvider.GetIndexForSymbolId(symbolId),
                        childItems: memberItems));
                }

                items.Sort((x1, x2) => x1.Text.CompareTo(x2.Text));
                return items;
            }
        }

        private async Task<IEnumerable<INamedTypeSymbol>> GetTypesInFileAsync(Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            return GetTypesInFile(semanticModel, cancellationToken);
        }

        private static IEnumerable<INamedTypeSymbol> GetTypesInFile(SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.NavigationBar_ItemService_GetTypesInFile_CSharp, cancellationToken))
            {
                var types = new HashSet<INamedTypeSymbol>();
                var nodesToVisit = new Stack<SyntaxNode>();

                nodesToVisit.Push(semanticModel.SyntaxTree.GetRoot(cancellationToken));

                while (!nodesToVisit.IsEmpty())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>();
                    }

                    var node = nodesToVisit.Pop();
                    var type = GetType(semanticModel, node, cancellationToken);

                    if (type != null)
                    {
                        types.Add((INamedTypeSymbol)type);
                    }

                    if (node is BaseMethodDeclarationSyntax ||
                        node is BasePropertyDeclarationSyntax ||
                        node is BaseFieldDeclarationSyntax ||
                        node is StatementSyntax ||
                        node is ExpressionSyntax)
                    {
                        // quick bail out to prevent us from creating every nodes exist in current file
                        continue;
                    }

                    foreach (var child in node.ChildNodes())
                    {
                        nodesToVisit.Push(child);
                    }
                }

                return types;
            }
        }

        private static ISymbol GetType(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => node switch
            {
                BaseTypeDeclarationSyntax t => semanticModel.GetDeclaredSymbol(t, cancellationToken),
                DelegateDeclarationSyntax d => semanticModel.GetDeclaredSymbol(d, cancellationToken),
                _ => null,
            };

        private static bool IsAccessor(ISymbol member)
        {
            if (member.Kind == SymbolKind.Method)
            {
                var method = (IMethodSymbol)member;

                return method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.PropertySet;
            }

            return false;
        }

        private NavigationBarItem CreateItemForMember(ISymbol member, int symbolIndex, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var spans = GetSpansInDocument(member, tree, cancellationToken);

            return new NavigationBarSymbolItem(
                member.ToDisplayString(s_memberFormat),
                member.GetGlyph(),
                spans,
                member.GetSymbolKey(),
                symbolIndex,
                grayed: spans.Count == 0);
        }

        private IList<TextSpan> GetSpansInDocument(ISymbol symbol, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var spans = new List<TextSpan>();
            if (!cancellationToken.IsCancellationRequested)
            {
                if (symbol.Kind == SymbolKind.Field)
                {
                    if (symbol.ContainingType.TypeKind == TypeKind.Enum)
                    {
                        AddEnumMemberSpan(symbol, tree, spans);
                    }
                    else
                    {
                        AddFieldSpan(symbol, tree, spans);
                    }
                }
                else
                {
                    foreach (var reference in symbol.DeclaringSyntaxReferences)
                    {
                        if (reference.SyntaxTree.Equals(tree))
                        {
                            var span = reference.Span;

                            spans.Add(span);
                        }
                    }
                }
            }

            return spans;
        }

        /// <summary>
        /// Computes a span for a given field symbol, expanding to the outer 
        /// </summary>
        private static void AddFieldSpan(ISymbol symbol, SyntaxTree tree, List<TextSpan> spans)
        {
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault(r => r.SyntaxTree == tree);
            if (reference == null)
            {
                return;
            }

            var declaringNode = reference.GetSyntax();

            var spanStart = declaringNode.SpanStart;
            var spanEnd = declaringNode.Span.End;

            var fieldDeclaration = declaringNode.GetAncestor<FieldDeclarationSyntax>();
            if (fieldDeclaration != null)
            {
                var variables = fieldDeclaration.Declaration.Variables;

                if (variables.FirstOrDefault() == declaringNode)
                {
                    spanStart = fieldDeclaration.SpanStart;
                }

                if (variables.LastOrDefault() == declaringNode)
                {
                    spanEnd = fieldDeclaration.Span.End;
                }
            }

            spans.Add(TextSpan.FromBounds(spanStart, spanEnd));
        }

        private static void AddEnumMemberSpan(ISymbol symbol, SyntaxTree tree, List<TextSpan> spans)
        {
            // Ideally we want the span of this to include the trailing comma, so let's find
            // the declaration
            var reference = symbol.DeclaringSyntaxReferences.FirstOrDefault(r => r.SyntaxTree == tree);
            if (reference == null)
            {
                return;
            }

            var declaringNode = reference.GetSyntax();
            if (declaringNode is EnumMemberDeclarationSyntax enumMember)
            {
                var enumDeclaration = enumMember.GetAncestor<EnumDeclarationSyntax>();

                if (enumDeclaration != null)
                {
                    var index = enumDeclaration.Members.IndexOf(enumMember);
                    if (index != -1 && index < enumDeclaration.Members.SeparatorCount)
                    {
                        // Cool, we have a comma, so do it
                        var start = enumMember.SpanStart;
                        var end = enumDeclaration.Members.GetSeparator(index).Span.End;

                        spans.Add(TextSpan.FromBounds(start, end));
                        return;
                    }
                }
            }

            spans.Add(declaringNode.Span);
        }

        protected internal override VirtualTreePoint? GetSymbolItemNavigationPoint(Document document, NavigationBarSymbolItem item, CancellationToken cancellationToken)
        {
            var compilation = document.Project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var symbols = item.NavigationSymbolId.Resolve(compilation, cancellationToken: cancellationToken);

            var symbol = symbols.Symbol;

            if (symbol == null)
            {
                if (item.NavigationSymbolIndex < symbols.CandidateSymbols.Length)
                {
                    symbol = symbols.CandidateSymbols[item.NavigationSymbolIndex.Value];
                }
                else
                {
                    return null;
                }
            }

            var syntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
            var location = symbol.Locations.FirstOrDefault(l => l.SourceTree.Equals(syntaxTree));

            if (location == null)
            {
                location = symbol.Locations.FirstOrDefault();
            }

            if (location == null)
            {
                return null;
            }

            return new VirtualTreePoint(location.SourceTree, location.SourceTree.GetText(cancellationToken), location.SourceSpan.Start);
        }

        [Conditional("DEBUG")]
        private static void ValidateSpanFromBounds(ITextSnapshot snapshot, int start, int end)
        {
            Debug.Assert(start >= 0 && end <= snapshot.Length && start <= end);
        }

        [Conditional("DEBUG")]
        private static void ValidateSpan(ITextSnapshot snapshot, int start, int length)
        {
            ValidateSpanFromBounds(snapshot, start, start + length);
        }

        public override void NavigateToItem(Document document, NavigationBarItem item, ITextView textView, CancellationToken cancellationToken)
        {
            NavigateToSymbolItem(document, (NavigationBarSymbolItem)item, cancellationToken);
        }
    }
}
