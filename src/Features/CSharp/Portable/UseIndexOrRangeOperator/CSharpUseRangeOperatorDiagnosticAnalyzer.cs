// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using static Helpers;

    /// <summary>
    /// Analyzer that looks for several variants of code like `s.Slice(start, end - start)` and
    /// offers to update to `s[start..end]` or `s.Slice(start..end)`.  In order to convert to the
    /// indexer, the type being called on needs a slice-like method that takes two ints, and returns
    /// an instance of the same type. It also needs a Length/Count property, as well as an indexer
    /// that takes a System.Range instance.  In order to convert between methods, there need to be
    /// two overloads that are equivalent except that one takes two ints, and the other takes a
    /// System.Range.
    ///
    /// It is assumed that if the type follows this shape that it is well behaved and that this
    /// transformation will preserve semantics.  If this assumption is not good in practice, we
    /// could always limit the feature to only work on a whitelist of known safe types.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        // public const string UseIndexer = nameof(UseIndexer);
        public const string ComputedRange = nameof(ComputedRange);
        public const string ConstantRange = nameof(ConstantRange);

        public CSharpUseRangeOperatorDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseRangeOperatorDiagnosticId,
                   CSharpCodeStyleOptions.PreferRangeOperator,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_range_operator), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources._0_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // We're going to be checking every invocation in the compilation. Cache information
                // we compute in this object so we don't have to continually recompute it.
                var infoCache = new InfoCache(compilationContext.Compilation);

                // The System.Range type is always required to offer this fix.
                if (infoCache.RangeType != null)
                {
                    compilationContext.RegisterOperationAction(
                        c => AnalyzeInvocation(c, infoCache),
                        OperationKind.Invocation);
                }
            });
        }

        private void AnalyzeInvocation(
            OperationAnalysisContext context, InfoCache infoCache)
        {
            var resultOpt = AnalyzeInvocation(
                (IInvocationOperation)context.Operation, infoCache, context.Options, context.CancellationToken);

            if (resultOpt == null)
            {
                return;
            }

            context.ReportDiagnostic(CreateDiagnostic(resultOpt.Value));
        }

        public static Result? AnalyzeInvocation(
            IInvocationOperation invocation, InfoCache infoCache,
            AnalyzerOptions analyzerOptionsOpt, CancellationToken cancellationToken)
        {
            // Validate we're on a piece of syntax we expect.  While not necessary for analysis, we
            // want to make sure we're on something the fixer will know how to actually fix.
            var invocationSyntax = invocation.Syntax as InvocationExpressionSyntax;
            if (invocationSyntax is null ||
                invocationSyntax.ArgumentList is null)
            {
                return default;
            }

            CodeStyleOption<bool> option = null;
            if (analyzerOptionsOpt != null)
            {
                // Check if we're at least on C# 8, and that the user wants these operators.
                var syntaxTree = invocationSyntax.SyntaxTree;
                var parseOptions = (CSharpParseOptions)syntaxTree.Options;
                if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
                {
                    return default;
                }

                var optionSet = analyzerOptionsOpt.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
                if (optionSet is null)
                {
                    return default;
                }

                option = optionSet.GetOption(CSharpCodeStyleOptions.PreferRangeOperator);
                if (!option.Value)
                {
                    return default;
                }
            }

            // look for `s.Slice(e1, end - e2)`
            if (invocation.Instance is null ||
                invocation.Arguments.Length != 2)
            {
                return default;
            }

            // See if the call is to something slice-like.
            var targetMethod = invocation.TargetMethod;

            // Second arg needs to be a subtraction for: `end - e2`.  Once we've seen that we have
            // that, try to see if we're calling into some sort of Slice method with a matching
            // indexer or overload
            if (!IsSubtraction(invocation.Arguments[1].Value, out var subtraction) ||
                !infoCache.TryGetMemberInfo(targetMethod, out var memberInfo))
            {
                return default;
            }

            // See if we have: (start, end - start).  Specifically where the start operation it the
            // same as the right side of the subtraction.
            var startOperation = invocation.Arguments[0].Value;

            if (CSharpSyntaxFactsService.Instance.AreEquivalent(startOperation.Syntax, subtraction.RightOperand.Syntax))
            {
                return new Result(
                    ResultKind.Computed, option,
                    invocation, invocationSyntax,
                    targetMethod, memberInfo,
                    startOperation, subtraction.LeftOperand);
            }

            // See if we have: (constant1, s.Length - constant2).  The constants don't have to be
            // the same value.  This will convert over to s[constant1..(constant - constant1)]
            if (IsConstantInt32(startOperation) &&
                IsConstantInt32(subtraction.RightOperand) &&
                IsInstanceLengthCheck(memberInfo.LengthLikeProperty, invocation.Instance, subtraction.LeftOperand))
            {
                return new Result(
                    ResultKind.Constant, option,
                    invocation, invocationSyntax,
                    targetMethod, memberInfo,
                    startOperation, subtraction.RightOperand);
            }

            return default;
        }

        private Diagnostic CreateDiagnostic(Result result)
        {
            // Keep track of the invocation node
            var invocation = result.Invocation;
            var additionalLocations = ImmutableArray.Create(
                invocation.GetLocation());

            // Mark the span under the two arguments to .Slice(..., ...) as what we will be
            // updating.
            var arguments = invocation.ArgumentList.Arguments;
            var location = Location.Create(invocation.SyntaxTree,
                TextSpan.FromBounds(arguments.First().SpanStart, arguments.Last().Span.End));

            return DiagnosticHelper.Create(
                Descriptor,
                location,
                result.Option.Notification.Severity,
                additionalLocations,
                ImmutableDictionary<string, string>.Empty,
                result.SliceLikeMethod.Name);
        }

        private static bool IsConstantInt32(IOperation operation)
            => operation.ConstantValue.HasValue && operation.ConstantValue.Value is int;
    }
}
