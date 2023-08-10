// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCollectionExpressionForCreate), Shared]
internal partial class CSharpUseCollectionExpressionForCreateCodeFixProvider
    : ForkingSyntaxEditorBasedCodeFixProvider<InvocationExpressionSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpUseCollectionExpressionForCreateCodeFixProvider()
        : base(CSharpCodeFixesResources.Use_collection_expression,
               IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId)
    {
    }

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId);

    protected override Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        InvocationExpressionSyntax invocationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var unwrapArguments = properties.ContainsKey(CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer.UnwrapArguments);

        var expressions = GetExpressions(invocationExpression, unwrapArguments);
    }

    private ImmutableArray<ExpressionSyntax> GetExpressions(InvocationExpressionSyntax invocationExpression, bool unwrapArguments)
    {
        var arguments = invocationExpression.ArgumentList.Arguments;

        // If we're not unwrapping a singular argument expression, then just pass back all the explicit argument
        // expressions the user wrote out.
        if (!unwrapArguments)
            return arguments.SelectAsArray(static a => a.Expression);

        Contract.ThrowIfTrue(arguments.Count != 1);
        var expression = arguments.Single().Expression;

        return expression switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray
                => GetExpressions(implicitArray.Initializer),

            ImplicitStackAllocArrayCreationExpressionSyntax implicitStackAlloc
                => GetExpressions(implicitStackAlloc.Initializer),

            ArrayCreationExpressionSyntax arrayCreation
                => GetExpressions(arrayCreation.Initializer),

            StackAllocArrayCreationExpressionSyntax stackAllocCreation
                => GetExpressions(stackAllocCreation.Initializer),

            ImplicitObjectCreationExpressionSyntax implicitObjectCreation
                => GetExpressions(implicitObjectCreation.Initializer),

            ObjectCreationExpressionSyntax objectCreation
                => GetExpressions(objectCreation.Initializer),

            _ => throw ExceptionUtilities.Unreachable();
        };

        static ImmutableArray<ExpressionSyntax> GetExpressions(InitializerExpressionSyntax? initializer)
            => initializer is null
                ? ImmutableArray<ExpressionSyntax>.Empty
                : initializer.Expressions.ToImmutableArray();
    }
}
