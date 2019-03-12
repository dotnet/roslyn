// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public abstract class EmitMetadataTestBase : CSharpTestBase
    {
        internal static XElement DumpTypeInfo(ModuleSymbol moduleSymbol)
        {
            return LoadChildNamespace(moduleSymbol.GlobalNamespace);
        }

        internal static XElement LoadChildNamespace(NamespaceSymbol n)
        {
            XElement elem = new XElement((n.Name.Length == 0 ? "Global" : n.Name));

            var childrenTypes = n.GetTypeMembers().OrderBy((t) => t, new NameAndArityComparer());

            elem.Add(from t in childrenTypes select LoadChildType(t));

            var childrenNS = n.GetMembers().
                                OfType<NamespaceSymbol>().
                                OrderBy(child => child.Name, StringComparer.OrdinalIgnoreCase);

            elem.Add(from c in childrenNS select LoadChildNamespace(c));

            return elem;
        }

        private static XElement LoadChildType(NamedTypeSymbol t)
        {
            XElement elem = new XElement("type");

            elem.Add(new XAttribute("name", t.Name));

            if (t.Arity > 0)
            {
                string typeParams = string.Empty;

                foreach (var param in t.TypeParameters)
                {
                    if (typeParams.Length > 0)
                    {
                        typeParams += ",";
                    }

                    typeParams += param.Name;
                }

                elem.Add(new XAttribute("Of", typeParams));
            }

            if ((object)t.BaseType() != null)
            {
                elem.Add(new XAttribute("base", t.BaseType().ToTestDisplayString()));
            }

            var fields = t.GetMembers().Where(m => m.Kind == SymbolKind.Field).OrderBy(f => f.Name).Cast<FieldSymbol>();

            elem.Add(from f in fields select LoadField(f));

            var childrenTypes = t.GetTypeMembers().OrderBy(c => c, new NameAndArityComparer());

            elem.Add(from c in childrenTypes select LoadChildType(c));

            return elem;
        }

        private static XElement LoadField(FieldSymbol f)
        {
            XElement elem = new XElement("field");

            elem.Add(new XAttribute("name", f.Name));
            elem.Add(new XAttribute("type", f.Type.ToTestDisplayString()));

            return elem;
        }

        #region DeclSecurityTable Validation
        /// <summary>
        /// Validate the contents of the DeclSecurity metadata table.
        /// </summary>
        internal static void ValidateDeclSecurity(ModuleSymbol module, params DeclSecurityEntry[] expectedEntries)
        {
            var metadataReader = module.GetMetadata().MetadataReader;
            var actualEntries = new List<DeclSecurityEntry>(expectedEntries.Length);

            int i = 0;
            foreach (var actualHandle in metadataReader.DeclarativeSecurityAttributes)
            {
                var actual = metadataReader.GetDeclarativeSecurityAttribute(actualHandle);

                var actualPermissionSetBytes = metadataReader.GetBlobBytes(actual.PermissionSet);
                var actualPermissionSet = new string(actualPermissionSetBytes.Select(b => (char)b).ToArray());
                string actualParentName;
                SymbolKind actualParentKind;
                GetAttributeParentNameAndKind(metadataReader, actual.Parent, out actualParentName, out actualParentKind);

                actualEntries.Add(new DeclSecurityEntry()
                {
                    ActionFlags = actual.Action,
                    ParentNameOpt = actualParentName,
                    PermissionSet = actualPermissionSet,
                    ParentKind = actualParentKind
                });

                i++;
            }

            AssertEx.SetEqual(expectedEntries, actualEntries, itemInspector: entry => $@"
{{
    ActionFlags = {entry.ActionFlags},
    ParentNameOpt = {entry.ParentNameOpt},
    PermissionSet = {entry.PermissionSet},
    ParentKind = {entry.ParentKind}
}}");
        }

        private static void GetAttributeParentNameAndKind(MetadataReader metadataReader, EntityHandle token, out string name, out SymbolKind kind)
        {
            switch (token.Kind)
            {
                case HandleKind.AssemblyDefinition:
                    name = null;
                    kind = SymbolKind.Assembly;
                    return;

                case HandleKind.TypeDefinition:
                    name = metadataReader.GetString(metadataReader.GetTypeDefinition((TypeDefinitionHandle)token).Name);
                    kind = SymbolKind.NamedType;
                    return;

                case HandleKind.MethodDefinition:
                    name = metadataReader.GetString(metadataReader.GetMethodDefinition((MethodDefinitionHandle)token).Name);
                    kind = SymbolKind.Method;
                    return;

                default:
                    throw TestExceptionUtilities.UnexpectedValue(token.Kind);
            }
        }

        private static TypeDefinitionHandle GetTokenForType(MetadataReader metadataReader, string typeName)
        {
            Assert.NotNull(typeName);
            Assert.NotEmpty(typeName);

            foreach (var typeDef in metadataReader.TypeDefinitions)
            {
                string name = metadataReader.GetString(metadataReader.GetTypeDefinition(typeDef).Name);

                if (typeName.Equals(name))
                {
                    return typeDef;
                }
            }

            AssertEx.Fail("Unable to find type:" + typeName);
            return default(TypeDefinitionHandle);
        }

        private static MethodDefinitionHandle GetTokenForMethod(MetadataReader metadataReader, string methodName)
        {
            Assert.NotNull(methodName);
            Assert.NotEmpty(methodName);

            foreach (var methodDef in metadataReader.MethodDefinitions)
            {
                string name = metadataReader.GetString(metadataReader.GetMethodDefinition(methodDef).Name);

                if (methodName.Equals(name))
                {
                    return methodDef;
                }
            }

            AssertEx.Fail("Unable to find method:" + methodName);
            return default(MethodDefinitionHandle);
        }

        internal struct DeclSecurityEntry
        {
            public DeclarativeSecurityAction ActionFlags;
            public SymbolKind ParentKind;
            public string ParentNameOpt;
            public string PermissionSet;
        }

        #endregion
    }
}
