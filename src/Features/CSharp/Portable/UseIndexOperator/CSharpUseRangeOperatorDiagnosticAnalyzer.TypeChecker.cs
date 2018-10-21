// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    using static Helpers;

    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer
    {
        /// <summary>
        /// Helper type to cache information about types while analyzing the compilation.
        /// </summary>
        private class TypeChecker
        {
            /// <summary>
            /// The System.Range type.  Needed so that we only fixup code if we see the type
            /// we're using has an indexer that takes a Range.
            /// </summary>
            private readonly INamedTypeSymbol _rangeType;
            private readonly ConcurrentDictionary<INamedTypeSymbol, MemberInfo> _typeToMemberInfo;

            public TypeChecker(Compilation compilation)
            {
                _rangeType = compilation.GetTypeByMetadataName("System.Range");

                _typeToMemberInfo = new ConcurrentDictionary<INamedTypeSymbol, MemberInfo>();

                // Always allow using System.Range indexers with System.String.  The compiler has
                // hard-coded knowledge on how to use this type, even if there is no this[Range]
                // indexer declared on it directly.
                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                _typeToMemberInfo[stringType] = Initialize(stringType, requireIndexer: false);
            }

            private IMethodSymbol GetSliceLikeMethod(INamedTypeSymbol namedType)
                => namedType.GetMembers()
                            .OfType<IMethodSymbol>()
                            .Where(m => IsSliceLikeMethod(m))
                            .FirstOrDefault();

            public bool TryGetMemberInfo(INamedTypeSymbol namedType, out MemberInfo memberInfo)
            {
                memberInfo = _typeToMemberInfo.GetOrAdd(namedType, n => Initialize(n, requireIndexer: true));
                return memberInfo.SliceLikeMethod != null;
            }

            private MemberInfo Initialize(INamedTypeSymbol namedType, bool requireIndexer)
            {
                // Check that the type has an int32 'Length' or 'Count' property. If not, we don't
                // consider it something indexable.
                var lengthLikeProperty = GetLengthOrCountProperty(namedType);
                if (lengthLikeProperty == null)
                {
                    return default;
                }

                // Look for something that appears to be a Slice method.  If we can't find one, then
                // this definitely isn't a type we can update code to use an indexer for.
                var sliceLikeMethod = GetSliceLikeMethod(namedType);
                if (sliceLikeMethod == null)
                {
                    return default;
                }

                // if we require an indexer, make sure this type has a this[Range] indexer. If not,
                // return 'default' so we'll consider this named-type non-viable.
                if (requireIndexer)
                {
                    var indexer = GetIndexer(namedType, _rangeType);
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

                return new MemberInfo(lengthLikeProperty, sliceLikeMethod);
            }
        }
    }
}
