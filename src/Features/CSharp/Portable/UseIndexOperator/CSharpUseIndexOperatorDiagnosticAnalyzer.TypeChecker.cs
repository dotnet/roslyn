// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Concurrent;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    internal partial class CSharpUseIndexOperatorDiagnosticAnalyzer
    {
        private class TypeChecker
        {
            private readonly INamedTypeSymbol _indexType;
            private readonly ConcurrentDictionary<INamedTypeSymbol, IPropertySymbol> _typeToLengthOrCountProperty;

            public TypeChecker(Compilation compilation)
            {
                _indexType = compilation.GetTypeByMetadataName("System.Index");

                _typeToLengthOrCountProperty = new ConcurrentDictionary<INamedTypeSymbol, IPropertySymbol>();

                var stringType = compilation.GetSpecialType(SpecialType.System_String);
                _typeToLengthOrCountProperty[stringType] = Initialize(stringType, requireIndexer: false);
            }

            public IPropertySymbol GetLengthOrCountProperty(INamedTypeSymbol namedType)
                => _typeToLengthOrCountProperty.GetOrAdd(namedType, n => Initialize(n, requireIndexer: true));

            private IPropertySymbol Initialize(INamedTypeSymbol namedType, bool requireIndexer)
            {
                var lengthOrCountProperty = Helpers.GetLengthOrCountProperty(namedType);
                if (lengthOrCountProperty == null)
                {
                    return null;
                }

                if (requireIndexer)
                {
                    var indexer = Helpers.GetIndexer(namedType, _indexType);
                    if (indexer == null)
                    {
                        return null;
                    }
                }

                return lengthOrCountProperty;
            }
        }
    }
}
