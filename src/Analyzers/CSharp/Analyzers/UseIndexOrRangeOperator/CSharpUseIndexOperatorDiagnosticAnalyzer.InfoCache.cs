// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    using static Helpers;

    internal partial class CSharpUseIndexOperatorDiagnosticAnalyzer
    {
        /// <summary>
        /// Helper type to cache information about types while analyzing the compilation.
        /// </summary>
        private class InfoCache
        {
            /// <summary>
            /// The <see cref="T:System.Index"/> type.  Needed so that we only fixup code if we see the type
            /// we're using has an indexer that takes an <see cref="T:System.Index"/>.
            /// </summary>
            [SuppressMessage("Documentation", "CA1200:Avoid using cref tags with a prefix", Justification = "Required to avoid ambiguous reference warnings.")]
            public readonly INamedTypeSymbol IndexType;

            public readonly INamedTypeSymbol? ExpressionOfTType;

            /// <summary>
            /// Mapping from a method like <c>MyType.Get(int)</c> to the <c>Length</c>/<c>Count</c> property for
            /// <c>MyType</c> as well as the optional <c>MyType.Get(System.Index)</c> member if it exists.
            /// </summary>
            private readonly ConcurrentDictionary<IMethodSymbol, MemberInfo> _methodToMemberInfo = new();

            private InfoCache(INamedTypeSymbol indexType, INamedTypeSymbol? expressionOfTType)
            {
                IndexType = indexType;
                ExpressionOfTType = expressionOfTType;
            }

            public static bool TryCreate(Compilation compilation, [NotNullWhen(true)] out InfoCache? infoCache)
            {
                var indexType = compilation.GetBestTypeByMetadataName(typeof(Index).FullName!);
                if (indexType == null || !indexType.IsAccessibleWithin(compilation.Assembly))
                {
                    infoCache = null;
                    return false;
                }

                infoCache = new InfoCache(indexType, compilation.ExpressionOfTType());
                return true;
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
                    return default;

                if (method.MethodKind == MethodKind.PropertyGet)
                {
                    // this is the getter for an indexer.  i.e. the user is calling something
                    // like s[...].
                    //
                    // These can always be converted to use a System.Index.  Either because the
                    // type itself has a System.Index-based indexer, or because the language just
                    // allows types to implicitly seem like they support this through:
                    //
                    // https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/ranges.md#implicit-index-support
                    return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                }
                else
                {
                    Debug.Assert(method.MethodKind == MethodKind.Ordinary);
                    // it's a method like:   `SomeType MyType.Get(int index)`.  Look 
                    // for an overload like: `SomeType MyType.Get(Range)`
                    var overloadedIndexMethod = GetOverload(method, IndexType);
                    if (overloadedIndexMethod != null)
                        return new MemberInfo(lengthLikeProperty, overloadedIndexMethod);
                }

                // A index-like method that we can't convert.
                return default;
            }
        }
    }
}
