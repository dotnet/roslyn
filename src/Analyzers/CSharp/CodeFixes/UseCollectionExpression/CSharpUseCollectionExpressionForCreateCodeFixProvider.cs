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

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        CodeActionOptionsProvider fallbackOptions,
        InvocationExpressionSyntax invocationExpression,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        var unwrapArgument = properties.ContainsKey(CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer.UnwrapArgument);

        // the option is currently not an editorconfig option, so not available in code style layer
#if CODE_STYLE
        var wrappingLength = CodeActionOptions.DefaultCollectionExpressionWrappingLength;
#else
        var wrappingLength = fallbackOptions.GetOptions(document.Project.Services).CollectionExpressionWrappingLength;
#endif

        // Get the expressions that we're going to fill the new collection expression with.
        var expressions = GetExpressions(invocationExpression, unwrapArgument);

        // We want to replace `XXX.Create(...)` with the new collection expression.  To do this, we go through the
        // following steps.  First, we replace `XXX.Create(...)` with `new()` (an empty object creation expression). We
        // then call into our helper which replaces expressions with collection expressions.  The reason for the dummy
        // object creation expression is that it serves as an actual node the rewriting code can attach an initializer
        // to, by which it can figure out appropriate wrapping and indentation for the collection expression elements.

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var syntaxAnnotation = new SyntaxAnnotation();
        var dummyObjectCreation = ImplicitObjectCreationExpression().WithAdditionalAnnotations(syntaxAnnotation);
    }

    private static ImmutableArray<ExpressionSyntax> GetExpressions(InvocationExpressionSyntax invocationExpression, bool unwrapArgument)
    {
        var arguments = invocationExpression.ArgumentList.Arguments;

        // If we're not unwrapping a singular argument expression, then just pass back all the explicit argument
        // expressions the user wrote out.
        if (!unwrapArgument)
            return arguments.SelectAsArray(static a => a.Expression);

        Contract.ThrowIfTrue(arguments.Count != 1);
        var expression = arguments.Single().Expression;

        var initializer = expression switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer,
            ImplicitStackAllocArrayCreationExpressionSyntax implicitStackAlloc => implicitStackAlloc.Initializer,
            ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
            StackAllocArrayCreationExpressionSyntax stackAllocCreation => stackAllocCreation.Initializer,
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation => implicitObjectCreation.Initializer,
            ObjectCreationExpressionSyntax objectCreation => objectCreation.Initializer,
            _ => throw ExceptionUtilities.Unreachable(),
        };

        return initializer is null
            ? ImmutableArray<ExpressionSyntax>.Empty
            : initializer.Expressions.ToImmutableArray();
    }
}
