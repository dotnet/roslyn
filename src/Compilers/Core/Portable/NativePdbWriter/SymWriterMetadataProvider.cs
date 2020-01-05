// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                typeName = MetadataWriter.GetMangledName((INamedTypeReference)t);

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
