// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.NavigationBar.RoslynNavigationBarItem;

namespace Microsoft.CodeAnalysis.CSharp.NavigationBar;

[ExportLanguageService(typeof(INavigationBarItemService), LanguageNames.CSharp), Shared]
internal class CSharpNavigationBarItemService : AbstractNavigationBarItemService
{
    private static readonly SymbolDisplayFormat s_typeFormat =
        SymbolDisplayFormat.CSharpErrorMessageFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeVariance);

    private static readonly SymbolDisplayFormat s_memberFormat =
        new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
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
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpNavigationBarItemService()
    {
    }

    protected override async Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsInCurrentProcessAsync(
        Document document, bool supportsCodeGeneration, CancellationToken cancellationToken)
    {
        var typesInFile = await GetTypesInFileAsync(document, cancellationToken).ConfigureAwait(false);
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        return GetMembersInTypes(document.Project.Solution, tree, typesInFile, cancellationToken);
    }

    private static ImmutableArray<RoslynNavigationBarItem> GetMembersInTypes(
        Solution solution, SyntaxTree tree, IEnumerable<INamedTypeSymbol> types, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.NavigationBar_ItemService_GetMembersInTypes_CSharp, cancellationToken))
        {
            using var _1 = ArrayBuilder<RoslynNavigationBarItem>.GetInstance(out var items);

            foreach (var type in types)
            {
                using var _2 = ArrayBuilder<RoslynNavigationBarItem>.GetInstance(out var memberItems);

                foreach (var member in type.GetMembers())
                {
                    if (member.IsImplicitlyDeclared ||
                        member.Kind == SymbolKind.NamedType ||
                        IsAccessor(member))
                    {
                        continue;
                    }

                    if (member is IMethodSymbol { PartialImplementationPart: { } } methodSymbol)
                    {
                        memberItems.AddIfNotNull(CreateItemForMember(solution, methodSymbol, tree, cancellationToken));
                        memberItems.AddIfNotNull(CreateItemForMember(solution, methodSymbol.PartialImplementationPart, tree, cancellationToken));
                    }
                    else if (member is IPropertySymbol { PartialImplementationPart: { } } propertySymbol)
                    {
                        memberItems.AddIfNotNull(CreateItemForMember(solution, propertySymbol, tree, cancellationToken));
                        memberItems.AddIfNotNull(CreateItemForMember(solution, propertySymbol.PartialImplementationPart, tree, cancellationToken));
                    }
                    else if (member is IMethodSymbol or IPropertySymbol)
                    {
                        Debug.Assert(member is IMethodSymbol { PartialDefinitionPart: null } or IPropertySymbol { PartialDefinitionPart: null },
                            $"NavBar expected GetMembers to return partial method/property definition parts but the implementation part was returned.");

                        memberItems.AddIfNotNull(CreateItemForMember(solution, member, tree, cancellationToken));
                    }
                    else
                    {
                        memberItems.AddIfNotNull(CreateItemForMember(solution, member, tree, cancellationToken));
                    }
                }

                memberItems.Sort((x, y) =>
                {
                    var textComparison = x.Text.CompareTo(y.Text);
                    return textComparison != 0 ? textComparison : x.Grayed.CompareTo(y.Grayed);
                });

                var spans = GetSymbolLocation(solution, type, tree, cancellationToken);
                if (spans == null)
                    continue;

                items.Add(new SymbolItem(
                    type.Name,
                    text: type.ToDisplayString(s_typeFormat),
                    glyph: type.GetGlyph(),
                    isObsolete: type.IsObsolete(),
                    spans.Value,
                    childItems: memberItems.ToImmutable()));
            }

            items.Sort((x1, x2) => x1.Text.CompareTo(x2.Text));
            return items.ToImmutableAndClear();
        }
    }

    private static async Task<IEnumerable<INamedTypeSymbol>> GetTypesInFileAsync(Document document, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        return GetTypesInFile(semanticModel, cancellationToken);
    }

    private static IEnumerable<INamedTypeSymbol> GetTypesInFile(SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.NavigationBar_ItemService_GetTypesInFile_CSharp, cancellationToken))
        {
            var types = new HashSet<INamedTypeSymbol>();
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToVisit);

            nodesToVisit.Push(semanticModel.SyntaxTree.GetRoot(cancellationToken));

            while (nodesToVisit.TryPop(out var node))
            {
                if (cancellationToken.IsCancellationRequested)
                    return [];

                var type = GetType(semanticModel, node, cancellationToken);

                if (type != null)
                {
                    types.Add((INamedTypeSymbol)type);
                }

                if (node is BaseMethodDeclarationSyntax or
                    BasePropertyDeclarationSyntax or
                    BaseFieldDeclarationSyntax or
                    StatementSyntax or
                    ExpressionSyntax)
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

    private static ISymbol? GetType(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
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

            return method.MethodKind is MethodKind.PropertyGet or MethodKind.PropertySet;
        }

        return false;
    }

    private static RoslynNavigationBarItem? CreateItemForMember(
        Solution solution, ISymbol member, SyntaxTree tree, CancellationToken cancellationToken)
    {
        var location = GetSymbolLocation(solution, member, tree, cancellationToken);
        if (location == null)
            return null;

        return new SymbolItem(
            member.Name,
            member.ToDisplayString(s_memberFormat),
            member.GetGlyph(),
            member.IsObsolete(),
            location.Value);
    }

    private static SymbolItemLocation? GetSymbolLocation(
        Solution solution, ISymbol symbol, SyntaxTree tree, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (symbol.Kind == SymbolKind.Field)
        {
            return symbol.ContainingType.TypeKind == TypeKind.Enum
                ? GetSymbolLocation(solution, symbol, tree, static reference => GetEnumMemberSpan(reference))
                : GetSymbolLocation(solution, symbol, tree, static reference => GetFieldReferenceSpan(reference));
        }
        else
        {
            return GetSymbolLocation(solution, symbol, tree, static reference => reference.Span);
        }
    }

    private static TextSpan GetFieldReferenceSpan(SyntaxReference reference)
    {
        var declaringNode = reference.GetSyntax();

        var spanStart = declaringNode.SpanStart;
        var spanEnd = declaringNode.Span.End;

        var fieldDeclaration = declaringNode.GetAncestor<FieldDeclarationSyntax>();
        if (fieldDeclaration != null)
        {
            var variables = fieldDeclaration.Declaration.Variables;

            if (variables.FirstOrDefault() == declaringNode)
                spanStart = fieldDeclaration.SpanStart;

            if (variables.LastOrDefault() == declaringNode)
                spanEnd = fieldDeclaration.Span.End;
        }

        return TextSpan.FromBounds(spanStart, spanEnd);
    }

    private static TextSpan GetEnumMemberSpan(SyntaxReference reference)
    {
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

                    return TextSpan.FromBounds(start, end);
                }
            }
        }

        return declaringNode.Span;
    }
}
