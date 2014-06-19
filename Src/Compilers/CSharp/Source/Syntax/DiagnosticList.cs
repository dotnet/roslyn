using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Semantics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A list of <see cref="Diagnostic"/> objects.
    /// </summary>
    public partial struct DiagnosticList : IEnumerable<Diagnostic>
    {
        private readonly SyntaxTree syntaxTree;
        private readonly Syntax.InternalSyntax.SyntaxNode node;
        private int position;
        private int count;
        private List<Diagnostic> list;

        internal DiagnosticList(SyntaxTree syntaxTree, Syntax.InternalSyntax.SyntaxNode node, int position)
        {
            this.syntaxTree = syntaxTree;
            this.node = node;
            this.position = position;
            this.count = -1;
            this.list = null;
        }

        /// <summary>
        /// Returns the count of diagnostic instances in this list.
        /// </summary>
        public int Count
        {
            get
            {
                if (this.count == -1)
                {
                    this.count = CountDiagnostics(this.node);
                }

                return this.count;
            }
        }

        /// <summary>
        /// Returns the diagnostic instance at the specified index in this list.
        /// </summary>
        /// <param name="index">The index of the diagnostic.</param>
        /// <returns>The specified diagnostic.</returns>
        public Diagnostic this[int index]
        {
            get
            {
                if (list == null)
                {
                    list = this.ToList();
                }

                return list[index];
            }
        }

        /// <summary>
        /// Returns an enumerator over this list of diagnostics.
        /// </summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this.syntaxTree, this.node, this.position);
        }

        private static int CountDiagnostics(Syntax.InternalSyntax.SyntaxNode node)
        {
            int n = 0;
            if (node != null && node.ContainsDiagnostics)
            {
                n = node.GetDiagnostics().Length;
                var token = node as Syntax.InternalSyntax.SyntaxToken;
                if (token != null)
                {
                    var leading = token.GetLeadingTrivia();
                    if (leading != null)
                    {
                        n += CountDiagnostics(leading);
                    }

                    var trailing = token.GetTrailingTrivia();
                    if (trailing != null)
                    {
                        n += CountDiagnostics(trailing);
                    }
                }
                else
                {
                    for (int i = 0, nc = node.SlotCount; i < nc; i++)
                    {
                        var child = node.GetSlot(i);
                        if (child != null)
                        {
                            n += CountDiagnostics(child);
                        }
                    }
                }
            }

            return n;
        }

        // for debugging
        private Diagnostic[] Nodes
        {
            get
            {
                return this.ToArray();
            }
        }

        IEnumerator<Diagnostic> IEnumerable<Diagnostic>.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<Diagnostic>();
            }

            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (this.node == null)
            {
                return SpecializedCollections.EmptyEnumerator<Diagnostic>();
            }

            return this.GetEnumerator();
        }
    }
}
