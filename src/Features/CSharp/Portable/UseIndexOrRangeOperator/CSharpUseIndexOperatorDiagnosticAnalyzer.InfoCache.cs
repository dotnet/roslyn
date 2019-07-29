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
            private readonly ConcurrentDictionary<IMethodSymbol, MemberInfo> _methodToMemberInfo;

            public InfoCache(Compilation compilation)
            {
                IndexType = compilation.GetTypeByMetadataName("System.Index");

                _methodToMemberInfo = new ConcurrentDictionary<IMethodSymbol, MemberInfo>();

                // Always allow using System.Index indexers with System.String.  The compiler has
                // hard-coded knowledge on how to use this type, even if there is no this[Index]
                // indexer declared on it directly.
                //
                // Ensure that we can actually get the 'string' type. We may fail if there is no
                // proper mscorlib reference (for example, while a project is loading).
                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                if (!stringType.IsErrorType())
                {
                    var indexer = GetIndexer(stringType,
                        compilation.GetSpecialType(SpecialType.System_Int32),
                        compilation.GetSpecialType(SpecialType.System_Char));

                    _methodToMemberInfo[indexer.GetMethod] = ComputeMemberInfo(indexer.GetMethod, requireIndexMember: false);
                }
            }

            public bool TryGetMemberInfo(IMethodSymbol methodSymbol, out MemberInfo memberInfo)
            {
                memberInfo = default;

                if (IsIntIndexingMethod(methodSymbol))
                {
                    memberInfo = _methodToMemberInfo.GetOrAdd(methodSymbol, m => ComputeMemberInfo(m, requireIndexMember: true));
                }

                return memberInfo.LengthLikeProperty != null;
            }

            private MemberInfo ComputeMemberInfo(IMethodSymbol method, bool requireIndexMember)
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

                if (!requireIndexMember)
                {
                    return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                }

                if (method.MethodKind == MethodKind.PropertyGet)
                {
                    // this is the getter for an indexer.  i.e. the user is calling something
                    // like s[...].  We need to see if there's an indexer that takes a System.Index
                    // value.
                    var indexer = GetIndexer(containingType, IndexType, method.ReturnType);
                    if (indexer != null)
                    {
                        // Type had a matching indexer.  We can convert calls to the int-indexer to
                        // calls to this System.Index-indexer.
                        return new MemberInfo(lengthLikeProperty, overloadedMethodOpt: null);
                    }
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
