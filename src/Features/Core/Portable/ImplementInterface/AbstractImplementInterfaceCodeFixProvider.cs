// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using static Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService;

namespace Microsoft.CodeAnalysis.ImplementInterface;

using static ImplementHelpers;

internal abstract class AbstractImplementInterfaceCodeFixProvider<TTypeSyntax> : CodeFixProvider
    where TTypeSyntax : SyntaxNode
{
    protected abstract bool IsTypeInInterfaceBaseList(TTypeSyntax type);

    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var span = context.Span;
        var cancellationToken = context.CancellationToken;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(span.Start);
        if (!token.Span.IntersectsWith(span))
            return;

        foreach (var type in token.Parent.GetAncestorsOrThis<TTypeSyntax>())
        {
            if (this.IsTypeInInterfaceBaseList(type))
            {
                var service = document.GetRequiredLanguageService<IImplementInterfaceService>();
                var options = context.Options.GetImplementTypeGenerationOptions(document.Project.Services);

                var info = await service.AnalyzeAsync(
                    document, type, cancellationToken).ConfigureAwait(false);
                using var _ = ArrayBuilder<IImplementInterfaceGenerator>.GetInstance(out var generators);
                await foreach (var generator in GetGeneratorsAsync(document, options, info, cancellationToken))
                    generators.AddIfNotNull(generator);

                if (generators.Count > 0)
                {
                    context.RegisterFixes(generators.SelectAsArray(
                        g => CodeAction.Create(
                            g.Title,
                            cancellationToken => g.ImplementInterfaceAsync(cancellationToken),
                            g.EquivalenceKey)), context.Diagnostics);
                }

                break;
            }
        }
    }

    private async IAsyncEnumerable<IImplementInterfaceGenerator> GetGeneratorsAsync(
        Document document, ImplementTypeGenerationOptions options, IImplementInterfaceInfo? state,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (state == null)
        {
            yield break;
        }

        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

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

            if (ShouldImplementDisposePattern(compilation, state, explicitly: false))
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

    private static bool AnyImplementedImplicitly(IImplementInterfaceInfo state)
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
}
