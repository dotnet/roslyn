// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Diagnostics;
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
            /// The System.Range type.  Needed so that we only fixup code if we see the type
            /// we're using has an indexer that takes a Range.
            /// </summary>
            public readonly INamedTypeSymbol RangeType;
            private readonly ConcurrentDictionary<IMethodSymbol, MemberInfo> _methodToMemberInfo;

            public InfoCache(Compilation compilation)
            {
                RangeType = compilation.GetTypeByMetadataName("System.Range");

                _methodToMemberInfo = new ConcurrentDictionary<IMethodSymbol, MemberInfo>();

                // Always allow using System.Range indexers with System.String.Substring.  The
                // compiler has hard-coded knowledge on how to use this type, even if there is no
                // this[Range] indexer declared on it directly.
                //
                // Ensure that we can actually get the 'string' type. We may fail if there is no
                // proper mscorlib reference (for example, while a project is loading).
                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                if (!stringType.IsErrorType())
                {
                    var substringMethod = stringType.GetMembers(nameof(string.Substring))
                                                    .OfType<IMethodSymbol>()
                                                    .FirstOrDefault(m => IsSliceLikeMethod(m));

                    _methodToMemberInfo[substringMethod] = ComputeMemberInfo(substringMethod, requireRangeMember: false);
                }
            }

            private IMethodSymbol GetSliceLikeMethod(INamedTypeSymbol namedType)
                => namedType.GetMembers()
                            .OfType<IMethodSymbol>()
                            .Where(m => IsSliceLikeMethod(m))
                            .FirstOrDefault();

            public bool TryGetMemberInfo(IMethodSymbol method, out MemberInfo memberInfo)
            {
                if (!IsSliceLikeMethod(method))
                {
                    memberInfo = default;
                    return false;
                }

                memberInfo = _methodToMemberInfo.GetOrAdd(method, m => ComputeMemberInfo(m, requireRangeMember: true));
                return memberInfo.LengthLikeProperty != null;
            }

            private MemberInfo ComputeMemberInfo(IMethodSymbol sliceLikeMethod, bool requireRangeMember)
            {
                Debug.Assert(IsSliceLikeMethod(sliceLikeMethod));

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
                // an Range-taking overload.
                if (sliceLikeMethod.ReturnType.Equals(containingType))
                {
                    // it's a method like:  MyType MyType.Slice(int start, int length).  Look for an
                    // indexer like  `MyType MyType.this[Range range]`. If we can't find one return
                    // 'default' so we'll consider this named-type non-viable.
                    var indexer = GetIndexer(containingType, RangeType, containingType);
                    if (indexer != null)
                    {
                        return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                    }
                }

                // it's a method like:   `SomeType MyType.Slice(int start, int length)`.  Look 
                // for an overload like: `SomeType MyType.Slice(Range)`
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
