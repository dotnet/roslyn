// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer
    : AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
{
    private const string CreateName = nameof(ImmutableArray.Create);
    private const string CreateRangeName = nameof(ImmutableArray.CreateRange);

    public const string UnwrapArgument = nameof(UnwrapArgument);

    private static readonly ImmutableDictionary<string, string?> s_unwrapArgumentProperties =
        ImmutableDictionary<string, string?>.Empty.Add(UnwrapArgument, UnwrapArgument);

    public CSharpUseCollectionExpressionForCreateDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCollectionExpressionForCreateDiagnosticId,
               EnforceOnBuildValues.UseCollectionExpressionForCreate)
    {
    }

    protected override bool IsSupported(Compilation compilation)
        => compilation.CollectionBuilderAttribute() is not null;

    protected override void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context)
        => context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);

    private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
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

        // Looking for `XXX.Create(...)`
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax
            {
                RawKind: (int)SyntaxKind.SimpleMemberAccessExpression,
                Name.Identifier.Value: CreateName or CreateRangeName,
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
        var collectionBuilderAttribute = compilation.CollectionBuilderAttribute()!;
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
        if (!IsCompatibleSignatureAndArguments(
                compilation, invocationExpression, createMethod.OriginalDefinition,
                out var unwrapArgument, cancellationToken))
        {
            return;
        }

        // Make sure we can actually use a collection expression in place of the full invocation.
        if (!UseCollectionExpressionHelpers.CanReplaceWithCollectionExpression(
                semanticModel, invocationExpression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return;
        }

        var locations = ImmutableArray.Create(invocationExpression.GetLocation());
        var properties = unwrapArgument ? s_unwrapArgumentProperties : null;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
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
            UnnecessaryCodeDescriptor,
            additionalUnnecessaryLocations[0],
            ReportDiagnostic.Default,
            additionalLocations: locations,
            additionalUnnecessaryLocations: additionalUnnecessaryLocations,
            properties));
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
