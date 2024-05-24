// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.DocumentationComments;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.MetadataAsSource;

using static SyntaxFactory;

internal partial class CSharpMetadataAsSourceService : AbstractMetadataAsSourceService
{
    private static readonly AbstractFormattingRule s_memberSeparationRule = new FormattingRule();
    public static readonly CSharpMetadataAsSourceService Instance = new();

    private CSharpMetadataAsSourceService()
    {
    }

    protected override async Task<Document> AddAssemblyInfoRegionAsync(Document document, Compilation symbolCompilation, ISymbol symbol, CancellationToken cancellationToken)
    {
        var assemblyInfo = MetadataAsSourceHelpers.GetAssemblyInfo(symbol.ContainingAssembly);
        var assemblyPath = MetadataAsSourceHelpers.GetAssemblyDisplay(symbolCompilation, symbol.ContainingAssembly);

        var regionTrivia = RegionDirectiveTrivia(true)
            .WithTrailingTrivia(new[] { Space, PreprocessingMessage(assemblyInfo) });

        var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = oldRoot.WithPrependedLeadingTrivia(
            Trivia(regionTrivia),
            CarriageReturnLineFeed,
            Comment("// " + assemblyPath),
            CarriageReturnLineFeed,
            Trivia(EndRegionDirectiveTrivia(true)),
            CarriageReturnLineFeed,
            CarriageReturnLineFeed);

        return document.WithSyntaxRoot(newRoot);
    }

    protected override ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document)
        => [s_memberSeparationRule, .. Formatter.GetDefaultFormattingRules(document)];

    protected override async Task<Document> ConvertDocCommentsToRegularCommentsAsync(Document document, IDocumentationCommentFormattingService docCommentFormattingService, CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var newSyntaxRoot = DocCommentConverter.ConvertToRegularComments(syntaxRoot, docCommentFormattingService, cancellationToken);

        return document.WithSyntaxRoot(newSyntaxRoot);
    }

    protected override ImmutableArray<AbstractReducer> GetReducers()
        => [
            new CSharpNameReducer(),
            new CSharpEscapingReducer(),
            new CSharpParenthesizedExpressionReducer(),
            new CSharpParenthesizedPatternReducer(),
            new CSharpDefaultExpressionReducer(),
        ];

    /// <summary>
    /// Adds <c>#nullable enable</c> and <c>#nullable disable</c> annotations to the file as necessary.  Note that
    /// this does not try to be 100% accurate, but rather it handles the most common cases out there.  Specifically,
    /// if a file contains any nullable annotated/not-annotated types, then we prefix the file with <c>#nullable
    /// enable</c>.  Then if we hit any members that explicitly have *oblivious* types, but no annotated or
    /// non-annotated types, then we switch to <c>#nullable disable</c> for those specific members.
    /// <para/>
    /// This is technically innacurate for possible, but very uncommon cases.  For example, if the user's code
    /// explicitly did something like this:
    /// 
    /// <code>
    /// public void Goo(string goo,
    ///                 #nullable disable
    ///                 string bar
    ///                 #nullable enable
    ///                 string baz);
    /// </code>
    /// 
    /// Then we would be unable to handle that.  However, this is highly unlikely to happen, and so we accept the
    /// inaccuracy for the purpose of simplicity and for handling the much more common cases of either the entire
    /// file being annotated, or the user individually disabling annotations at the member level.
    /// </summary>
    protected override async Task<Document> AddNullableRegionsAsync(Document document, CancellationToken cancellationToken)
    {
        var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var options = (CSharpParseOptions)tree.Options;

        // Only valid for C# 8 and above.
        if (options.LanguageVersion < LanguageVersion.CSharp8)
            return document;

        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        var (_, annotatedOrNotAnnotated) = GetNullableAnnotations(root);

        // If there are no annotated or not-annotated types, then no need to add `#nullable enable`.
        if (!annotatedOrNotAnnotated)
            return document;

        var newRoot = AddNullableRegions(root, cancellationToken);
        newRoot = newRoot.WithPrependedLeadingTrivia(CreateNullableTrivia(enable: true));

        return document.WithSyntaxRoot(newRoot);
    }

    private static (bool oblivious, bool annotatedOrNotAnnotated) GetNullableAnnotations(SyntaxNode node)
    {
        return (HasAnnotation(node, NullableSyntaxAnnotation.Oblivious),
                HasAnnotation(node, NullableSyntaxAnnotation.AnnotatedOrNotAnnotated));
    }

    private static bool HasAnnotation(SyntaxNode node, SyntaxAnnotation annotation)
    {
        // see if any child nodes have this annotation.  Ignore anything in attributes (like `[Obsolete]void Goo()`
        // as these are not impacted by `#nullable` regions.  Instead, we only care about signature types.
        var annotatedChildren = node.GetAnnotatedNodes(annotation);
        return annotatedChildren.Any(n => n.GetAncestorOrThis<AttributeSyntax>() == null);
    }

    private static SyntaxTrivia[] CreateNullableTrivia(bool enable)
    {
        var keyword = enable ? SyntaxKind.EnableKeyword : SyntaxKind.DisableKeyword;
        return
        [
            Trivia(NullableDirectiveTrivia(Token(keyword), isActive: enable)),
            ElasticCarriageReturnLineFeed,
            ElasticCarriageReturnLineFeed,
        ];
    }

    private TSyntax AddNullableRegions<TSyntax>(TSyntax node, CancellationToken cancellationToken)
        where TSyntax : SyntaxNode
    {
        return node switch
        {
            CompilationUnitSyntax compilationUnit => (TSyntax)(object)compilationUnit.WithMembers(AddNullableRegions(compilationUnit.Members, cancellationToken)),
            NamespaceDeclarationSyntax ns => (TSyntax)(object)ns.WithMembers(AddNullableRegions(ns.Members, cancellationToken)),
            TypeDeclarationSyntax type => (TSyntax)(object)AddNullableRegionsAroundTypeMembers(type, cancellationToken),
            _ => node,
        };
    }

    private SyntaxList<MemberDeclarationSyntax> AddNullableRegions(
        SyntaxList<MemberDeclarationSyntax> members,
        CancellationToken cancellationToken)
    {
        return [.. members.Select(m => AddNullableRegions(m, cancellationToken))];
    }

    private TypeDeclarationSyntax AddNullableRegionsAroundTypeMembers(
        TypeDeclarationSyntax type, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<MemberDeclarationSyntax>.GetInstance(out var builder);

        var currentlyEnabled = true;

        foreach (var member in type.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is BaseTypeDeclarationSyntax)
            {
                // if we hit a type, and we're currently disabled, then switch us back to enabled for that type.
                // This ensures whenever we walk into a type-decl, we're always in the enabled-state.
                builder.Add(TransitionTo(AddNullableRegions(member, cancellationToken), enabled: true, ref currentlyEnabled));
                continue;
            }

            // we hit a member.  see what sort of types it contained.
            var (oblivious, annotatedOrNotAnnotated) = GetNullableAnnotations(member);

            // if we have null annotations, transition us back to the enabled state
            if (annotatedOrNotAnnotated)
            {
                builder.Add(TransitionTo(member, enabled: true, ref currentlyEnabled));
            }
            else if (oblivious)
            {
                // if we didn't have null annotations, and we had an explicit oblivious type,
                // then definitely transition us to the disabled state
                builder.Add(TransitionTo(member, enabled: false, ref currentlyEnabled));
            }
            else
            {
                // had no types at all.  no need to change state.
                builder.Add(member);
            }
        }

        var result = type.WithMembers([.. builder]);
        if (!currentlyEnabled)
        {
            // switch us back to enabled as we leave the type.
            result = result.WithCloseBraceToken(
                result.CloseBraceToken.WithPrependedLeadingTrivia(CreateNullableTrivia(enable: true)));
        }

        return result;
    }

    private static MemberDeclarationSyntax TransitionTo(MemberDeclarationSyntax member, bool enabled, ref bool currentlyEnabled)
    {
        if (enabled == currentlyEnabled)
        {
            // already in the right state.  don't start a #nullable region
            return member;
        }
        else
        {
            // switch to the desired state and add the right trivia to the node.
            currentlyEnabled = enabled;
            return member.WithPrependedLeadingTrivia(CreateNullableTrivia(currentlyEnabled));
        }
    }
}
