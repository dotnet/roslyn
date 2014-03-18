// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Represents an assembly reference with an alias (i.e. an extern alias in C#).
    /// </summary>
    internal class ExternNamespace : IExternNamespace
    {
        private readonly string namespaceAlias;
        private readonly string assemblyName;

        internal ExternNamespace(string namespaceAlias, string assemblyName)
        {
            this.namespaceAlias = namespaceAlias;
            this.assemblyName = assemblyName;
        }

        public string NamespaceAlias { get { return namespaceAlias; } }

        public string AssemblyName { get { return assemblyName; } }
    }
}