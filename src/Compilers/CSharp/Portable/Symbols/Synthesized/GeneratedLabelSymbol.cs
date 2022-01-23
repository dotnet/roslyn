// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class GeneratedLabelSymbol : LabelSymbol
    {
        private readonly string _name;

        public GeneratedLabelSymbol(string name)
        {
            _name = LabelName(name);
#if DEBUG
            NameNoSequence = $"<{name}>";
#endif
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

#if DEBUG
        internal string NameNoSequence { get; }

        private static int s_sequence = 1;
#endif
        private static string LabelName(string name)
        {
#if DEBUG
            int seq = System.Threading.Interlocked.Add(ref s_sequence, 1);
            return "<" + name + "-" + (seq & 0xffff) + ">";
#else
            return name;
#endif
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }
    }
}
