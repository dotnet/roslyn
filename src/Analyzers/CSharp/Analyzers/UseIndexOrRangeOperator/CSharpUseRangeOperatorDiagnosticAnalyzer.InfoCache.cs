// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using static Helpers;

    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer
    {
        /// <summary>
        /// Helper type to cache information about types while analyzing the compilation.
        /// </summary>
        public class InfoCache
        {
            /// <summary>
            /// The <see cref="T:System.Range"/> type.  Needed so that we only fixup code if we see the type
            /// we're using has an indexer that takes a <see cref="T:System.Range"/>.
            /// </summary>
            [SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "Required to avoid ambiguous reference warnings.")]
            public readonly INamedTypeSymbol RangeType;
            public readonly INamedTypeSymbol? ExpressionOfTType;

            private readonly ConcurrentDictionary<IMethodSymbol, MemberInfo> _methodToMemberInfo = new();

            private InfoCache(INamedTypeSymbol rangeType, INamedTypeSymbol stringType, INamedTypeSymbol? expressionOfTType)
            {
                RangeType = rangeType;
                ExpressionOfTType = expressionOfTType;

                // Always allow using System.Range indexers with System.String.Substring.  The
                // compiler has hard-coded knowledge on how to use this type, even if there is no
                // this[Range] indexer declared on it directly.
                //
                // Ensure that we can actually get the 'string' type. We may fail if there is no
                // proper mscorlib reference (for example, while a project is loading).
                if (!stringType.IsErrorType())
                {
                    var substringMethod = stringType.GetMembers(nameof(string.Substring))
                                                    .OfType<IMethodSymbol>()
                                                    .FirstOrDefault(m => IsTwoArgumentSliceLikeMethod(m));

                    _methodToMemberInfo[substringMethod] = ComputeMemberInfo(substringMethod, requireRangeMember: false);
                }
            }

            public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out InfoCache? infoCache)
            {
                var rangeType = compilation.GetBestTypeByMetadataName(typeof(Range).FullName!);
                if (rangeType == null || !rangeType.IsAccessibleWithin(compilation.Assembly))
                {
                    infoCache = null;
                    return false;
                }

                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                infoCache = new InfoCache(rangeType, stringType, compilation.ExpressionOfTType());
                return true;
            }

            private static IMethodSymbol GetSliceLikeMethod(INamedTypeSymbol namedType)
                => namedType.GetMembers()
                            .OfType<IMethodSymbol>()
                            .Where(m => IsTwoArgumentSliceLikeMethod(m))
                            .FirstOrDefault();

            public bool TryGetMemberInfo(IMethodSymbol method, out MemberInfo memberInfo)
            {
                if (!IsTwoArgumentSliceLikeMethod(method))
                {
                    memberInfo = default;
                    return false;
                }

                memberInfo = _methodToMemberInfo.GetOrAdd(method, m => ComputeMemberInfo(m, requireRangeMember: true));
                return memberInfo.LengthLikeProperty != null;
            }

            public bool TryGetMemberInfoOneArgument(IMethodSymbol method, out MemberInfo memberInfo)
            {
                if (!IsOneArgumentSliceLikeMethod(method))
                {
                    memberInfo = default;
                    return false;
                }

                if (!_methodToMemberInfo.TryGetValue(method, out memberInfo))
                {
                    // Find overload of our method that is a slice-like method with two arguments.
                    // Computing member info for this method will also check that the containing type
                    // has an int32 'Length' or 'Count' property, and has a suitable indexer,
                    // so we don't have to.
                    var overloadWithTwoArguments = method.ContainingType
                        .GetMembers(method.Name)
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(s => IsTwoArgumentSliceLikeMethod(s));
                    if (overloadWithTwoArguments is null)
                    {
                        memberInfo = default;
                        return false;
                    }

                    // Since the search is expensive, we keep both the original one-argument and
                    // two-arguments overload as keys in the cache, pointing to the same
                    // member information object.
                    var newMemberInfo = _methodToMemberInfo.GetOrAdd(overloadWithTwoArguments, _ => ComputeMemberInfo(overloadWithTwoArguments, requireRangeMember: true));
                    _methodToMemberInfo.GetOrAdd(method, _ => newMemberInfo);
                    memberInfo = newMemberInfo;
                }

                return memberInfo.LengthLikeProperty != null;
            }

            private MemberInfo ComputeMemberInfo(IMethodSymbol sliceLikeMethod, bool requireRangeMember)
            {
                Debug.Assert(IsTwoArgumentSliceLikeMethod(sliceLikeMethod));

                // Check that the type has an int32 'Length' or 'Count' property. If not, we don't
                // consider it something indexable.
                var containingType = sliceLikeMethod.ContainingType;
                var lengthLikeProperty = TryGetLengthOrCountProperty(containingType);
                if (lengthLikeProperty == null)
                {
                    return default;
                }

                if (!requireRangeMember)
                {
                    return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                }

                // A Slice method can either be paired with an Range-taking indexer on the type, or
                // an Range-taking overload, or an explicit method called .Slice that takes two ints:
                //
                // https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md#implicit-range-support
                if (sliceLikeMethod.ReturnType.Equals(containingType))
                {
                    // it's a method like:  MyType MyType.Get(int start, int length).  Look for an
                    // indexer like  `MyType MyType.this[Range range]`.
                    var indexer = GetIndexer(containingType, RangeType, containingType);
                    if (indexer != null)
                    {
                        return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                    }

                    // Also, look to see if the type has a `.Slice(int start, int length)` method.
                    // This is also a method the compiler knows to look for when a user writes `x[a..b]`
                    var actualSliceMethod =
                        sliceLikeMethod.ContainingType.GetMembers(nameof(Span<int>.Slice))
                                                      .OfType<IMethodSymbol>()
                                                      .FirstOrDefault(s => IsTwoArgumentSliceLikeMethod(s));
                    if (actualSliceMethod != null)
                    {
                        return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                    }
                }

                // it's a method like:   `SomeType MyType.Get(int start, int length)`.  Look 
                // for an overload like: `SomeType MyType.Get(Range)`
                var overloadedRangeMethod = GetOverload(sliceLikeMethod, RangeType);
                if (overloadedRangeMethod != null)
                {
                    return new MemberInfo(lengthLikeProperty, overloadedRangeMethod);
                }

                // A slice-like method that we can't convert.
                return default;
            }
        }
    }
}
