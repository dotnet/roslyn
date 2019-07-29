// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using static Helpers;

    /// <summary>
    /// Analyzer that looks for code like: 
    /// 
    /// 1) `s[s.Length - n]` and offers to change that to `s[^n]`. and.
    /// 2) `s.Get(s.Length - n)` and offers to change that to `s.Get(^n)`
    ///
    /// In order to do convert between indexers, the type must look 'indexable'.  Meaning, it must
    /// have an int-returning property called 'Length' or 'Count', and it must have both an
    /// int-indexer, and a System.Index-indexer.  In order to convert between methods, the type
    /// must have identical overloads except that one takes an int, and the other a System.Index.
    ///
    /// It is assumed that if the type follows this shape that it is well behaved and that this
    /// transformation will preserve semantics.  If this assumption is not good in practice, we
    /// could always limit the feature to only work on a whitelist of known safe types.
    /// 
    /// Note that this feature only works if the code literally has `expr1.Length - expr2`.  If
    /// code has this, and is calling into a method that takes either an int or a System.Index,
    /// it feels very safe to assume this is well behaved and switching to `^expr2` is going to
    /// preserve semantics.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseIndexOperatorDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseIndexOperatorDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseIndexOperatorDiagnosticId,
                   CSharpCodeStyleOptions.PreferIndexOperator,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_index_operator), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Indexing_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(startContext =>
            {
                // We're going to be checking every property-reference and invocation in the
                // compilation. Cache information we compute in this object so we don't have to
                // continually recompute it.
                var compilation = startContext.Compilation;
                var infoCache = new InfoCache(compilation);

                // The System.Index type is always required to offer this fix.
                if (infoCache.IndexType == null)
                {
                    return;
                }

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
            {
                return;
            }

            AnalyzeInvokedMember(
                context, infoCache,
                invocationOperation.Instance,
                invocationOperation.TargetMethod,
                invocationOperation.Arguments[0].Value,
                lengthLikePropertyOpt: null,
                cancellationToken);
        }

        private void AnalyzePropertyReference(
            OperationAnalysisContext context, InfoCache infoCache)
        {
            var cancellationToken = context.CancellationToken;
            var propertyReference = (IPropertyReferenceOperation)context.Operation;

            // Only analyze indexer calls.
            if (!propertyReference.Property.IsIndexer)
            {
                return;
            }

            if (propertyReference.Arguments.Length != 1)
            {
                return;
            }

            AnalyzeInvokedMember(
                context, infoCache,
                propertyReference.Instance,
                propertyReference.Property.GetMethod,
                propertyReference.Arguments[0].Value,
                lengthLikePropertyOpt: null,
                cancellationToken);
        }

        private void AnalyzeArrayElementReference(
            OperationAnalysisContext context, InfoCache infoCache, IPropertySymbol arrayLengthProperty)
        {
            var cancellationToken = context.CancellationToken;
            var arrayElementReference = (IArrayElementReferenceOperation)context.Operation;

            // Has to be a single-dimensional element access.
            if (arrayElementReference.Indices.Length != 1)
            {
                return;
            }

            AnalyzeInvokedMember(
                context, infoCache,
                arrayElementReference.ArrayReference,
                targetMethodOpt: null,
                arrayElementReference.Indices[0],
                lengthLikePropertyOpt: arrayLengthProperty,
                cancellationToken);
        }

        private void AnalyzeInvokedMember(
            OperationAnalysisContext context, InfoCache infoCache,
            IOperation instance, IMethodSymbol targetMethodOpt, IOperation argumentValue,
            IPropertySymbol lengthLikePropertyOpt, CancellationToken cancellationToken)
        {
            // look for `s[s.Length - value]` or `s.Get(s.Length- value)`.

            // Needs to have the one arg for `s.Length - value`, and that arg needs to be
            // a subtraction.
            if (instance is null ||
                !IsSubtraction(argumentValue, out var subtraction))
            {
                return;
            }

            if (!(subtraction.Syntax is BinaryExpressionSyntax binaryExpression))
            {
                return;
            }

            // Only supported on C# 8 and above.
            var syntaxTree = binaryExpression.SyntaxTree;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
            {
                return;
            }

            // Don't bother analyzing if the user doesn't like using Index/Range operators.
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet is null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferIndexOperator);
            if (!option.Value)
            {
                return;
            }

            // Ok, looks promising.  We're indexing in with some subtraction expression. Examine the
            // type this indexer is in to see if there's another member that takes a System.Index
            // that we can convert to.
            //
            // Also ensure that the left side of the subtraction : `s.Length - value` is actually
            // getting the length off the same instance we're indexing into.

            lengthLikePropertyOpt ??= TryGetLengthLikeProperty(infoCache, targetMethodOpt);
            if (lengthLikePropertyOpt == null ||
                !IsInstanceLengthCheck(lengthLikePropertyOpt, instance, subtraction.LeftOperand))
            {
                return;
            }

            // Everything looks good.  We can update this to use the System.Index member instead.
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    binaryExpression.GetLocation(),
                    option.Notification.Severity,
                    ImmutableArray<Location>.Empty,
                    ImmutableDictionary<string, string>.Empty));
        }

        private IPropertySymbol TryGetLengthLikeProperty(InfoCache infoCache, IMethodSymbol targetMethodOpt)
            => targetMethodOpt != null && infoCache.TryGetMemberInfo(targetMethodOpt, out var memberInfo)
                ? memberInfo.LengthLikeProperty
                : null;
    }
}
