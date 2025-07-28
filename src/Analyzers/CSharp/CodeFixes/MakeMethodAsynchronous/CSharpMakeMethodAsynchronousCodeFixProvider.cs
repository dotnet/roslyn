// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MakeMethodAsynchronous;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddAsync), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpMakeMethodAsynchronousCodeFixProvider() : AbstractMakeMethodAsynchronousCodeFixProvider
{
    private const string CS4032 = nameof(CS4032); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
    private const string CS4033 = nameof(CS4033); // The 'await' operator can only be used within an async method. Consider marking this method with the 'async' modifier and changing its return type to 'Task'.
    private const string CS4034 = nameof(CS4034); // The 'await' operator can only be used within an async lambda expression. Consider marking this method with the 'async' modifier.
    private const string CS0246 = nameof(CS0246); // The type or namespace name 'await' could not be found

    private static readonly SyntaxToken s_asyncKeywordWithSpace = AsyncKeyword.WithoutTrivia().WithTrailingTrivia(Space);

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [CS4032, CS4033, CS4034, CS0246];

    protected override bool IsSupportedDiagnostic(Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        if (diagnostic.Id == CS0246)
        {
            // "The type or namespace name '{0}' could not be found"
            // Needs to be reported on an identifier caller 'await'.
            if (diagnostic.Location.SourceTree is null)
                return false;

            var root = diagnostic.Location.SourceTree.GetRoot(cancellationToken);
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            return token.Kind() == SyntaxKind.IdentifierToken && token.Text == "await";
        }

        // All the other diagnostics IDs are fine to use without additional checks.
        return true;
    }

    protected override string GetMakeAsyncTaskFunctionResource()
        => CSharpCodeFixesResources.Make_method_async;

    protected override string GetMakeAsyncVoidFunctionResource()
        => CSharpCodeFixesResources.Make_method_async_remain_void;

    protected override bool IsAsyncSupportingFunctionSyntax(SyntaxNode node)
        => node.IsAsyncSupportingFunctionSyntax();

    protected override bool IsAsyncReturnType(ITypeSymbol type, KnownTaskTypes knownTypes)
        => IsIAsyncEnumerableOrEnumerator(type, knownTypes) ||
           knownTypes.IsTaskLike(type);

    protected override SyntaxNode FixMethodSignature(
        bool addAsyncModifier,
        bool keepVoid,
        IMethodSymbol methodSymbol,
        SyntaxNode node,
        KnownTaskTypes knownTypes)
    {
        // We currently fix signature without adding 'async' modifier
        // only for a partial definitions part of partial methods
        Debug.Assert(addAsyncModifier || node is MethodDeclarationSyntax);

        return node switch
        {
            MethodDeclarationSyntax method => FixMethod(addAsyncModifier, keepVoid, methodSymbol, method, knownTypes),
            LocalFunctionStatementSyntax localFunction => FixLocalFunction(keepVoid, methodSymbol, localFunction, knownTypes),
            AnonymousFunctionExpressionSyntax anonymous => FixAnonymousFunction(anonymous),
            _ => node,
        };
    }

    private static MethodDeclarationSyntax FixMethod(
        bool addAsyncModifier,
        bool keepVoid,
        IMethodSymbol methodSymbol,
        MethodDeclarationSyntax method,
        KnownTaskTypes knownTypes)
    {
        var fixedReturnType = FixMethodReturnType(keepVoid, methodSymbol, method.ReturnType, knownTypes);

        if (addAsyncModifier)
        {
            var (newModifiers, newReturnType) = AddAsyncModifierWithCorrectedTrivia(method.Modifiers, fixedReturnType);
            return method.WithReturnType(newReturnType).WithModifiers(newModifiers);
        }
        else
        {
            return method.WithReturnType(fixedReturnType);
        }
    }

    private static LocalFunctionStatementSyntax FixLocalFunction(
        bool keepVoid,
        IMethodSymbol methodSymbol,
        LocalFunctionStatementSyntax localFunction,
        KnownTaskTypes knownTypes)
    {
        var (newModifiers, newReturnType) = AddAsyncModifierWithCorrectedTrivia(
            localFunction.Modifiers,
            FixMethodReturnType(keepVoid, methodSymbol, localFunction.ReturnType, knownTypes));
        return localFunction.WithReturnType(newReturnType).WithModifiers(newModifiers);
    }

    private static TypeSyntax FixMethodReturnType(
        bool keepVoid,
        IMethodSymbol methodSymbol,
        TypeSyntax returnTypeSyntax,
        KnownTaskTypes knownTypes)
    {
        var newReturnType = returnTypeSyntax.WithAdditionalAnnotations(Formatter.Annotation);

        if (methodSymbol.ReturnsVoid)
        {
            if (!keepVoid)
            {
                newReturnType = knownTypes.TaskType!.GenerateTypeSyntax();
            }
        }
        else
        {
            var returnType = methodSymbol.ReturnType;
            if (IsIEnumerable(returnType, knownTypes) && methodSymbol.IsIterator)
            {
                newReturnType = knownTypes.IAsyncEnumerableOfTType is null
                    ? MakeGenericType(nameof(IAsyncEnumerable<>), methodSymbol.ReturnType)
                    : knownTypes.IAsyncEnumerableOfTType.Construct(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
            }
            else if (IsIEnumerator(returnType, knownTypes) && methodSymbol.IsIterator)
            {
                newReturnType = knownTypes.IAsyncEnumeratorOfTType is null
                    ? MakeGenericType(nameof(IAsyncEnumerator<>), methodSymbol.ReturnType)
                    : knownTypes.IAsyncEnumeratorOfTType.Construct(methodSymbol.ReturnType.GetTypeArguments()[0]).GenerateTypeSyntax();
            }
            else if (IsIAsyncEnumerableOrEnumerator(returnType, knownTypes))
            {
                // Leave the return type alone
            }
            else if (!knownTypes.IsTaskLike(returnType))
            {
                // If it's not already Task-like, then wrap the existing return type
                // in Task<>.
                newReturnType = knownTypes.TaskOfTType!.Construct(methodSymbol.ReturnType).GenerateTypeSyntax();
            }
        }

        return newReturnType.WithTriviaFrom(returnTypeSyntax).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);

        static TypeSyntax MakeGenericType(string type, ITypeSymbol typeArgumentFrom)
        {
            var result = GenericName(
                Identifier(type),
                TypeArgumentList([typeArgumentFrom.GetTypeArguments()[0].GenerateTypeSyntax()]));

            return result.WithAdditionalAnnotations(Simplifier.Annotation);
        }
    }

    private static bool IsIAsyncEnumerableOrEnumerator(ITypeSymbol returnType, KnownTaskTypes knownTypes)
        => returnType.OriginalDefinition.Equals(knownTypes.IAsyncEnumerableOfTType) ||
           returnType.OriginalDefinition.Equals(knownTypes.IAsyncEnumeratorOfTType);

    private static bool IsIEnumerable(ITypeSymbol returnType, KnownTaskTypes knownTypes)
        => returnType.OriginalDefinition.Equals(knownTypes.IEnumerableOfTType);

    private static bool IsIEnumerator(ITypeSymbol returnType, KnownTaskTypes knownTypes)
        => returnType.OriginalDefinition.Equals(knownTypes.IEnumeratorOfTType);

    private static (SyntaxTokenList newModifiers, TypeSyntax newReturnType) AddAsyncModifierWithCorrectedTrivia(SyntaxTokenList modifiers, TypeSyntax returnType)
    {
        if (modifiers.Any())
        {
            // 'partial' modifier must say at the end of the modifiers arrays.
            var partialModifier = modifiers.FirstOrDefault(static m => m.IsKind(SyntaxKind.PartialKeyword));
            if (partialModifier != default)
            {
                var insertionIndex = modifiers.IndexOf(partialModifier);
                modifiers = modifiers.Replace(partialModifier, partialModifier.WithoutLeadingTrivia());

                return (modifiers.Insert(insertionIndex, s_asyncKeywordWithSpace.WithLeadingTrivia(partialModifier.LeadingTrivia)), returnType);
            }
            else
            {
                return (modifiers.Add(s_asyncKeywordWithSpace), returnType);
            }
        }

        // Move the leading trivia from the return type to the new modifiers list.
        var newModifiers = TokenList(s_asyncKeywordWithSpace.WithLeadingTrivia(returnType.GetLeadingTrivia()));
        var newReturnType = returnType.WithoutLeadingTrivia();
        return (newModifiers, newReturnType);
    }

    private static AnonymousFunctionExpressionSyntax FixAnonymousFunction(AnonymousFunctionExpressionSyntax anonymous)
        => anonymous
            .WithoutLeadingTrivia()
            .WithAsyncKeyword(AsyncKeyword.WithPrependedLeadingTrivia(anonymous.GetLeadingTrivia()));
}
