// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

using static ImplementHelpers;

internal abstract partial class AbstractImplementInterfaceService() : IImplementInterfaceService
{
    protected const string DisposingName = "disposing";

    protected abstract ISyntaxFormatting SyntaxFormatting { get; }
    protected abstract SyntaxGeneratorInternal SyntaxGeneratorInternal { get; }

    protected abstract string ToDisplayString(IMethodSymbol disposeImplMethod, SymbolDisplayFormat format);

    protected abstract bool CanImplementImplicitly { get; }
    protected abstract bool HasHiddenExplicitImplementation { get; }
    protected abstract bool TryInitializeState(Document document, SemanticModel model, SyntaxNode interfaceNode, CancellationToken cancellationToken,
        [NotNullWhen(true)] out SyntaxNode? classOrStructDecl,
        [NotNullWhen(true)] out INamedTypeSymbol? classOrStructType,
        out ImmutableArray<INamedTypeSymbol> interfaceTypes);
    protected abstract bool AllowDelegateAndEnumConstraints(ParseOptions options);

    protected abstract SyntaxNode AddCommentInsideIfStatement(SyntaxNode ifDisposingStatement, SyntaxTriviaList trivia);
    protected abstract SyntaxNode CreateFinalizer(SyntaxGenerator generator, INamedTypeSymbol classType, string disposeMethodDisplayString);

    public async Task<Document> ImplementInterfaceAsync(
        Document document, ImplementTypeOptions options, SyntaxNode node, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Refactoring_ImplementInterface, cancellationToken))
        {
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var state = State.Generate(this, document, model, node, cancellationToken);
            if (state == null)
                return document;

            // While implementing just one default action, like in the case of pressing enter after interface name in VB,
            // choose to implement with the dispose pattern as that's the Dev12 behavior.
            var implementDisposePattern = ShouldImplementDisposePattern(model.Compilation, state.Info, explicitly: false);
            var generator = new ImplementInterfaceGenerator(
                this, document, state.Info, options, new() { OnlyRemaining = true, ImplementDisposePattern = implementDisposePattern });

            return await generator.ImplementInterfaceAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ImplementInterfaceInfo?> AnalyzeAsync(Document document, SyntaxNode interfaceType, CancellationToken cancellationToken)
    {
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return State.Generate(this, document, model, interfaceType, cancellationToken)?.Info;
    }

    protected TNode AddComment<TNode>(string comment, TNode node) where TNode : SyntaxNode
        => AddComments([comment], node);

    protected TNode AddComments<TNode>(string comment1, string comment2, TNode node) where TNode : SyntaxNode
        => AddComments([comment1, comment2], node);

    protected TNode AddComments<TNode>(string[] comments, TNode node) where TNode : SyntaxNode
        => node.WithPrependedLeadingTrivia(CreateCommentTrivia(comments));

    protected SyntaxTriviaList CreateCommentTrivia(
        params ReadOnlySpan<string> comments)
    {
        using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var trivia);

        foreach (var comment in comments)
        {
            trivia.Add(this.SyntaxGeneratorInternal.SingleLineComment(" " + comment));
            trivia.Add(this.SyntaxGeneratorInternal.ElasticCarriageReturnLineFeed);
        }

        return [.. trivia];
    }

    public async Task<Document> ImplementInterfaceAsync(
        Document document,
        ImplementInterfaceInfo info,
        ImplementTypeOptions options,
        ImplementInterfaceConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var generator = new ImplementInterfaceGenerator(
            this, document, info, options, configuration);
        return await generator.ImplementInterfaceAsync(cancellationToken).ConfigureAwait(false);
    }

    public ImmutableArray<ISymbol> ImplementInterfaceMember(
        Document document,
        ImplementInterfaceInfo info,
        ImplementTypeOptions options,
        ImplementInterfaceConfiguration configuration,
        Compilation compilation,
        ISymbol interfaceMember)
    {
        var generator = new ImplementInterfaceGenerator(
            this, document, info, options, configuration);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var supportsImplementingLessAccessibleMember = syntaxFacts.SupportsImplicitImplementationOfNonPublicInterfaceMembers(document.Project.ParseOptions!);
        var implementedMembers = generator.GenerateMembers(
            compilation,
            interfaceMember,
            conflictingMember: null,
            memberName: interfaceMember.Name,
            generateInvisibly: generator.ShouldGenerateInvisibleMember(document.Project.ParseOptions!, interfaceMember, interfaceMember.Name, supportsImplementingLessAccessibleMember),
            generateAbstractly: configuration.Abstractly,
            addNew: false,
            interfaceMember.RequiresUnsafeModifier() && !syntaxFacts.IsUnsafeContext(info.ContextNode),
            options.PropertyGenerationBehavior);

        return implementedMembers;
    }
}
