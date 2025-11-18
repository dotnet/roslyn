// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp;

internal partial class Binder
{
    /// <summary>
    /// This type collects different kinds of results from operator scenarios and provides a unified way to report diagnostics.
    /// It collects the first non-empty result for extensions and non-extensions separately.
    /// This follows a similar logic to ResolveMethodGroupInternal and OverloadResolutionResult.ReportDiagnostics
    /// </summary>
    private struct OperatorResolutionForReporting
    {
        private object? _nonExtensionResult;
        private object? _extensionResult;

        [Conditional("DEBUG")]
        private readonly void AssertInvariant()
        {
            Debug.Assert(_nonExtensionResult is null or OverloadResolutionResult<MethodSymbol> or BinaryOperatorOverloadResolutionResult or UnaryOperatorOverloadResolutionResult);
            Debug.Assert(_extensionResult is null or OverloadResolutionResult<MethodSymbol> or BinaryOperatorOverloadResolutionResult or UnaryOperatorOverloadResolutionResult);
        }

        /// <returns>Returns true if the result was set and <see cref="OperatorResolutionForReporting"/> took ownership of the result.</returns>
        private bool SaveResult(object result, ref object? savedResult)
        {
            if (savedResult is null)
            {
                savedResult = result;
                AssertInvariant();
                return true;
            }

            return false;
        }

        /// <returns>Returns true if the result was set and <see cref="OperatorResolutionForReporting"/> took ownership of the result.</returns>
        public bool SaveResult(OverloadResolutionResult<MethodSymbol> result, bool isExtension)
        {
            if (result.ResultsBuilder.IsEmpty)
            {
                return false;
            }

            return SaveResult(result, ref isExtension ? ref _extensionResult : ref _nonExtensionResult);
        }

        /// <returns>Returns true if the result was set and <see cref="OperatorResolutionForReporting"/> took ownership of the result.</returns>
        public bool SaveResult(BinaryOperatorOverloadResolutionResult result, bool isExtension)
        {
            if (result.Results.IsEmpty)
            {
                return false;
            }

            return SaveResult(result, ref isExtension ? ref _extensionResult : ref _nonExtensionResult);
        }

        /// <returns>Returns true if the result was set and <see cref="OperatorResolutionForReporting"/> took ownership of the result.</returns>
        public bool SaveResult(UnaryOperatorOverloadResolutionResult result, bool isExtension)
        {
            if (result.Results.IsEmpty)
            {
                return false;
            }

            return SaveResult(result, ref isExtension ? ref _extensionResult : ref _nonExtensionResult);
        }

        /// <summary>
        /// Follows a very simplified version of OverloadResolutionResult.ReportDiagnostics which can be expanded in the future if needed.
        /// </summary>
        internal readonly bool TryReportDiagnostics(SyntaxNode node, Binder binder, object leftDisplay, object? rightDisplay, BindingDiagnosticBag diagnostics)
        {
            object? resultToUse = pickResultToUse(_nonExtensionResult, _extensionResult);
            if (resultToUse is null)
            {
                return false;
            }

            var results = ArrayBuilder<(MethodSymbol?, OperatorAnalysisResultKind)>.GetInstance();
            populateResults(results, resultToUse);

            bool reported = tryReportDiagnostics(node, binder, results, leftDisplay, rightDisplay, diagnostics);
            results.Free();

            return reported;

            static bool tryReportDiagnostics(
                SyntaxNode node,
                Binder binder,
                ArrayBuilder<(MethodSymbol? member, OperatorAnalysisResultKind resultKind)> results,
                object leftDisplay,
                object? rightDisplay,
                BindingDiagnosticBag diagnostics)
            {
                assertNone(results, OperatorAnalysisResultKind.Undefined);

                if (hadAmbiguousBestMethods(results, node, binder, diagnostics))
                {
                    return true;
                }

                if (results.Any(m => m.resultKind == OperatorAnalysisResultKind.Applicable))
                {
                    return false;
                }

                assertNone(results, OperatorAnalysisResultKind.Applicable);

                if (results.Any(m => m.resultKind == OperatorAnalysisResultKind.Worse))
                {
                    return false;
                }

                assertNone(results, OperatorAnalysisResultKind.Worse);

                Debug.Assert(results.All(r => r.resultKind == OperatorAnalysisResultKind.Inapplicable));

                // There is much room to improve diagnostics on inapplicable candidates, but for now we just report the candidate if there is a single one.
                if (results is [{ member: { } inapplicableMember }])
                {
                    var toReport = nodeToReport(node);
                    if (rightDisplay is null)
                    {
                        // error: Operator cannot be applied to operand of type '{0}'. The closest inapplicable candidate is '{1}'
                        Error(diagnostics, ErrorCode.ERR_SingleInapplicableUnaryOperator, toReport, leftDisplay, inapplicableMember);
                    }
                    else
                    {
                        // error: Operator cannot be applied to operands of type '{0}' and '{1}'. The closest inapplicable candidate is '{2}'
                        Error(diagnostics, ErrorCode.ERR_SingleInapplicableBinaryOperator, toReport, leftDisplay, rightDisplay, inapplicableMember);
                    }

                    return true;
                }

                return false;
            }

            static object? pickResultToUse(object? nonExtensionResult, object? extensionResult)
            {
                if (nonExtensionResult is null)
                {
                    return extensionResult;
                }

                if (extensionResult is null)
                {
                    return nonExtensionResult;
                }

                bool useNonExtension = getBestKind(nonExtensionResult) >= getBestKind(extensionResult);
                return useNonExtension ? nonExtensionResult : extensionResult;
            }

            static OperatorAnalysisResultKind getBestKind(object result)
            {
                OperatorAnalysisResultKind bestKind = OperatorAnalysisResultKind.Undefined;

                switch (result)
                {
                    case OverloadResolutionResult<MethodSymbol> r1:
                        foreach (var res in r1.ResultsBuilder)
                        {
                            var kind = mapKind(res.Result.Kind);
                            if (kind > bestKind)
                            {
                                bestKind = kind;
                            }
                        }
                        break;

                    case BinaryOperatorOverloadResolutionResult r2:
                        foreach (var res in r2.Results)
                        {
                            if (res.Signature.Method is null)
                            {
                                // Skip built-in operators
                                continue;
                            }

                            if (res.Kind > bestKind)
                            {
                                bestKind = res.Kind;
                            }
                        }
                        break;

                    case UnaryOperatorOverloadResolutionResult r3:
                        foreach (var res in r3.Results)
                        {
                            if (res.Signature.Method is null)
                            {
                                // Skip built-in operators
                                continue;
                            }

                            if (res.Kind > bestKind)
                            {
                                bestKind = res.Kind;
                            }
                        }
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(result);
                }

                return bestKind;
            }

            static bool hadAmbiguousBestMethods(ArrayBuilder<(MethodSymbol?, OperatorAnalysisResultKind)> results, SyntaxNode node, Binder binder, BindingDiagnosticBag diagnostics)
            {
                if (!tryGetTwoBest(results, out var first, out var second))
                {
                    return false;
                }

                Error(diagnostics, ErrorCode.ERR_AmbigOperator, nodeToReport(node), first, second);
                return true;
            }

            static SyntaxNodeOrToken nodeToReport(SyntaxNode node)
            {
                return node switch
                {
                    AssignmentExpressionSyntax assignment => assignment.OperatorToken,
                    BinaryExpressionSyntax binary => binary.OperatorToken,
                    PrefixUnaryExpressionSyntax prefix => prefix.OperatorToken,
                    PostfixUnaryExpressionSyntax postfix => postfix.OperatorToken,
                    _ => node
                };
            }

            [Conditional("DEBUG")]
            static void assertNone(ArrayBuilder<(MethodSymbol? member, OperatorAnalysisResultKind resultKind)> results, OperatorAnalysisResultKind kind)
            {
                Debug.Assert(results.All(r => r.resultKind != kind));
            }

            static bool tryGetTwoBest(ArrayBuilder<(MethodSymbol?, OperatorAnalysisResultKind)> results, [NotNullWhen(true)] out MethodSymbol? first, [NotNullWhen(true)] out MethodSymbol? second)
            {
                first = null;
                second = null;
                bool foundFirst = false;

                foreach (var (member, resultKind) in results)
                {
                    if (member is null)
                    {
                        continue;
                    }

                    if (resultKind == OperatorAnalysisResultKind.Applicable)
                    {
                        if (!foundFirst)
                        {
                            first = member;
                            foundFirst = true;
                        }
                        else
                        {
                            Debug.Assert(first is not null);
                            second = member;
                            return true;
                        }
                    }
                }

                return false;
            }

            static void populateResults(ArrayBuilder<(MethodSymbol?, OperatorAnalysisResultKind)> results, object? result)
            {
                switch (result)
                {
                    case OverloadResolutionResult<MethodSymbol> result1:
                        foreach (var res in result1.ResultsBuilder)
                        {
                            OperatorAnalysisResultKind kind = mapKind(res.Result.Kind);

                            results.Add((res.Member, kind));
                        }
                        break;

                    case BinaryOperatorOverloadResolutionResult result2:
                        foreach (var res in result2.Results)
                        {
                            results.Add((res.Signature.Method, res.Kind));
                        }
                        break;

                    case UnaryOperatorOverloadResolutionResult result3:
                        foreach (var res in result3.Results)
                        {
                            results.Add((res.Signature.Method, res.Kind));
                        }
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(result);
                }
            }

            static OperatorAnalysisResultKind mapKind(MemberResolutionKind kind)
            {
                return kind switch
                {
                    MemberResolutionKind.ApplicableInExpandedForm => OperatorAnalysisResultKind.Applicable,
                    MemberResolutionKind.ApplicableInNormalForm => OperatorAnalysisResultKind.Applicable,
                    MemberResolutionKind.Worse => OperatorAnalysisResultKind.Worse,
                    MemberResolutionKind.Worst => OperatorAnalysisResultKind.Worse,
                    _ => OperatorAnalysisResultKind.Inapplicable,
                };
            }
        }

        internal void Free()
        {
            free(ref _nonExtensionResult);
            free(ref _extensionResult);

            static void free(ref object? result)
            {
                switch (result)
                {
                    case null:
                        return;
                    case OverloadResolutionResult<MethodSymbol> result1:
                        result1.Free();
                        break;
                    case BinaryOperatorOverloadResolutionResult result2:
                        result2.Free();
                        break;
                    case UnaryOperatorOverloadResolutionResult result3:
                        result3.Free();
                        break;
                }

                result = null;
            }
        }
    }
}
