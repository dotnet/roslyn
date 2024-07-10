// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

using static ImplementHelpers;

internal abstract partial class AbstractImplementInterfaceService() : IImplementInterfaceService
{
    protected const string DisposingName = "disposing";

    protected abstract string ToDisplayString(IMethodSymbol disposeImplMethod, SymbolDisplayFormat format);

    protected abstract bool CanImplementImplicitly { get; }
    protected abstract bool HasHiddenExplicitImplementation { get; }
    protected abstract bool TryInitializeState(Document document, SemanticModel model, SyntaxNode interfaceNode, CancellationToken cancellationToken, out SyntaxNode classOrStructDecl, out INamedTypeSymbol classOrStructType, out IEnumerable<INamedTypeSymbol> interfaceTypes);
    protected abstract bool AllowDelegateAndEnumConstraints(ParseOptions options);

    protected abstract SyntaxNode AddCommentInsideIfStatement(SyntaxNode ifDisposingStatement, SyntaxTriviaList trivia);
    protected abstract SyntaxNode CreateFinalizer(SyntaxGenerator generator, INamedTypeSymbol classType, string disposeMethodDisplayString);

    public async Task<Document> ImplementInterfaceAsync(
        Document document, ImplementTypeGenerationOptions options, SyntaxNode node, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.Refactoring_ImplementInterface, cancellationToken))
        {
            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var state = State.Generate(this, document, model, node, cancellationToken);
            if (state == null)
            {
                return document;
            }

            // TODO: https://github.com/dotnet/roslyn/issues/60990
            // While implementing just one default action, like in the case of pressing enter after interface name in VB,
            // choose to implement with the dispose pattern as that's the Dev12 behavior.
            var generator = ShouldImplementDisposePattern(state, explicitly: false)
                ? ImplementInterfaceWithDisposePatternGenerator.CreateImplementWithDisposePattern(this, document, options, state)
                : ImplementInterfaceGenerator.CreateImplement(this, document, options, state);

            return await generator.GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IImplementInterfaceInfo?> AnalyzeAsync(Document document, SyntaxNode interfaceType, CancellationToken cancellationToken)
    {
        var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var state = State.Generate(this, document, model, interfaceType, cancellationToken);
        return state;
    }

    public ImmutableArray<IImplementInterfaceGenerator> GetGenerators(Document document, ImplementTypeGenerationOptions options, SemanticModel model, SyntaxNode node, CancellationToken cancellationToken)
    {
        var state = State.Generate(this, document, model, node, cancellationToken);
        return GetGenerators(document, options, state, cancellationToken).ToImmutableArray();
    }

    private IEnumerable<IImplementInterfaceGenerator> GetGenerators(
        Document document, ImplementTypeGenerationOptions options, State? state, CancellationToken cancellationToken)
    {
        if (state == null)
        {
            yield break;
        }

        if (state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented.Length > 0)
        {
            var totalMemberCount = 0;
            var inaccessibleMemberCount = 0;

            foreach (var (_, members) in state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented)
            {
                foreach (var member in members)
                {
                    totalMemberCount++;

                    if (IsLessAccessibleThan(member, state.ClassOrStructType))
                    {
                        inaccessibleMemberCount++;
                    }
                }
            }

            // If all members to implement are inaccessible, then "Implement interface" codeaction
            // will be the same as "Implement interface explicitly", so there is no point in having both of them
            if (totalMemberCount != inaccessibleMemberCount)
            {
                yield return ImplementInterfaceGenerator.CreateImplement(this, document, options, state);
            }

            if (ShouldImplementDisposePattern(state, explicitly: false))
            {
                yield return ImplementInterfaceWithDisposePatternGenerator.CreateImplementWithDisposePattern(this, document, options, state);
            }

            var delegatableMembers = GetDelegatableMembers(state, cancellationToken);
            foreach (var member in delegatableMembers)
            {
                yield return ImplementInterfaceGenerator.CreateImplementThroughMember(this, document, options, state, member);
            }

            if (state.ClassOrStructType.IsAbstract)
            {
                yield return ImplementInterfaceGenerator.CreateImplementAbstractly(this, document, options, state);
            }
        }

        if (state.MembersWithoutExplicitImplementation.Length > 0)
        {
            yield return ImplementInterfaceGenerator.CreateImplementExplicitly(this, document, options, state);

            if (ShouldImplementDisposePattern(state, explicitly: true))
            {
                yield return ImplementInterfaceWithDisposePatternGenerator.CreateImplementExplicitlyWithDisposePattern(this, document, options, state);
            }
        }

        if (AnyImplementedImplicitly(state))
        {
            yield return ImplementInterfaceGenerator.CreateImplementRemainingExplicitly(this, document, options, state);
        }
    }

    private static bool AnyImplementedImplicitly(State state)
    {
        if (state.MembersWithoutExplicitOrImplicitImplementation.Length != state.MembersWithoutExplicitImplementation.Length)
        {
            return true;
        }

        for (var i = 0; i < state.MembersWithoutExplicitOrImplicitImplementation.Length; i++)
        {
            var (typeA, membersA) = state.MembersWithoutExplicitOrImplicitImplementation[i];
            var (typeB, membersB) = state.MembersWithoutExplicitImplementation[i];
            if (!typeA.Equals(typeB))
            {
                return true;
            }

            if (!membersA.SequenceEqual(membersB))
            {
                return true;
            }
        }

        return false;
    }

    private static ImmutableArray<ISymbol> GetDelegatableMembers(State state, CancellationToken cancellationToken)
    {
        var firstInterfaceType = state.InterfaceTypes.First();

        return ImplementHelpers.GetDelegatableMembers(
            state.Document,
            state.ClassOrStructType,
            t => t.GetAllInterfacesIncludingThis().Contains(firstInterfaceType),
            cancellationToken);
    }

    protected static TNode AddComment<TNode>(SyntaxGenerator g, string comment, TNode node) where TNode : SyntaxNode
        => AddComments(g, [comment], node);

    protected static TNode AddComments<TNode>(SyntaxGenerator g, string comment1, string comment2, TNode node) where TNode : SyntaxNode
        => AddComments(g, [comment1, comment2,], node);

    protected static TNode AddComments<TNode>(SyntaxGenerator g, string[] comments, TNode node) where TNode : SyntaxNode
        => node.WithPrependedLeadingTrivia(CreateCommentTrivia(g, comments));

    protected static SyntaxTriviaList CreateCommentTrivia(SyntaxGenerator generator, params string[] comments)
    {
        using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var trivia);

        foreach (var comment in comments)
        {
            trivia.Add(generator.SingleLineComment(" " + comment));
            trivia.Add(generator.ElasticCarriageReturnLineFeed);
        }

        return new SyntaxTriviaList(trivia);
    }
}
