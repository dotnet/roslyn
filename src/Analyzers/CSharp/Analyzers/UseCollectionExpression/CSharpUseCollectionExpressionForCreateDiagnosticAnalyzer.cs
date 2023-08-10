// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    private const string CreateName = "Create";

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForCreate,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForCreate,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    public CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer()
        : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
                .Add(s_descriptor, CodeStyleOptions2.PreferCollectionExpression)
                .Add(s_unnecessaryCodeDescriptor, CodeStyleOptions2.PreferCollectionExpression))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            if (!compilation.LanguageVersion().SupportsCollectionExpressions())
                return;

            var collectionBuilderAttribute = compilation.CollectionBuilderAttribute();
            if (collectionBuilderAttribute is null)
                return;

            // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
            // get callbacks for object creation expression nodes, but analyze nodes across the entire code block
            // and eventually report fading diagnostics with location outside this node.
            // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
            // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
            context.RegisterCodeBlockStartAction<SyntaxKind>(context =>
            {
                context.RegisterSyntaxNodeAction(
                    context => AnalyzeInvocationExpression(context, collectionBuilderAttribute),
                    SyntaxKind.InvocationExpression);
            });
        });

    private static void AnalyzeInvocationExpression(
        SyntaxNodeAnalysisContext context,
        INamedTypeSymbol collectionBuilderAttribute)
    {
        var semanticModel = context.SemanticModel;
        var syntaxTree = semanticModel.SyntaxTree;
        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        // Looking for `XXX.Create(...)`
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax
            {
                RawKind: (int)SyntaxKind.SimpleMemberAccessExpression,
                Name.Identifier.Value: CreateName,
            } memberAccessExpression)
        {
            return;
        }

        var createMethod = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol as IMethodSymbol;
        if (createMethod is not { IsStatic: true })
            return;

        var factoryType = semanticModel.GetSymbolInfo(memberAccessExpression.Expression, cancellationToken).Symbol as INamedTypeSymbol;
        if (factoryType is null)
            return;

        // The pattern is a type like `ImmutableArray` (non-generic), returning an instance of `ImmutableArray<T>`.  The
        // actual collection type (`ImmutableArray<T>`) has to have a `[CollectionBuilder(...)]` attribute on it that
        // then points at the factory type.
        var collectionBuilderAttributeData = createMethod.ReturnType.OriginalDefinition
            .GetAttributes()
            .FirstOrDefault(a => collectionBuilderAttribute.Equals(a.AttributeClass));
        if (collectionBuilderAttributeData?.ConstructorArguments is not [{ Value: ITypeSymbol collectionBuilderType }, { Value: CreateName }])
            return;

        if (!factoryType.OriginalDefinition.Equals(collectionBuilderType.OriginalDefinition))
            return;

        // Ok, this is type that has a collection-builder option available.  We can switch over if the current method
        // we're calling has one of the following signatures:
        //
        //  `Create()`.  Trivial case, can be replaced with `[]`.
        //  `Create(T), Create(T, T), Create(T, T, T)` etc.
        //  `Create(params T[])` (passing as individual elements, or an array with an initializer)
        //  `Create(ReadOnlySpan<T>)` (passing as a stack-alloc with an initializer)
        //  `Create(IEnumerable<T>)` (passing as something with an initializer.
        if (!IsCompatibleSignatureAndArguments(semanticModel.Compilation, invocationExpression, createMethod.OriginalDefinition, cancellationToken))
            return;

        // Make sure we can actually use a collection expression in place of the full invocation.
        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, invocationExpression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return;
        }

        var locations = ImmutableArray.Create(invocationExpression.GetLocation());
        context.ReportDiagnostic(DiagnosticHelper.Create(
            s_descriptor,
            memberAccessExpression.Name.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties: null));

        var additionalUnnecessaryLocations = ImmutableArray.Create(
            syntaxTree.GetLocation(TextSpan.FromBounds(
                invocationExpression.SpanStart,
                invocationExpression.ArgumentList.OpenParenToken.Span.End)),
            invocationExpression.ArgumentList.CloseParenToken.GetLocation());

        context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
            s_unnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations));
    }

    private static bool IsCompatibleSignatureAndArguments(
        Compilation compilation,
        InvocationExpressionSyntax invocationExpression,
        IMethodSymbol originalCreateMethod,
        CancellationToken cancellationToken)
    {
        // `XXX.Create()` can always be converted to `[]`
        if (originalCreateMethod.Parameters.Length == 0)
            return true;

        var arguments = invocationExpression.ArgumentList.Arguments;

        // Don't bother offering if any of the arguments are named.  It's unlikely for this to occur in practice, and it
        // means we do not have to worry about order of operations.
        if (arguments.Any(static a => a.NameColon != null))
            return false;

        // If we have `Create<T>(T)`, `Create<T>(T, T)` etc., then this is convertible.
        if (originalCreateMethod.Parameters.All(static p => p.Type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }))
            return true;

        // If we have `Create<T>(params T[])` this is legal if there are multiple arguments.  Or a single argument that
        // is an array literal.
        if (originalCreateMethod.Parameters is [{ IsParams: true, Type: IArrayTypeSymbol { ElementType: ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } } }])
        {
            if (arguments.Count >= 2)
                return true;

            if (arguments is [{ Expression: ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax])
                return true;

            return false;
        }

        // If we have `Create<T>(ReadOnlySpan<T> values)` this is legal if a stack-alloc expression is passed along.
        if (arguments.Count == 1 &&
            originalCreateMethod.Parameters is [INamedTypeSymbol
            {
                Name: nameof(Span<int>) or nameof(ReadOnlySpan<int>),
                TypeArguments: [ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }]
            } spanType])
        {
            if (spanType.OriginalDefinition.Equals(compilation.SpanOfTType()) ||
                spanType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType()))
            {
                if (arguments[0].Expression is StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax)
                    return true;
            }
        }

        // If we have `Create<T>(IEnumerable<T> values)` this is legal if we have an array, or no-arg object creation.
        if (arguments.Count == 1 &&
            originalCreateMethod.Parameters is [INamedTypeSymbol
            {
                Name: nameof(IEnumerable<int>),
                TypeArguments: [ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }]
            } enumerableType] && enumerableType.OriginalDefinition.Equals(compilation.IEnumerableOfTType()))
        {
            if (arguments[0].Expression
                    is ArrayCreationExpressionSyntax
                    or ImplicitArrayCreationExpressionSyntax
                    or ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 }
                    or ImplicitObjectCreationExpressionSyntax {  ArgumentList.Arguments.Count: 0 })
            {
                return true;
            }
        }

        return false;
    }
}
