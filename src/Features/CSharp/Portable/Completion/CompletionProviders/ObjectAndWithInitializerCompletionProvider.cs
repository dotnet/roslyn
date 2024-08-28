// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

// Provides symbol completions in object and 'with' initializers:
// - new() { $$
// - new C() { $$
// - expr with { $$
[ExportCompletionProvider(nameof(ObjectAndWithInitializerCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(ObjectCreationCompletionProvider))]
[Shared]
internal class ObjectAndWithInitializerCompletionProvider : AbstractObjectInitializerCompletionProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ObjectAndWithInitializerCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    protected override async Task<bool> IsExclusiveAsync(Document document, int position, CancellationToken cancellationToken)
    {
        // We're exclusive if this context could only be an object initializer and not also a
        // collection initializer. If we're initializing something that could be initialized as
        // an object or as a collection, say we're not exclusive. That way the rest of
        // intellisense can be used in the collection initializer.
        // 
        // Consider this case:

        // class c : IEnumerable<int> 
        // { 
        // public void Add(int addend) { }
        // public int goo; 
        // }

        // void goo()
        // {
        //    var b = new c {|
        // }

        // There we could initialize b using either an object initializer or a collection
        // initializer. Since we don't know which the user will use, we'll be non-exclusive, so
        // the other providers can help the user write the collection initializer, if they want
        // to.
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

        if (tree.IsInNonUserCode(position, cancellationToken))
        {
            return false;
        }

        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Parent == null)
        {
            return false;
        }

        if (token.Parent.Parent is not ExpressionSyntax expression)
        {
            return false;
        }

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(expression, cancellationToken).ConfigureAwait(false);
        var initializedType = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        if (initializedType == null)
        {
            return false;
        }

        var enclosingSymbol = semanticModel.GetEnclosingNamedTypeOrAssembly(position, cancellationToken);
        // Non-exclusive if initializedType can be initialized as a collection.
        if (initializedType.CanSupportCollectionInitializer(enclosingSymbol))
        {
            return false;
        }

        // By default, only our member names will show up.
        return true;
    }

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options) || text[characterPosition] == ' ';

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters.Add(' ');

    protected override Tuple<ITypeSymbol, Location>? GetInitializedType(
        Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
    {
        var tree = semanticModel.SyntaxTree;
        if (tree.IsInNonUserCode(position, cancellationToken))
        {
            return null;
        }

        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
        token = token.GetPreviousTokenIfTouchingWord(position);

        if (token.Kind() is not SyntaxKind.CommaToken and not SyntaxKind.OpenBraceToken)
        {
            return null;
        }

        if (token.Parent == null || token.Parent.Parent == null)
        {
            return null;
        }

        // If we got a comma, we can syntactically find out if we're in an ObjectInitializerExpression or WithExpression
        if (token.Kind() == SyntaxKind.CommaToken &&
            token.Parent.Kind() is not (SyntaxKind.ObjectInitializerExpression or SyntaxKind.WithInitializerExpression))
        {
            return null;
        }

        var type = GetInitializedType(token, document, semanticModel, cancellationToken);
        if (type is null)
        {
            return null;
        }

        return Tuple.Create(type, token.GetLocation());
    }

    private static ITypeSymbol? GetInitializedType(SyntaxToken token, Document document, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var parent = token.Parent?.Parent;

        // new() { $$
        // new Goo { $$
        if (parent is (kind: SyntaxKind.ObjectCreationExpression or SyntaxKind.ImplicitObjectCreationExpression))
        {
            return semanticModel.GetTypeInfo(parent, cancellationToken).Type;
        }

        // Nested: new Goo { bar = { $$
        if (parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            // Use the type inferrer to get the type being initialized.
            var typeInferenceService = document.GetRequiredLanguageService<ITypeInferenceService>();
            var parentInitializer = token.GetAncestor<InitializerExpressionSyntax>()!;
            return typeInferenceService.InferType(semanticModel, parentInitializer, objectAsDefault: false, cancellationToken: cancellationToken);
        }

        // expr with { $$
        if (parent is WithExpressionSyntax withExpression)
        {
            return semanticModel.GetTypeInfo(withExpression.Expression, cancellationToken).Type;
        }

        return null;
    }

    protected override HashSet<string> GetInitializedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken)
    {
        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
                        .GetPreviousTokenIfTouchingWord(position);

        // We should have gotten back a { or ,
        if (token.Kind() is SyntaxKind.CommaToken or SyntaxKind.OpenBraceToken)
        {
            if (token.Parent != null)
            {

                if (token.Parent is InitializerExpressionSyntax initializer)
                {
                    return new HashSet<string>(initializer.Expressions.OfType<AssignmentExpressionSyntax>()
                        .Where(b => b.OperatorToken.Kind() == SyntaxKind.EqualsToken)
                        .Select(b => b.Left)
                        .OfType<IdentifierNameSyntax>()
                        .Select(i => i.Identifier.ValueText));
                }
            }
        }

        return [];
    }

    protected override bool IsInitializable(ISymbol member, INamedTypeSymbol containingType)
    {
        if (member is IPropertySymbol property && property.Parameters.Any(static p => !p.IsOptional))
        {
            return false;
        }

        return base.IsInitializable(member, containingType);
    }

    protected override string EscapeIdentifier(ISymbol symbol)
        => symbol.Name.EscapeIdentifier();
}
