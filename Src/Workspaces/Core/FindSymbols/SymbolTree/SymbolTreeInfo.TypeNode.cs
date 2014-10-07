using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class SymbolTreeInfo
    {
        public sealed class TypeNode
        {
            public string Name { get; private set; }
            internal List<TypeNode> Types { get; private set; }
            internal ICollection<string> MemberNames { get; private set; }

            internal TypeNode(string name, List<TypeNode> types, ICollection<string> memberNames)
            {
                if (name == null)
                {
                    throw new ArgumentNullException("name");
                }

                this.Name = name;
                this.Types = types;
                this.MemberNames = memberNames;
            }
        }
    }
}