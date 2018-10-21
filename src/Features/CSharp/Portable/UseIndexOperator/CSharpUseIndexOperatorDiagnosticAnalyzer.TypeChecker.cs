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
                var lengthProp = GetNoArgInt32Property(stringType, nameof(string.Length));

                _typeToLengthOrCountProperty[stringType] = lengthProp;
            }

            public IPropertySymbol GetLengthOrCountProperty(INamedTypeSymbol namedType)
                => _typeToLengthOrCountProperty.GetOrAdd(namedType, n => Initialize(n));

            private IPropertySymbol Initialize(INamedTypeSymbol namedType)
            {
                var lengthOrCountProperty =
                    GetNoArgInt32Property(namedType, nameof(string.Length)) ??
                    GetNoArgInt32Property(namedType, nameof(ICollection.Count));

                if (lengthOrCountProperty == null)
                {
                    return null;
                }

                var indexer =
                    namedType.GetMembers()
                             .OfType<IPropertySymbol>()
                             .Where(p => p.IsIndexer &&
                                         p.Parameters.Length == 1 &&
                                         p.Parameters[0].Type.Equals(_indexType))
                             .FirstOrDefault();

                return indexer != null ? lengthOrCountProperty : null;
            }

            private static IPropertySymbol GetNoArgInt32Property(INamedTypeSymbol type, string name)
            {
                return type.GetMembers(name)
                           .OfType<IPropertySymbol>()
                           .Where(p => !p.IsIndexer && p.Type.SpecialType == SpecialType.System_Int32)
                           .FirstOrDefault();
            }
        }
    }
}
