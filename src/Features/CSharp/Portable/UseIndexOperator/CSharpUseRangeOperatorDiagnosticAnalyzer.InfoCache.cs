// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    using System.Diagnostics;
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
            private readonly INamedTypeSymbol _rangeType;
            private readonly ConcurrentDictionary<IMethodSymbol, MemberInfo> _methodToMemberInfo;

            public InfoCache(Compilation compilation)
            {
                _rangeType = compilation.GetTypeByMetadataName("System.Range");

                _methodToMemberInfo = new ConcurrentDictionary<IMethodSymbol, MemberInfo>();

                // Always allow using System.Range indexers with System.String.  The compiler has
                // hard-coded knowledge on how to use this type, even if there is no this[Range]
                // indexer declared on it directly.
                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                var substringMethod = stringType.GetMembers(nameof(string.Substring))
                                                .OfType<IMethodSymbol>()
                                                .FirstOrDefault(m => IsSliceLikeMethod(m));

                _methodToMemberInfo[substringMethod] = ComputeMemberInfo(substringMethod, requireIndexer: false);
            }

            private IMethodSymbol GetSliceLikeMethod(INamedTypeSymbol namedType)
                => namedType.GetMembers()
                            .OfType<IMethodSymbol>()
                            .Where(m => IsSliceLikeMethod(m))
                            .FirstOrDefault();

            public bool TryGetMemberInfo(IMethodSymbol sliceLikeMethod, out MemberInfo memberInfo)
            {
                Debug.Assert(IsSliceLikeMethod(sliceLikeMethod));

                memberInfo = _methodToMemberInfo.GetOrAdd(sliceLikeMethod, m => ComputeMemberInfo(m, requireIndexer: true));
                return memberInfo.LengthLikeProperty != null;
            }

            private MemberInfo ComputeMemberInfo(IMethodSymbol sliceLikeMethod, bool requireIndexer)
            {
                // Check that the type has an int32 'Length' or 'Count' property. If not, we don't
                // consider it something indexable.
                var containingType = sliceLikeMethod.ContainingType;
                var lengthLikeProperty = GetLengthOrCountProperty(containingType);
                if (lengthLikeProperty == null)
                {
                    return default;
                }

                // A Slice method can either be paired with an Range-taking indexer on the type, or
                // an Range-taking overload.

                IMethodSymbol sliceRangeMethodOpt = null;
                if (sliceLikeMethod.ReturnType.Equals(containingType))
                {
                    // it's a method like:  MyType MyType.Slice(int start, int length).  Look for an
                    // indexer like  `MyType MyType.this[Range range]`. If we can't find one return
                    // 'default' so we'll consider this named-type non-viable.
                    if (requireIndexer)
                    {
                        var indexer = GetIndexer(containingType, _rangeType);
                        if (indexer == null)
                        {
                            return default;
                        }

                        // The "this[Range]" indexer has to return the same type as the Slice method.
                        // If not, this type isn't one that matches the pattern we're looking for.
                        if (!indexer.Type.Equals(sliceLikeMethod.ReturnType))
                        {
                            return default;
                        }
                    }
                }
                else
                {
                    // it's a method like:   `SomeType MyType.Slice(int start, int length)`.  Look 
                    // for an overload like: `SomeType MyType.Slice(Range)`
                    sliceRangeMethodOpt =
                        containingType.GetMembers(sliceLikeMethod.Name)
                                      .OfType<IMethodSymbol>()
                                      .Where(m => IsPublicInstance(m) &&
                                                  m.Parameters.Length == 1 &&
                                                  m.Parameters[0].Type.Equals(_rangeType) &&
                                                  m.ReturnType.Equals(sliceLikeMethod.ReturnType))
                                      .FirstOrDefault();
                    if (sliceRangeMethodOpt == null)
                    {
                        return default;
                    }
                }

                return new MemberInfo(lengthLikeProperty, sliceRangeMethodOpt);
            }
        }
    }
}
