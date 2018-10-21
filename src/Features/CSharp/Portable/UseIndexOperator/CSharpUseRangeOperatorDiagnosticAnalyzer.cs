// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    using System;
    using static Helpers;

    /// <summary>
    /// Analyzer that looks for several variants of code like `s.Slice(start, end - start)` and
    /// offers to update to `s[start..end]`.  In order to convert, the type being called on needs a
    /// slice-like method that takes two ints, and returns an instance of the same type. It also
    /// needs a Length/Count property, as well as an indexer that takes a System.Range instance.
    ///
    /// It is assumed that if the type follows this shape that it is well behaved and that this
    /// transformation will preserve semantics.  If this assumption is not good in practice, we
    /// could always limit the feature to only work on a whitelist of known safe types.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        // Flags to indicate if we should generate 'val' or '^val' for the start or end range values
        public const string StartFromEnd = nameof(StartFromEnd);
        public const string EndFromEnd = nameof(EndFromEnd);

        // Flags to indicate if we should just omit the start/end value of the range entirely.
        public const string OmitStart = nameof(OmitStart);
        public const string OmitEnd = nameof(OmitEnd);

        public CSharpUseRangeOperatorDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseRangeOperatorDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_range_operator), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources._0_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        /// <summary>
        /// Look for methods like "SomeType SomeType.Slice(int start, int length)".
        /// </summary>
        private static bool IsSliceLikeMethod(IMethodSymbol method)
            => IsPublicInstance(method) &&
               method.Parameters.Length == 2 &&
               IsSliceFirstParameter(method.Parameters[0]) &&
               IsSliceSecondParameter(method.Parameters[1]) &&
               method.ContainingType.Equals(method.ReturnType);

        private static bool IsSliceFirstParameter(IParameterSymbol parameter)
            => parameter.Type.SpecialType == SpecialType.System_Int32 &&
               (parameter.Name == "start" || parameter.Name == "startIndex");

        private static bool IsSliceSecondParameter(IParameterSymbol parameter)
            => parameter.Type.SpecialType == SpecialType.System_Int32 &&
               (parameter.Name == "count" || parameter.Name == "length");

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // We're going to be checking every invocation in the compilation. Cache information
                // we compute in this object so we don't have to continually recompute it.
                var infoCache = new InfoCache(compilationContext.Compilation);
                compilationContext.RegisterOperationAction(
                    c => AnalyzeInvocation(c, infoCache),
                    OperationKind.Invocation);
            });
        }

        private void AnalyzeInvocation(
            OperationAnalysisContext context, InfoCache infoCache)
        {
            var cancellationToken = context.CancellationToken;
            var invocation = (IInvocationOperation)context.Operation;

            var invocationSyntax = invocation.Syntax as InvocationExpressionSyntax;
            if (invocationSyntax is null ||
                invocationSyntax.ArgumentList is null)
            {
                return;
            }

            // Check if we're at least on C# 8, and that the user wants these operators.
            var syntaxTree = invocationSyntax.SyntaxTree;
            var parseOptions = (CSharpParseOptions)syntaxTree.Options;
            //if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
            //{
            //    return;
            //}

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferRangeOperator);
            if (!option.Value)
            {
                return;
            }

            // See if the call is to something slice-like.
            var targetMethod = invocation.TargetMethod;
            if (!IsSliceLikeMethod(invocation.TargetMethod))
            {
                return;
            }

            // See if this is a type we can use range-indexer for, and also if this is a call to the
            // Slice-Like method we've found for that type.  Use the InfoCache so that we can reuse
            // any previously computed values for this type.
            if (!infoCache.TryGetMemberInfo(targetMethod.ContainingType, out var memberInfo) ||
                !targetMethod.Equals(memberInfo.SliceLikeMethod))
            {
                return;
            }

            // look for `s.Slice(start, end - start)` and convert to `s[Range]`

            // Needs to have the two args for `start` and `end - start`
            if (invocation.Instance is null ||
                invocation.Instance.Syntax is null ||
                invocation.Arguments.Length != 2)
            {
                return;
            }

            // Arg2 needs to be a subtraction for: `end - start`
            var arg2 = invocation.Arguments[1];
            if (!(arg2.Value is IBinaryOperation binaryOperation) ||
                binaryOperation.OperatorKind != BinaryOperatorKind.Subtract)
            {
                return;
            }

            var startOperation = invocation.Arguments[0].Value;
            var startSyntax = startOperation.Syntax;

            // Make sure we have: (start, end - start).  The start operation has to be
            // the same as the right side of the subtraction.
            var syntaxFacts = CSharpSyntaxFactsService.Instance;
            if (!syntaxFacts.AreEquivalent(startSyntax, binaryOperation.RightOperand.Syntax))
            {
                return;
            }

            // The end operation is the left side of `end - start`
            var endOperation = binaryOperation.LeftOperand;

            // We have enough information now to generate `start..end`.  However, this will often
            // not be what the user wants.  For example, generating `start..expr.Length` is not as
            // desirable as `start..`.  Similarly, `start..(expr.Length - 1)` is not as desirable as
            // `start..^1`.  Look for these patterns and record what we have so we can produce more
            // idiomatic results in the fixer.
            //
            // Note: we could also compute this in the fixer.  But it's nice and easy to do here
            // given that we already have the options, and it's cheap to do now.

            var properties = ImmutableDictionary<string, string>.Empty;

            var lengthLikeProperty = memberInfo.LengthLikeProperty;

            // If our start-op is actually equivalent to `expr.Length - val`, then just change our
            // start-op to be `val` and record that we should emit it as `^val`.
            if (IsFromEnd(lengthLikeProperty, invocation.Instance, ref startOperation))
            {
                properties = properties.Add(StartFromEnd, StartFromEnd);
            }

            // Similarly, if our end-op is actually equivalent to `expr.Length - val`, then just
            // change our end-op to be `val` and record that we should emit it as `^val`.
            if (IsFromEnd(lengthLikeProperty, invocation.Instance, ref endOperation))
            {
                properties = properties.Add(EndFromEnd, EndFromEnd);
            }

            // If the range operation goes to 'expr.Length' then we can just leave off the end part
            // of the range.  i.e. `start..`
            if (IsInstanceLengthCheck(lengthLikeProperty, invocation.Instance, endOperation))
            {
                properties = properties.Add(OmitEnd, OmitEnd);
            }

            // If we're starting the range operation from 0, then we can just leave off the start of
            // the range. i.e. `..end`
            if (startOperation.ConstantValue.HasValue &&
                startOperation.ConstantValue.Value is 0)
            {
                properties = properties.Add(OmitStart, OmitStart);
            }

            // Keep track of the syntax nodes from the start/end ops so that we can easily 
            // generate the range-expression in the fixer.
            var additionalLocations = ImmutableArray.Create(
                invocationSyntax.GetLocation(),
                startOperation.Syntax.GetLocation(),
                endOperation.Syntax.GetLocation());

            // Mark the span under the two arguments to .Slice(..., ...) as what we will be
            // updating.
            var arguments = invocationSyntax.ArgumentList.Arguments;
            var location = Location.Create(syntaxTree,
                TextSpan.FromBounds(arguments.First().SpanStart, arguments.Last().Span.End));

            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor,
                    location,
                    option.Notification.Severity,
                    additionalLocations,
                    properties,
                    memberInfo.SliceLikeMethod.Name));
        }

        /// <summary>
        /// check if its the form: `expr.Length - value`.  If so, update rangeOperation to then
        /// point to 'value'.
        /// </summary>
        private bool IsFromEnd(
            IPropertySymbol lengthLikeProperty, IOperation instance, ref IOperation rangeOperation)
        {
            if (rangeOperation is IBinaryOperation binaryOperation &&
                binaryOperation.OperatorKind == BinaryOperatorKind.Subtract &&
                IsInstanceLengthCheck(lengthLikeProperty, instance, binaryOperation.LeftOperand))
            {
                rangeOperation = binaryOperation.RightOperand;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Checks if this is an expression `expr.Length` where `expr` is equivalent to
        /// the instance we were calling .Slice off of.
        /// </summary>
        private bool IsInstanceLengthCheck(IPropertySymbol lengthLikeProperty, IOperation instance, IOperation operation)
            => operation is IPropertyReferenceOperation propertyRef &&
               lengthLikeProperty.Equals(propertyRef.Property) &&
               propertyRef.Instance != null &&
               CSharpSyntaxFactsService.Instance.AreEquivalent(instance.Syntax, propertyRef.Instance.Syntax);
    }
}
