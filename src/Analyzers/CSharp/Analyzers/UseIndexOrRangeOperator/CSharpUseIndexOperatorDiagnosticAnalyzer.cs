// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator;

using static Helpers;

/// <summary>
/// <para>Analyzer that looks for code like:</para>
///
/// <list type="number">
/// <item><description><c>s[s.Length - n]</c> and offers to change that to <c>s[^n]</c></description></item>
/// <item><description></description><c>s.Get(s.Length - n)</c> and offers to change that to <c>s.Get(^n)</c></item>
/// </list>
///
/// <para>In order to do convert between indexers, the type must look 'indexable'.  Meaning, it must
/// have an <see cref="int"/>-returning property called <c>Length</c> or <c>Count</c>, and it must have both an
/// <see cref="int"/>-indexer, and a <see cref="T:System.Index"/>-indexer.  In order to convert between methods, the type
/// must have identical overloads except that one takes an <see cref="int"/>, and the other a <see cref="T:System.Index"/>.</para>
///
/// <para>It is assumed that if the type follows this shape that it is well behaved and that this
/// transformation will preserve semantics.  If this assumption is not good in practice, we
/// could always limit the feature to only work on an allow list of known safe types.</para>
///
/// <para>Note that this feature only works if the code literally has <c>expr1.Length - expr2</c>. If
/// code has this, and is calling into a method that takes either an <see cref="int"/> or a <see cref="T:System.Index"/>,
/// it feels very safe to assume this is well behaved and switching to <c>^expr2</c> is going to
/// preserve semantics.</para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "Required to avoid ambiguous reference warnings.")]
internal sealed partial class CSharpUseIndexOperatorDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseIndexOperatorDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseIndexOperatorDiagnosticId,
               EnforceOnBuildValues.UseIndexOperator,
               CSharpCodeStyleOptions.PreferIndexOperator,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_index_operator), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Indexing_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            var compilation = (CSharpCompilation)context.Compilation;

            // Only supported on C# 8 and above.
            if (compilation.LanguageVersion < LanguageVersion.CSharp8)
                return;

            // We're going to be checking every property-reference and invocation in the
            // compilation. Cache information we compute in this object so we don't have to
            // continually recompute it.
            if (!InfoCache.TryCreate(compilation, out var infoCache))
                return;

            // Register to hear property references, so we can hear about calls to indexers
            // like: s[s.Length - n]
            context.RegisterOperationAction(
                c => AnalyzePropertyReference(c, infoCache),
                OperationKind.PropertyReference);

            // Register to hear about methods for: s.Get(s.Length - n)
            context.RegisterOperationAction(
                c => AnalyzeInvocation(c, infoCache),
                OperationKind.Invocation);

            var arrayType = compilation.GetSpecialType(SpecialType.System_Array);
            var arrayLengthProperty = TryGetNoArgInt32Property(arrayType, nameof(Array.Length));

            if (arrayLengthProperty != null)
            {
                // Array indexing is represented with a different operation kind.  Register
                // specifically for that.
                context.RegisterOperationAction(
                    c => AnalyzeArrayElementReference(c, infoCache, arrayLengthProperty),
                    OperationKind.ArrayElementReference);
            }
        });
    }

    private void AnalyzeInvocation(
        OperationAnalysisContext context, InfoCache infoCache)
    {
        var cancellationToken = context.CancellationToken;
        var invocationOperation = (IInvocationOperation)context.Operation;

        if (invocationOperation.Arguments.Length != 1)
            return;

        AnalyzeInvokedMember(
            context, infoCache,
            invocationOperation.Instance,
            invocationOperation.TargetMethod,
            invocationOperation.Arguments[0].Value,
            lengthLikeProperty: null,
            cancellationToken);
    }

    private void AnalyzePropertyReference(
        OperationAnalysisContext context, InfoCache infoCache)
    {
        var cancellationToken = context.CancellationToken;
        var propertyReference = (IPropertyReferenceOperation)context.Operation;

        // Only analyze indexer calls.
        if (!propertyReference.Property.IsIndexer)
            return;

        if (propertyReference.Arguments.Length != 1)
            return;

        AnalyzeInvokedMember(
            context, infoCache,
            propertyReference.Instance,
            propertyReference.Property.GetMethod,
            propertyReference.Arguments[0].Value,
            lengthLikeProperty: null,
            cancellationToken);
    }

    private void AnalyzeArrayElementReference(
        OperationAnalysisContext context, InfoCache infoCache, IPropertySymbol arrayLengthProperty)
    {
        var cancellationToken = context.CancellationToken;
        var arrayElementReference = (IArrayElementReferenceOperation)context.Operation;

        // Has to be a single-dimensional element access.
        if (arrayElementReference.Indices.Length != 1)
            return;

        AnalyzeInvokedMember(
            context, infoCache,
            arrayElementReference.ArrayReference,
            targetMethod: null,
            arrayElementReference.Indices[0],
            lengthLikeProperty: arrayLengthProperty,
            cancellationToken);
    }

    private void AnalyzeInvokedMember(
        OperationAnalysisContext context,
        InfoCache infoCache,
        IOperation? instance,
        IMethodSymbol? targetMethod,
        IOperation argumentValue,
        IPropertySymbol? lengthLikeProperty,
        CancellationToken cancellationToken)
    {
        // look for `s[s.Length - value]` or `s.Get(s.Length- value)`.

        // Needs to have the one arg for `s.Length - value`, and that arg needs to be
        // a subtraction.
        if (instance is null ||
            !IsSubtraction(argumentValue, out var subtraction))
        {
            return;
        }

        if (subtraction.Syntax is not BinaryExpressionSyntax binaryExpression)
            return;

        // Don't bother analyzing if the user doesn't like using Index/Range operators.
        var option = context.GetCSharpAnalyzerOptions().PreferIndexOperator;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        // Ok, looks promising.  We're indexing in with some subtraction expression. Examine the
        // type this indexer is in to see if there's another member that takes a System.Index
        // that we can convert to.
        //
        // Also ensure that the left side of the subtraction : `s.Length - value` is actually
        // getting the length off the same instance we're indexing into.

        lengthLikeProperty ??= TryGetLengthLikeProperty(infoCache, targetMethod);
        if (lengthLikeProperty == null ||
            !IsInstanceLengthCheck(lengthLikeProperty, instance, subtraction.LeftOperand))
        {
            return;
        }

        var semanticModel = instance.SemanticModel;
        Contract.ThrowIfNull(semanticModel);

        if (CSharpSemanticFacts.Instance.IsInExpressionTree(semanticModel, instance.Syntax, infoCache.ExpressionOfTType, cancellationToken))
            return;

        // Everything looks good.  We can update this to use the System.Index member instead.
        context.ReportDiagnostic(
            DiagnosticHelper.Create(
                Descriptor,
                binaryExpression.GetLocation(),
                option.Notification,
                context.Options,
                [],
                ImmutableDictionary<string, string?>.Empty));
    }

    private static IPropertySymbol? TryGetLengthLikeProperty(InfoCache infoCache, IMethodSymbol? targetMethod)
        => targetMethod != null && infoCache.TryGetMemberInfo(targetMethod, out var memberInfo)
            ? memberInfo.LengthLikeProperty
            : null;
}
