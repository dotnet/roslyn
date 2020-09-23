// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using static Helpers;

    /// <summary>
    /// <para>Analyzer that looks for several variants of code like <c>s.Slice(start, end - start)</c> and
    /// offers to update to <c>s[start..end]</c> or <c>s.Slice(start..end)</c>.  In order to convert to the
    /// indexer, the type being called on needs a slice-like method that takes two ints, and returns
    /// an instance of the same type. It also needs a <c>Length</c>/<c>Count</c> property, as well as an indexer
    /// that takes a <see cref="T:System.Range"/> instance.  In order to convert between methods, there need to be
    /// two overloads that are equivalent except that one takes two ints, and the other takes a
    /// <see cref="T:System.Range"/>.</para>
    ///
    /// <para>It is assumed that if the type follows this shape that it is well behaved and that this
    /// transformation will preserve semantics.  If this assumption is not good in practice, we
    /// could always limit the feature to only work on an allow list of known safe types.</para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "Required to avoid ambiguous reference warnings.")]
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        // public const string UseIndexer = nameof(UseIndexer);
        public const string ComputedRange = nameof(ComputedRange);
        public const string ConstantRange = nameof(ConstantRange);

        public CSharpUseRangeOperatorDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseRangeOperatorDiagnosticId,
                   CSharpCodeStyleOptions.PreferRangeOperator,
                   LanguageNames.CSharp,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_range_operator), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources._0_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
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
            if (!(invocation.Syntax is InvocationExpressionSyntax invocationSyntax) ||
                invocationSyntax.ArgumentList is null)
            {
                return null;
            }

            CodeStyleOption2<bool> option = null;
            if (analyzerOptionsOpt != null)
            {
                // Check if we're at least on C# 8, and that the user wants these operators.
                var syntaxTree = invocationSyntax.SyntaxTree;
                var parseOptions = (CSharpParseOptions)syntaxTree.Options;
                if (parseOptions.LanguageVersion < LanguageVersion.CSharp8)
                {
                    return null;
                }

                option = analyzerOptionsOpt.GetOption(CSharpCodeStyleOptions.PreferRangeOperator, syntaxTree, cancellationToken);
                if (!option.Value)
                {
                    return null;
                }
            }

            // look for `s.Slice(e1, end - e2)` or `s.Slice(e1)`
            if (invocation.Instance is null)
            {
                return null;
            }

            return invocation.Arguments.Length switch
            {
                1 => AnalyzeOneArgumentInvocation(invocation, infoCache, invocationSyntax, option),
                2 => AnalyzeTwoArgumentInvocation(invocation, infoCache, invocationSyntax, option),
                _ => null,
            };
        }

        private static Result? AnalyzeOneArgumentInvocation(
            IInvocationOperation invocation,
            InfoCache infoCache,
            InvocationExpressionSyntax invocationSyntax,
            CodeStyleOption2<bool> option)
        {
            var targetMethod = invocation.TargetMethod;

            // We are dealing with a call like `.Substring(expr)`.
            // Ensure that there is an overload with signature like `Substring(int start, int length)`
            // and there is a suitable indexer to replace this with `[expr..]`.
            if (!infoCache.TryGetMemberInfoOneArgument(targetMethod, out var memberInfo))
            {
                return null;
            }

            var startOperation = invocation.Arguments[0].Value;
            return new Result(
                ResultKind.Computed,
                option,
                invocation,
                invocationSyntax,
                targetMethod,
                memberInfo,
                op1: startOperation,
                op2: null); // The range will run to the end.
        }

        private static Result? AnalyzeTwoArgumentInvocation(
            IInvocationOperation invocation,
            InfoCache infoCache,
            InvocationExpressionSyntax invocationSyntax,
            CodeStyleOption2<bool> option)
        {
            // See if the call is to something slice-like.
            var targetMethod = invocation.TargetMethod;

            // Second arg needs to be a subtraction for: `end - e2`.  Once we've seen that we have
            // that, try to see if we're calling into some sort of Slice method with a matching
            // indexer or overload
            if (!IsSubtraction(invocation.Arguments[1].Value, out var subtraction) ||
                !infoCache.TryGetMemberInfo(targetMethod, out var memberInfo))
            {
                return null;
            }

            var indexer = GetIndexer(targetMethod.ContainingType, infoCache.RangeType, targetMethod.ContainingType);
            // Need to make sure that if the target method is being written to, that the indexer returns a ref, is a read/write property,
            // or the syntax allows for the slice method to be run
            if (invocation.Syntax.IsLeftSideOfAnyAssignExpression() && indexer != null && IsWriteableIndexer(invocation, indexer))
            {
                return null;
            }

            // See if we have: (start, end - start).  Specifically where the start operation it the
            // same as the right side of the subtraction.
            var startOperation = invocation.Arguments[0].Value;

            if (CSharpSyntaxFacts.Instance.AreEquivalent(startOperation.Syntax, subtraction.RightOperand.Syntax))
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

            return null;
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

        private static bool IsWriteableIndexer(IInvocationOperation invocation, IPropertySymbol indexer)
        {
            var refReturnMismatch = indexer.ReturnsByRef != invocation.TargetMethod.ReturnsByRef;
            var indexerIsReadWrite = indexer.IsWriteableFieldOrProperty();
            return refReturnMismatch && !indexerIsReadWrite;
        }
    }
}
