using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        public struct SearchResult
        {
            public LinkedList<NamespaceNode> NamespaceNodes { get; private set; }
            public LinkedList<TypeNode> TypeNodes { get; private set; }
            public string MemberName { get; private set; }

            public SearchResult(NamespaceNode namespaceNode = null, TypeNode typeNode = null, string memberName = null)
                : this()
            {
                this.NamespaceNodes = new LinkedList<NamespaceNode>();
                this.TypeNodes = new LinkedList<TypeNode>();

                if (namespaceNode != null)
                {
                    this.NamespaceNodes.AddFirst(namespaceNode);
                }

                if (typeNode != null)
                {
                    this.TypeNodes.AddFirst(typeNode);
                }

                this.MemberName = memberName;
            }
        }
    }
}