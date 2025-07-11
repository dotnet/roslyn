// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpNavigationBarItemService() : AbstractNavigationBarItemService
{
    private static readonly SymbolDisplayFormat s_typeFormat =
        SymbolDisplayFormat.CSharpErrorMessageFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeVariance);

    private static readonly SymbolDisplayFormat s_memberNameFormat =
        new(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    private static readonly SymbolDisplayFormat s_memberDetailsFormat =
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

    protected override async Task<ImmutableArray<RoslynNavigationBarItem>> GetItemsInCurrentProcessAsync(
        Document document, bool supportsCodeGeneration, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        using var _ = PooledHashSet<INamedTypeSymbol>.GetInstance(out var typesInFile);

        AddTypesInFile(semanticModel, typesInFile, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
            return [];

        return GetMembersInTypes(document.Project.Solution, semanticModel, typesInFile, cancellationToken);
    }

    private static ImmutableArray<RoslynNavigationBarItem> GetMembersInTypes(
        Solution solution, SemanticModel semanticModel, HashSet<INamedTypeSymbol> types, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.NavigationBar_ItemService_GetMembersInTypes_CSharp, cancellationToken))
        {
            var tree = semanticModel.SyntaxTree;
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
                        memberItems.AddIfNotNull(CreateItemForMember(solution, methodSymbol, semanticModel, cancellationToken));
                        memberItems.AddIfNotNull(CreateItemForMember(solution, methodSymbol.PartialImplementationPart, semanticModel, cancellationToken));
                    }
                    else if (member is IPropertySymbol { PartialImplementationPart: { } } propertySymbol)
                    {
                        memberItems.AddIfNotNull(CreateItemForMember(solution, propertySymbol, semanticModel, cancellationToken));
                        memberItems.AddIfNotNull(CreateItemForMember(solution, propertySymbol.PartialImplementationPart, semanticModel, cancellationToken));
                    }
                    else if (member is IEventSymbol { PartialImplementationPart: { } } eventSymbol)
                    {
                        memberItems.AddIfNotNull(CreateItemForMember(solution, eventSymbol, semanticModel, cancellationToken));
                        memberItems.AddIfNotNull(CreateItemForMember(solution, eventSymbol.PartialImplementationPart, semanticModel, cancellationToken));
                    }
                    else if (member is IMethodSymbol or IPropertySymbol or IEventSymbol)
                    {
                        Debug.Assert(member is IMethodSymbol { PartialDefinitionPart: null } or IPropertySymbol { PartialDefinitionPart: null } or IEventSymbol { PartialDefinitionPart: null },
                            $"NavBar expected GetMembers to return partial method/property/event definition parts but the implementation part was returned.");

                        memberItems.AddIfNotNull(CreateItemForMember(solution, member, semanticModel, cancellationToken));
                    }
                    else
                    {
                        memberItems.AddIfNotNull(CreateItemForMember(solution, member, semanticModel, cancellationToken));
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

    private static void AddTypesInFile(
        SemanticModel semanticModel, HashSet<INamedTypeSymbol> types, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.NavigationBar_ItemService_GetTypesInFile_CSharp, cancellationToken))
        {
            using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var nodesToVisit);

            nodesToVisit.Push(semanticModel.SyntaxTree.GetRoot(cancellationToken));

            while (nodesToVisit.TryPop(out var node))
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                types.AddIfNotNull(GetType(semanticModel, node, cancellationToken));

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
                    nodesToVisit.Push(child);
            }
        }
    }

    private static INamedTypeSymbol? GetType(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        => node switch
        {
            BaseTypeDeclarationSyntax t => semanticModel.GetDeclaredSymbol(t, cancellationToken),
            DelegateDeclarationSyntax d => semanticModel.GetDeclaredSymbol(d, cancellationToken),
            CompilationUnitSyntax c => c.IsTopLevelProgram() ? semanticModel.GetDeclaredSymbol(c, cancellationToken)?.ContainingType : null,
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

    private static SymbolItem? CreateItemForMember(
        Solution solution, ISymbol member, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var location = GetSymbolLocation(solution, member, semanticModel.SyntaxTree, cancellationToken);
        if (location == null)
            return null;

        using var _ = ArrayBuilder<RoslynNavigationBarItem>.GetInstance(out var localFunctionItems);
        foreach (var syntaxReference in member.DeclaringSyntaxReferences)
        {
            if (syntaxReference.SyntaxTree != semanticModel.SyntaxTree)
            {
                // The reference is not in this file, no need to include in the outline view.
                continue;
            }

            var referenceNode = syntaxReference.GetSyntax(cancellationToken);
            localFunctionItems.AddRange(CreateLocalFunctionMembers(solution, referenceNode, semanticModel, cancellationToken));
        }

        return new SymbolItem(
            member.ToDisplayString(s_memberNameFormat),
            member.ToDisplayString(s_memberDetailsFormat),
            member.GetGlyph(),
            member.IsObsolete(),
            location.Value,
            localFunctionItems.ToImmutable());

        static ImmutableArray<RoslynNavigationBarItem> CreateLocalFunctionMembers(
            Solution solution, SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // Get only the local functions that are direct descendents of this method.
            var localFunctions = node.DescendantNodes(descendIntoChildren: (n) =>
                // Always descend from the original node even if its a local function, but do not descend further into descendent local functions.
                n == node || n is not LocalFunctionStatementSyntax).Where(n => n is LocalFunctionStatementSyntax);
            using var _ = ArrayBuilder<RoslynNavigationBarItem>.GetInstance(out var items);
            foreach (var localFunction in localFunctions)
            {
                var localFunctionSymbol = semanticModel.GetDeclaredSymbol(localFunction, cancellationToken);
                if (localFunctionSymbol == null)
                    continue;

                var location = GetSymbolLocation(solution, localFunctionSymbol, semanticModel.SyntaxTree, cancellationToken);
                if (location == null)
                    continue;

                // Check the child local functions to see if they have nested local functions.
                var childItems = CreateLocalFunctionMembers(solution, localFunction, semanticModel, cancellationToken);

                var symbolItem = new SymbolItem(
                    localFunctionSymbol.ToDisplayString(s_memberNameFormat),
                    localFunctionSymbol.ToDisplayString(s_memberDetailsFormat),
                    localFunctionSymbol.GetGlyph(),
                    localFunctionSymbol.IsObsolete(),
                    location.Value,
                    childItems);

                items.Add(symbolItem);
            }

            return items.ToImmutable();
        }
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
