// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Microsoft.DiaSymReader;

namespace Microsoft.Cci
{
    internal sealed class SymWriterMetadataProvider : ISymWriterMetadataProvider
    {
        private readonly MetadataWriter _writer;

        private int _lastTypeDef;
        private string _lastTypeDefName;
        private string _lastTypeDefNamespace;

        internal SymWriterMetadataProvider(MetadataWriter writer)
        {
            _writer = writer;
        }

        // typeDefinitionToken is token returned by GetMethodProps or GetNestedClassProps
        public bool TryGetTypeDefinitionInfo(int typeDefinitionToken, out string namespaceName, out string typeName, out TypeAttributes attributes)
        {
            if (typeDefinitionToken == 0)
            {
                namespaceName = null;
                typeName = null;
                attributes = 0;
                return false;
            }

            // The typeDef name should be fully qualified 
            ITypeDefinition t = _writer.GetTypeDefinition(typeDefinitionToken);
            if (_lastTypeDef == typeDefinitionToken)
            {
                typeName = _lastTypeDefName;
                namespaceName = _lastTypeDefNamespace;
            }
            else
            {
                int generation = (t is INamedTypeDefinition namedType) ? _writer.Module.GetTypeDefinitionGeneration(namedType) : 0;
                typeName = MetadataWriter.GetMetadataName((INamedTypeReference)t, generation);

                INamespaceTypeDefinition namespaceTypeDef;
                if ((namespaceTypeDef = t.AsNamespaceTypeDefinition(_writer.Context)) != null)
                {
                    namespaceName = namespaceTypeDef.NamespaceName;
                }
                else
                {
                    namespaceName = null;
                }

                _lastTypeDef = typeDefinitionToken;
                _lastTypeDefName = typeName;
                _lastTypeDefNamespace = namespaceName;
            }

            attributes = _writer.GetTypeAttributes(t.GetResolvedType(_writer.Context));
            return true;
        }

        // methodDefinitionToken is the token passed to OpenMethod. The token is remembered until the corresponding CloseMethod, which passes it to TryGetMethodInfo.
        public bool TryGetMethodInfo(int methodDefinitionToken, out string methodName, out int declaringTypeToken)
        {
            IMethodDefinition m = _writer.GetMethodDefinition(methodDefinitionToken);
            methodName = m.Name;
            declaringTypeToken = MetadataTokens.GetToken(_writer.GetTypeHandle(m.GetContainingType(_writer.Context)));
            return true;
        }

        public bool TryGetEnclosingType(int nestedTypeToken, out int enclosingTypeToken)
        {
            INestedTypeReference nt = _writer.GetNestedTypeReference(nestedTypeToken);
            if (nt == null)
            {
                enclosingTypeToken = 0;
                return false;
            }

            enclosingTypeToken = MetadataTokens.GetToken(_writer.GetTypeHandle(nt.GetContainingType(_writer.Context)));
            return true;
        }
    }
}
