// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal static class InvocationCountAnalysisHelper
    {
        private static readonly ImmutableArray<string> s_immediateExecutedMethods = ImmutableArray.Create(
            "System.Linq.Enumerable.Aggregate",
            "System.Linq.Enumerable.All",
            "System.Linq.Enumerable.Any",
            "System.Linq.Enumerable.Average",
            "System.Linq.Enumerable.Contains",
            "System.Linq.Enumerable.Count",
            "System.Linq.Enumerable.ElementAt",
            "System.Linq.Enumerable.ElementAtOrDefault",
            "System.Linq.Enumerable.First",
            "System.Linq.Enumerable.FirstOrDefault",
            "System.Linq.Enumerable.Last",
            "System.Linq.Enumerable.LastOrDefault",
            "System.Linq.Enumerable.LongCount",
            "System.Linq.Enumerable.Max",
            "System.Linq.Enumerable.Min",
            "System.Linq.Enumerable.SequenceEqual",
            "System.Linq.Enumerable.Single",
            "System.Linq.Enumerable.SingleOrDefault",
            "System.Linq.Enumerable.Sum",
            "System.Linq.Enumerable.ToArray",
            "System.Linq.Enumerable.ToDictionary",
            "System.Linq.Enumerable.ToHashSet",
            "System.Linq.Enumerable.ToList",
            "System.Linq.Enumerable.ToLookup");

        private static readonly SymbolDisplayFormat s_methodFullyQualifiedNameFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

        // public override InvocationCountAbstractValue Merge(InvocationCountAbstractValue value1, InvocationCountAbstractValue value2) =>
        //     (Kind: value1.InvocationTimes, Kind: value2.InvocationTimes) switch
        //     {
        //         (InvocationTimes.Unknown, _) => InvocationCountAbstractValue.Unknown,
        //         (_, InvocationTimes.Unknown) => InvocationCountAbstractValue.Unknown,
        //         (InvocationTimes.Zero, _) => value2,
        //         (_, InvocationTimes.Zero) => value1,
        //         (InvocationTimes.MoreThanOneTime, _) => InvocationCountAbstractValue.MoreThanOneTime,
        //         (_, InvocationTimes.MoreThanOneTime) => InvocationCountAbstractValue.MoreThanOneTime,
        //         (InvocationTimes.OneTime, InvocationTimes.OneTime) => InvocationCountAbstractValue.MoreThanOneTime,
        //         _ => throw new ArgumentException($"Unexpected combinations of {value1} and {value2}"),
        //     };

        public static bool CauseEnumeration(IOperation operation, out IOperation referencedIEnumerableInstance)
        {
            referencedIEnumerableInstance = null;
            // ForEach Loop
            if (operation.Parent.Parent is IForEachLoopOperation && IsReferencingEnumerable(operation))
            {
                return true;
            }

            if (operation is IInvocationOperation invocationOperation)
            {
                var method = invocationOperation.TargetMethod;
                if (method.IsExtensionMethod && s_immediateExecutedMethods.Contains(method.ToDisplayString(s_methodFullyQualifiedNameFormat)))
                {
                    return true;
                }

            }

            return false;
        }

        private static bool IsReferencingEnumerable(IOperation operation)
        {
            return operation switch
            {
                ILocalReferenceOperation localReferenceOperation when localReferenceOperation.Local.Type.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T => true,
                IParameterReferenceOperation parameterReferenceOperation when parameterReferenceOperation.Parameter.Type.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T => true,
                _ => false
            };
        }
    }
}