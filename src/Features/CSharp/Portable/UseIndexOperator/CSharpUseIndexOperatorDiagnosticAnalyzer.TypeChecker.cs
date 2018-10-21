// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    using static Helpers;

    internal partial class CSharpUseIndexOperatorDiagnosticAnalyzer
    {
        /// <summary>
        /// Helper type to cache information about types while analyzing the compilation.
        /// </summary>
        private class TypeChecker
        {
            /// <summary>
            /// The System.Index type.  Needed so that we only fixup code if we see the type
            /// we're using has an indexer that takes an Index.
            /// </summary>
            private readonly INamedTypeSymbol _indexType;
            private readonly ConcurrentDictionary<INamedTypeSymbol, IPropertySymbol> _typeToLengthLikeProperty;

            public TypeChecker(Compilation compilation)
            {
                _indexType = compilation.GetTypeByMetadataName("System.Index");

                _typeToLengthLikeProperty = new ConcurrentDictionary<INamedTypeSymbol, IPropertySymbol>();

                // Always allow using System.Index indexers with System.String.  The compiler has
                // hard-coded knowledge on how to use this type, even if there is no this[Index]
                // indexer declared on it directly.
                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                _typeToLengthLikeProperty[stringType] = Initialize(stringType, requireIndexer: false);
            }

            public IPropertySymbol GetLengthLikeProperty(INamedTypeSymbol namedType)
                => _typeToLengthLikeProperty.GetOrAdd(namedType, n => Initialize(n, requireIndexer: true));

            private IPropertySymbol Initialize(INamedTypeSymbol namedType, bool requireIndexer)
            {
                // Check that the type has an int32 'Length' or 'Count' property. If not, we don't
                // consider it something indexable.
                var lengthLikeProperty = GetLengthOrCountProperty(namedType);
                if (lengthLikeProperty == null)
                {
                    return null;
                }

                // if we require an indexer, make sure this type has a this[Index] indexer. If not,
                // return 'null' so we'll consider this named-type non-viable. Otherwise, return the
                // lengthLikeProp property, marking this type as viable.
                if (requireIndexer)
                {
                    var indexer = GetIndexer(namedType, _indexType);
                    if (indexer == null)
                    {
                        return null;
                    }
                }

                return lengthLikeProperty;
            }
        }
    }
}
