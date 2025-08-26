// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ImplementInterface;

using static ImplementHelpers;

internal abstract partial class AbstractImplementInterfaceService<TTypeDeclarationSyntax> : IImplementInterfaceService
    where TTypeDeclarationSyntax : SyntaxNode
{
    protected const string DisposingName = "disposing";

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

    protected abstract bool IsTypeInInterfaceBaseList([NotNullWhen(true)] SyntaxNode? type);
    protected abstract void AddInterfaceTypes(TTypeDeclarationSyntax typeDeclaration, ArrayBuilder<SyntaxNode> result);

    public ImmutableArray<SyntaxNode> GetInterfaceTypes(SyntaxNode typeDeclaration)
    {
        using var _ = ArrayBuilder<SyntaxNode>.GetInstance(out var result);
        if (typeDeclaration is TTypeDeclarationSyntax typeSyntax)
            AddInterfaceTypes(typeSyntax, result);

        return result.ToImmutableAndClear();
    }

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

    private async Task<ImplementInterfaceInfo?> AnalyzeAsync(Document document, SyntaxNode interfaceType, CancellationToken cancellationToken)
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

    private async Task<Document> ImplementInterfaceAsync(
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

    public async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode? interfaceType, CancellationToken cancellationToken)
    {
        var options = await document.GetImplementTypeOptionsAsync(cancellationToken).ConfigureAwait(false);

        if (!this.IsTypeInInterfaceBaseList(interfaceType))
            return [];

        var info = await this.AnalyzeAsync(
            document, interfaceType, cancellationToken).ConfigureAwait(false);
        if (info is null)
            return [];

        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var codeActions);
        await foreach (var implementOptions in GetImplementOptionsAsync(document, info, cancellationToken).ConfigureAwait(false))
        {
            var title = GetTitle(implementOptions);
            var equivalenceKey = GetEquivalenceKey(info, implementOptions);
            codeActions.Add(CodeAction.Create(
                title,
                cancellationToken => this.ImplementInterfaceAsync(
                    document, info, options, implementOptions, cancellationToken),
                equivalenceKey));
        }

        return codeActions.ToImmutableAndClear();
    }

    private static async IAsyncEnumerable<ImplementInterfaceConfiguration> GetImplementOptionsAsync(
        Document document, ImplementInterfaceInfo state, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var supportsImplicitImplementationOfNonPublicInterfaceMembers = syntaxFacts.SupportsImplicitImplementationOfNonPublicInterfaceMembers(document.Project.ParseOptions!);
        if (state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented.Length > 0)
        {
            var totalMemberCount = 0;
            var inaccessibleMemberCount = 0;

            foreach (var (_, members) in state.MembersWithoutExplicitOrImplicitImplementationWhichCanBeImplicitlyImplemented)
            {
                foreach (var member in members)
                {
                    totalMemberCount++;

                    if (ContainsTypeLessAccessibleThan(member, state.ClassOrStructType, supportsImplicitImplementationOfNonPublicInterfaceMembers))
                        inaccessibleMemberCount++;
                }
            }

            // If all members to implement are inaccessible, then "Implement interface" codeaction
            // will be the same as "Implement interface explicitly", so there is no point in having both of them
            if (totalMemberCount != inaccessibleMemberCount)
                yield return new() { OnlyRemaining = true };

            if (ShouldImplementDisposePattern(compilation, state, explicitly: false))
                yield return new() { OnlyRemaining = true, ImplementDisposePattern = true, };

            var delegatableMembers = GetDelegatableMembers(document, state, cancellationToken);
            foreach (var member in delegatableMembers)
                yield return new() { ThroughMember = member };

            if (state.ClassOrStructType.IsAbstract)
                yield return new() { OnlyRemaining = true, Abstractly = true };
        }

        if (state.MembersWithoutExplicitImplementation.Length > 0)
        {
            yield return new() { Explicitly = true };

            if (ShouldImplementDisposePattern(compilation, state, explicitly: true))
                yield return new() { ImplementDisposePattern = true, Explicitly = true };
        }

        if (AnyImplementedImplicitly(state))
            yield return new() { OnlyRemaining = true, Explicitly = true };
    }

    private static string GetTitle(ImplementInterfaceConfiguration options)
    {
        if (options.ImplementDisposePattern)
        {
            return options.Explicitly
                ? CodeFixesResources.Implement_interface_explicitly_with_Dispose_pattern
                : CodeFixesResources.Implement_interface_with_Dispose_pattern;
        }
        else if (options.Explicitly)
        {
            return options.OnlyRemaining
                ? CodeFixesResources.Implement_remaining_members_explicitly
                : CodeFixesResources.Implement_all_members_explicitly;
        }
        else if (options.Abstractly)
        {
            return CodeFixesResources.Implement_interface_abstractly;
        }
        else if (options.ThroughMember != null)
        {
            return string.Format(CodeFixesResources.Implement_interface_through_0, options.ThroughMember.Name);
        }
        else
        {
            return CodeFixesResources.Implement_interface;
        }
    }

    private static string GetEquivalenceKey(
        ImplementInterfaceInfo state,
        ImplementInterfaceConfiguration options)
    {
        var interfaceType = state.InterfaceTypes.First();
        var typeName = interfaceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Legacy part of the equivalence key.  Kept the same to avoid test churn.
        var codeActionTypeName = options.ImplementDisposePattern
            ? "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceWithDisposePatternCodeAction"
            : "Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService+ImplementInterfaceCodeAction";

        // Consider code actions equivalent if they correspond to the same interface being implemented elsewhere
        // in the same manner.  Note: 'implement through member' means implementing the same interface through
        // an applicable member with the same name in the destination.
        return options.Explicitly.ToString() + ";" +
           options.Abstractly.ToString() + ";" +
           options.OnlyRemaining.ToString() + ":" +
           typeName + ";" +
           codeActionTypeName + ";" +
           options.ThroughMember?.Name;
    }

    private static bool AnyImplementedImplicitly(ImplementInterfaceInfo state)
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

    private static ImmutableArray<ISymbol> GetDelegatableMembers(
        Document document, ImplementInterfaceInfo state, CancellationToken cancellationToken)
    {
        var firstInterfaceType = state.InterfaceTypes.First();

        return ImplementHelpers.GetDelegatableMembers(
            document,
            state.ClassOrStructType,
            t => t.GetAllInterfacesIncludingThis().Contains(firstInterfaceType),
            cancellationToken);
    }
}
