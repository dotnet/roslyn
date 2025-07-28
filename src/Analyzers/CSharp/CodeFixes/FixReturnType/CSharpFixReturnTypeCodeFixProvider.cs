// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType;

/// <summary>
/// Helps fix void-returning methods or local functions to return a correct type.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FixReturnType), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpFixReturnTypeCodeFixProvider()
    : SyntaxEditorBasedCodeFixProvider(supportsFixAll: false)
{
    // error CS0127: Since 'M()' returns void, a return keyword must not be followed by an object expression
    // error CS1997: Since 'M()' is an async method that returns 'Task', a return keyword must not be followed by an object expression
    // error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
    public override ImmutableArray<string> FixableDiagnosticIds => ["CS0127", "CS1997", "CS0201"];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var diagnostics = context.Diagnostics;
        var cancellationToken = context.CancellationToken;

        var analyzedTypes = await TryGetOldAndNewReturnTypeAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
        if (analyzedTypes == default)
            return;

        if (IsVoid(analyzedTypes.declarationToFix) && IsVoid(analyzedTypes.fixedDeclaration))
        {
            // Don't offer a code fix if the return type is void and return is followed by a void expression.
            // See https://github.com/dotnet/roslyn/issues/47089
            return;
        }

        RegisterCodeFix(context, CSharpCodeFixesResources.Fix_return_type, nameof(CSharpCodeFixesResources.Fix_return_type));

        return;

        static bool IsVoid(TypeSyntax typeSyntax)
            => typeSyntax is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword };
    }

    private static async Task<(TypeSyntax declarationToFix, TypeSyntax fixedDeclaration)> TryGetOldAndNewReturnTypeAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        Debug.Assert(diagnostics.Length == 1);
        var location = diagnostics[0].Location;
        var node = location.FindNode(getInnermostNodeForTie: true, cancellationToken);
        var returnedValue = node is ReturnStatementSyntax returnStatement ? returnStatement.Expression : node;
        if (returnedValue is null)
            return default;

        var (declarationTypeToFix, isAsync) = TryGetDeclarationTypeToFix(node);
        if (declarationTypeToFix is null)
            return default;

        var syntaxGenerator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var returnedType = semanticModel.GetTypeInfo(returnedValue, cancellationToken).Type;

        // Special case when tuple has elements with unknown type, e.g. `(null, default)`
        // Need to replace this unknown elements with default `object`s
        if (returnedType is null &&
            returnedValue is TupleExpressionSyntax tuple)
        {
            returnedType = InferTupleType(tuple, semanticModel, cancellationToken);
        }

        returnedType ??= semanticModel.Compilation.ObjectType;

        var fixedDeclaration = returnedType.GenerateTypeSyntax(allowVar: false);

        if (isAsync)
        {
            var previousReturnType = semanticModel.GetTypeInfo(declarationTypeToFix, cancellationToken).Type;
            if (previousReturnType is null)
                return default;

            var compilation = semanticModel.Compilation;

            INamedTypeSymbol? taskType = null;

            // void, Task -> Task<T>
            // ValueTask -> ValueTask<T>
            // other type -> we cannot infer anything
            if (previousReturnType.SpecialType is SpecialType.System_Void ||
                Equals(previousReturnType, compilation.TaskType()))
            {
                taskType = compilation.TaskOfTType();
            }
            else if (Equals(previousReturnType, compilation.ValueTaskType()))
            {
                taskType = compilation.ValueTaskOfTType();
            }

            if (taskType is null)
                return default;

            var taskTypeSyntax = taskType.GenerateTypeSyntax(allowVar: false);
            fixedDeclaration = (TypeSyntax)syntaxGenerator.WithTypeArguments(taskTypeSyntax, fixedDeclaration);
        }

        fixedDeclaration = fixedDeclaration.WithAdditionalAnnotations(Simplifier.Annotation).WithTriviaFrom(declarationTypeToFix);

        return (declarationTypeToFix, fixedDeclaration);
    }

    protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var (declarationTypeToFix, fixedDeclaration) =
            await TryGetOldAndNewReturnTypeAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);

        editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
    }

    private static (TypeSyntax type, bool isAsync) TryGetDeclarationTypeToFix(SyntaxNode node)
    {
        return node.GetAncestors().Select(TryGetReturnTypeToFix).FirstOrDefault(p => p.type != null);

        static (TypeSyntax type, bool isAsync) TryGetReturnTypeToFix(SyntaxNode containingMember)
        {
            return containingMember switch
            {
                // void M() { return 1; }
                // async Task M() { return 1; }
                MethodDeclarationSyntax method => (method.ReturnType, method.Modifiers.Any(SyntaxKind.AsyncKeyword)),
                // void local() { return 1; }
                // async Task local() { return 1; }
                LocalFunctionStatementSyntax localFunction => (localFunction.ReturnType, localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword)),
                _ => default,
            };
        }
    }

    private static ITypeSymbol? InferTupleType(TupleExpressionSyntax tuple, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var compilation = semanticModel.Compilation;
        var argCount = tuple.Arguments.Count;

        var baseTupleType = compilation.ValueTupleType(argCount);
        if (baseTupleType is null)
            return null;

        var inferredTupleTypes = new ITypeSymbol[argCount];

        for (var i = 0; i < argCount; i++)
        {
            var argumentExpression = tuple.Arguments[i].Expression;
            var type = semanticModel.GetTypeInfo(argumentExpression, cancellationToken).Type;

            // Nested tuple with unknown type, e.g. `(string.Empty, (2, null))`
            if (type is null &&
                argumentExpression is TupleExpressionSyntax nestedTuple)
            {
                type = InferTupleType(nestedTuple, semanticModel, cancellationToken);
            }

            inferredTupleTypes[i] = type is null ? semanticModel.Compilation.ObjectType : type;
        }

        return baseTupleType.Construct(inferredTupleTypes);
    }
}
