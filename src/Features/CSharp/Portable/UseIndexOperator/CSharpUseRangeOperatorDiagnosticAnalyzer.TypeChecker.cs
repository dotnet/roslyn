// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    using static Helpers;

    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer
    {
        private class TypeChecker
        {
            private readonly INamedTypeSymbol _rangeType;
            private readonly ConcurrentDictionary<INamedTypeSymbol, MemberInfo> _typeToMemberInfo;

            public TypeChecker(Compilation compilation)
            {
                _rangeType = compilation.GetTypeByMetadataName("System.Range");

                _typeToMemberInfo = new ConcurrentDictionary<INamedTypeSymbol, MemberInfo>();

                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                _typeToMemberInfo[stringType] = Initialize(stringType, requireIndexer: false);
            }

            private IMethodSymbol GetSliceLikeMethod(INamedTypeSymbol namedType)
            {
                return namedType.GetMembers()
                                .OfType<IMethodSymbol>()
                                .Where(m => IsSliceLikeMethod(m))
                                .FirstOrDefault();
            }

            public bool TryGetMemberInfo(INamedTypeSymbol namedType, out MemberInfo memberInfo)
            {
                memberInfo = _typeToMemberInfo.GetOrAdd(namedType, n => Initialize(n, requireIndexer: true));
                return memberInfo.SliceLikeMethod != null;
            }

            private MemberInfo Initialize(INamedTypeSymbol namedType, bool requireIndexer)
            {
                var lengthOrCountProp = GetLengthOrCountProperty(namedType);
                if (lengthOrCountProp == null)
                {
                    return default;
                }

                var sliceLikeMethod = GetSliceLikeMethod(namedType);
                if (sliceLikeMethod == null)
                {
                    return default;
                }

                if (requireIndexer)
                {
                    var indexer = GetIndexer(namedType, _rangeType);
                    if (indexer == null)
                    {
                        return default;
                    }

                    // The "this[Range]" indexer has to return the same type as the Slice method.
                    if (!indexer.Type.Equals(sliceLikeMethod.ReturnType))
                    {
                        return default;
                    }
                }

                return new MemberInfo(lengthOrCountProp, sliceLikeMethod);
            }
        }
    }
}
