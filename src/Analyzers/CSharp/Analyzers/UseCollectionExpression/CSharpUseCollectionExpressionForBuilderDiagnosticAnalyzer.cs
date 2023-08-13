// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForBuilderDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    private const string CreateBuilderName = nameof(ImmutableArray.CreateBuilder);
    private const string GetInstanceName = nameof(ArrayBuilder<int>.GetInstance);

    public const string UnwrapArgument = nameof(UnwrapArgument);

    private static readonly ImmutableDictionary<string, string?> s_unwrapArgumentProperties =
        ImmutableDictionary<string, string?>.Empty.Add(UnwrapArgument, UnwrapArgument);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    private static readonly DiagnosticDescriptor s_descriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForBuilder,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: false);

    private static readonly DiagnosticDescriptor s_unnecessaryCodeDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.UseCollectionExpressionForBuilderDiagnosticId,
        EnforceOnBuildValues.UseCollectionExpressionForBuilder,
        new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        isUnnecessary: true);

    public CSharpUseCollectionExpressionForBuilderDiagnosticAnalyzer()
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

            // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to
            // get callbacks for object creation expression nodes, but analyze nodes across the entire code block
            // and eventually report fading diagnostics with location outside this node.
            // Without the containing CodeBlockStartAction, our reported diagnostic would be classified
            // as a non-local diagnostic and would not participate in lightbulb for computing code fixes.
            context.RegisterCodeBlockStartAction<SyntaxKind>(context =>
            {
                context.RegisterSyntaxNodeAction(
                    context => AnalyzeInvocationExpression(context),
                    SyntaxKind.InvocationExpression);
            });
        });

    private static void AnalyzeInvocationExpression(
        SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var compilation = semanticModel.Compilation;
        var syntaxTree = semanticModel.SyntaxTree;
        var invocationExpression = (InvocationExpressionSyntax)context.Node;
        var cancellationToken = context.CancellationToken;

        // no point in analyzing if the option is off.
        var option = context.GetAnalyzerOptions().PreferCollectionExpression;
        if (!option.Value)
            return;

        //if (!IsCompatibleSignatureAndArguments(
        //        compilation, invocationExpression, createMethod.OriginalDefinition,
        //        out var unwrapArgument, cancellationToken))
        //{
        //    return;
        //}

        var matches = AnalyzeInvocation(semanticModel, invocationExpression, cancellationToken);
        if (matches == null)
            return;

        UseCollectionExpressionForAnalyzer.Analyze(localD)

        // Make sure we can actually use a collection expression in place of the full invocation.
        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, invocationExpression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return;
        }

        var locations = ImmutableArray.Create(invocationExpression.GetLocation());
        var properties = unwrapArgument ? s_unwrapArgumentProperties : null;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            s_descriptor,
            memberAccessExpression.Name.Identifier.GetLocation(),
            option.Notification.Severity,
            additionalLocations: locations,
            properties));

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
            additionalUnnecessaryLocations: additionalUnnecessaryLocations,
            properties));
    }

    private static CollectionBuilderMatches? AnalyzeInvocation(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocationExpression,
        CancellationToken cancellationToken)
    {
        // Looking for `XXX.Create(...)`
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression) memberAccessExpression)
            return null;

        if (memberAccessExpression.Name.Identifier.ValueText is not CreateBuilderName and not GetInstanceName)
            return null;

        if (memberAccessExpression.Name.Identifier.ValueText == GetInstanceName &&
            memberAccessExpression.Expression is not GenericNameSyntax { Identifier.ValueText: nameof(ArrayBuilder<int>) })
        {
            return null;
        }

        if (memberAccessExpression.Expression is not SimpleNameSyntax)
            return null;

        var createMethod = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol;
        if (createMethod is not IMethodSymbol { IsStatic: true })
            return null;

        var factoryType = semanticModel.GetSymbolInfo(memberAccessExpression.Expression, cancellationToken).Symbol as INamedTypeSymbol;
        if (factoryType is null)
            return null;

        // has to be the form: `Builder b = XXX.CreateBuilder();` or
        //                     `var _ = XXX.CreateBuilder(out var builder);
        if (invocationExpression.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax } declarator } })
            return null;

        var arguments = invocationExpression.ArgumentList.Arguments;
        if (arguments is not [] and not [{ RefKindKeyword.RawKind: (int)SyntaxKind.OutKeyword, Expression: DeclarationExpressionSyntax }])
            return null;


    }

    private static bool IsCompatibleSignatureAndArguments(
        Compilation compilation,
        InvocationExpressionSyntax invocationExpression,
        IMethodSymbol originalCreateMethod,
        out bool unwrapArgument,
        CancellationToken cancellationToken)
    {
        unwrapArgument = false;

        var arguments = invocationExpression.ArgumentList.Arguments;

        // Don't bother offering if any of the arguments are named.  It's unlikely for this to occur in practice, and it
        // means we do not have to worry about order of operations.
        if (arguments.Any(static a => a.NameColon != null))
            return false;

        if (originalCreateMethod.Name is CreateRangeName)
        {
            // If we have `CreateRange<T>(IEnumerable<T> values)` this is legal if we have an array, or no-arg object creation.
            if (originalCreateMethod.Parameters is [
                {
                    Type: INamedTypeSymbol
                    {
                        Name: nameof(IEnumerable<int>),
                        TypeArguments: [ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }]
                    } enumerableType
                }] && enumerableType.OriginalDefinition.Equals(compilation.IEnumerableOfTType()))
            {
                var argExpression = arguments[0].Expression;
                if (argExpression
                        is ArrayCreationExpressionSyntax { Initializer: not null }
                        or ImplicitArrayCreationExpressionSyntax)
                {
                    unwrapArgument = true;
                    return true;
                }

                if (argExpression is ObjectCreationExpressionSyntax objectCreation)
                {
                    // Can't have any arguments, as we cannot preserve them once we grab out all the elements.
                    if (objectCreation.ArgumentList != null && objectCreation.ArgumentList.Arguments.Count > 0)
                        return false;

                    // If it's got an initializer, it has to be a collection initializer (or an empty object initializer);
                    if (objectCreation.Initializer.IsKind(SyntaxKind.ObjectCreationExpression) && objectCreation.Initializer.Expressions.Count > 0)
                        return false;

                    unwrapArgument = true;
                    return true;
                }
            }
        }
        else if (originalCreateMethod.Name is CreateName)
        {
            // `XXX.Create()` can be converted to `[]`
            if (originalCreateMethod.Parameters.Length == 0)
                return arguments.Count == 0;

            // If we have `Create<T>(T)`, `Create<T>(T, T)` etc., then this is convertible.
            if (originalCreateMethod.Parameters.All(static p => p.Type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }))
                return arguments.Count == originalCreateMethod.Parameters.Length;

            // If we have `Create<T>(params T[])` this is legal if there are multiple arguments.  Or a single argument that
            // is an array literal.
            if (originalCreateMethod.Parameters is [{ IsParams: true, Type: IArrayTypeSymbol { ElementType: ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } } }])
            {
                if (arguments.Count >= 2)
                    return true;

                if (arguments is [{ Expression: ArrayCreationExpressionSyntax { Initializer: not null } or ImplicitArrayCreationExpressionSyntax }])
                {
                    unwrapArgument = true;
                    return true;
                }

                return false;
            }

            // If we have `Create<T>(ReadOnlySpan<T> values)` this is legal if a stack-alloc expression is passed along.
            //
            // Runtime needs to support inline arrays in order for this to be ok.  Otherwise compiler will change the
            // stack alloc to a heap alloc, which could be very bad for user perf.

            if (arguments.Count == 1 &&
                compilation.SupportsRuntimeCapability(RuntimeCapability.InlineArrayTypes) &&
                originalCreateMethod.Parameters is [
                    {
                        Type: INamedTypeSymbol
                        {
                            Name: nameof(Span<int>) or nameof(ReadOnlySpan<int>),
                            TypeArguments: [ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }]
                        } spanType
                    }])
            {
                if (spanType.OriginalDefinition.Equals(compilation.SpanOfTType()) ||
                    spanType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType()))
                {
                    if (arguments[0].Expression
                            is StackAllocArrayCreationExpressionSyntax { Initializer: not null }
                            or ImplicitStackAllocArrayCreationExpressionSyntax)
                    {
                        unwrapArgument = true;
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
