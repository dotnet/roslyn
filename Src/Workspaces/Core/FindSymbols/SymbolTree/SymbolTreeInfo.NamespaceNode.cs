using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        public sealed class NamespaceNode
        {
            public string Name { get; private set; }
            internal List<NamespaceNode> Namespaces { get; private set; }
            internal List<TypeNode> Types { get; private set; }

            internal NamespaceNode(string name, List<NamespaceNode> namespaces, List<TypeNode> types)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                this.Name = name;
                this.Namespaces = namespaces;
                this.Types = types;
            }
        }
    }
}