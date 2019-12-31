// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using Microsoft.CodeAnalysis.Shared.Extensions;
    using static Helpers;

    internal partial class CSharpUseIndexOperatorDiagnosticAnalyzer
    {
        /// <summary>
        /// Helper type to cache information about types while analyzing the compilation.
        /// </summary>
        private class InfoCache
        {
            /// <summary>
            /// The System.Index type.  Needed so that we only fixup code if we see the type
            /// we're using has an indexer that takes an Index.
            /// </summary>
            public readonly INamedTypeSymbol IndexType;

            /// <summary>
            /// Mapping from a method like 'MyType.Get(int)' to the Length/Count property for
            /// 'MyType' as well as the optional 'MyType.Get(System.Index)' member if it exists.
            /// </summary>
            private readonly ConcurrentDictionary<IMethodSymbol, MemberInfo> _methodToMemberInfo =
                new ConcurrentDictionary<IMethodSymbol, MemberInfo>();

            public InfoCache(Compilation compilation)
            {
                IndexType = compilation.GetTypeByMetadataName("System.Index");
            }

            public bool TryGetMemberInfo(IMethodSymbol methodSymbol, out MemberInfo memberInfo)
            {
                memberInfo = default;

                if (IsIntIndexingMethod(methodSymbol))
                {
                    memberInfo = _methodToMemberInfo.GetOrAdd(methodSymbol, m => ComputeMemberInfo(m));
                }

                return memberInfo.LengthLikeProperty != null;
            }

            private MemberInfo ComputeMemberInfo(IMethodSymbol method)
            {
                Debug.Assert(IsIntIndexingMethod(method));

                // Check that the type has an int32 'Length' or 'Count' property. If not, we don't
                // consider it something indexable.
                var containingType = method.ContainingType;
                var lengthLikeProperty = TryGetLengthOrCountProperty(containingType);
                if (lengthLikeProperty == null)
                {
                    return default;
                }

                if (method.MethodKind == MethodKind.PropertyGet)
                {
                    // this is the getter for an indexer.  i.e. the user is calling something
                    // like s[...].
                    //
                    // These can always be converted to use a System.Index.  Either because the
                    // type itself has a System.Index-based indexer, or because the language just
                    // allows types to implicitly seem like they support this through:
                    //
                    // https://github.com/dotnet/csharplang/blob/master/proposals/csharp-8.0/ranges.md#implicit-index-support
                    return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                }
                else
                {
                    Debug.Assert(method.MethodKind == MethodKind.Ordinary);
                    // it's a method like:   `SomeType MyType.Get(int index)`.  Look 
                    // for an overload like: `SomeType MyType.Get(Range)`
                    var overloadedIndexMethod = GetOverload(method, IndexType);
                    if (overloadedIndexMethod != null)
                    {
                        return new MemberInfo(lengthLikeProperty, overloadedIndexMethod);
                    }
                }

                // A index-like method that we can't convert.
                return default;
            }
        }
    }
}
