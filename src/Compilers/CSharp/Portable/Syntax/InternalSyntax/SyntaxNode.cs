// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract class CSharpSyntaxNode : GreenNode
    {
        internal CSharpSyntaxNode(SyntaxKind kind)
            : base((ushort)kind)
        {
            GreenStats.NoteGreen(this);
        }

        internal CSharpSyntaxNode(SyntaxKind kind, int fullWidth)
            : base((ushort)kind, fullWidth)
        {
            GreenStats.NoteGreen(this);
        }

        internal CSharpSyntaxNode(SyntaxKind kind, DiagnosticInfo[] diagnostics)
            : base((ushort)kind, diagnostics)
        {
            GreenStats.NoteGreen(this);
        }

        internal CSharpSyntaxNode(SyntaxKind kind, DiagnosticInfo[] diagnostics, int fullWidth)
            : base((ushort)kind, diagnostics, fullWidth)
        {
            GreenStats.NoteGreen(this);
        }

        internal CSharpSyntaxNode(SyntaxKind kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations)
            : base((ushort)kind, diagnostics, annotations)
        {
            GreenStats.NoteGreen(this);
        }

        internal CSharpSyntaxNode(SyntaxKind kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, int fullWidth)
            : base((ushort)kind, diagnostics, annotations, fullWidth)
        {
            GreenStats.NoteGreen(this);
        }

        internal CSharpSyntaxNode(ObjectReader reader)
            : base(reader)
        {
        }

        public override string Language
        {
            get { return LanguageNames.CSharp; }
        }

        public SyntaxKind Kind
        {
            get { return (SyntaxKind)this.RawKind; }
        }

        public override string KindText
        {
            get
            {
                return this.Kind.ToString();
            }
        }

        public override int RawContextualKind
        {
            get
            {
                return this.RawKind;
            }
        }

        public override bool IsStructuredTrivia
        {
            get
            {
                return this is StructuredTriviaSyntax;
            }
        }

        public override bool IsDirective
        {
            get
            {
                return this is DirectiveTriviaSyntax;
            }
        }

        public override int GetSlotOffset(int index)
        {
            // This implementation should not support arbitrary
            // length lists since the implementation is O(n).
            System.Diagnostics.Debug.Assert(index < 11); // Max. slots 11 (TypeDeclarationSyntax)

            int offset = 0;
            for (int i = 0; i < index; i++)
            {
                var child = this.GetSlot(i);
                if (child != null)
                {
                    offset += child.FullWidth;
                }
            }

            return offset;
        }

        internal ChildSyntaxList ChildNodesAndTokens()
        {
            return new ChildSyntaxList(this);
        }

        /// <summary>
        /// Enumerates all nodes of the tree rooted by this node (including this node).
        /// </summary>
        internal IEnumerable<GreenNode> EnumerateNodes()
        {
            yield return this;

            var stack = new Stack<ChildSyntaxList.Enumerator>(24);
            stack.Push(this.ChildNodesAndTokens().GetEnumerator());

            while (stack.Count > 0)
            {
                var en = stack.Pop();
                if (!en.MoveNext())
                {
                    // no more down this branch
                    continue;
                }

                var current = en.Current;
                stack.Push(en); // put it back on stack (struct enumerator)

                yield return current;

                if (!(current is SyntaxToken))
                {
                    // not token, so consider children
                    stack.Push(((CSharpSyntaxNode)current).ChildNodesAndTokens().GetEnumerator());
                    continue;
                }
            }
        }

        public SyntaxToken GetFirstToken()
        {
            return (SyntaxToken)this.GetFirstTerminal();
        }

        public SyntaxToken GetLastToken()
        {
            return (SyntaxToken)this.GetLastTerminal();
        }

        public SyntaxToken GetLastNonmissingToken()
        {
            return (SyntaxToken)this.GetLastNonmissingTerminal();
        }

        public virtual CSharpSyntaxNode GetLeadingTrivia()
        {
            return null;
        }

        public override GreenNode GetLeadingTriviaCore()
        {
            return this.GetLeadingTrivia();
        }

        public virtual CSharpSyntaxNode GetTrailingTrivia()
        {
            return null;
        }

        public override GreenNode GetTrailingTriviaCore()
        {
            return this.GetTrailingTrivia();
        }

        public override string ToString()
        {
            var sb = PooledStringBuilder.GetInstance();
            var writer = new System.IO.StringWriter(sb.Builder, System.Globalization.CultureInfo.InvariantCulture);
            this.WriteTo(writer, leading: false, trailing: false);
            return sb.ToStringAndFree();
        }

        public override string ToFullString()
        {
            var sb = PooledStringBuilder.GetInstance();
            var writer = new System.IO.StringWriter(sb.Builder, System.Globalization.CultureInfo.InvariantCulture);
            this.WriteTo(writer, leading: true, trailing: true);
            return sb.ToStringAndFree();
        }

        public abstract TResult Accept<TResult>(CSharpSyntaxVisitor<TResult> visitor);

        public abstract void Accept(CSharpSyntaxVisitor visitor);

        internal virtual DirectiveStack ApplyDirectives(DirectiveStack stack)
        {
            if (this.ContainsDirectives)
            {
                for (int i = 0, n = this.SlotCount; i < n; i++)
                {
                    var child = this.GetSlot(i);
                    if (child != null)
                    {
                        stack = ((CSharpSyntaxNode)child).ApplyDirectives(stack);
                    }
                }
            }

            return stack;
        }

        internal virtual IList<DirectiveTriviaSyntax> GetDirectives()
        {
            if ((this.flags & NodeFlags.ContainsDirectives) != 0)
            {
                var list = new List<DirectiveTriviaSyntax>(32);
                GetDirectives(this, list);
                return list;
            }

            return SpecializedCollections.EmptyList<DirectiveTriviaSyntax>();
        }

        private static void GetDirectives(GreenNode node, List<DirectiveTriviaSyntax> directives)
        {
            if (node != null && node.ContainsDirectives)
            {
                var d = node as DirectiveTriviaSyntax;
                if (d != null)
                {
                    directives.Add(d);
                }
                else
                {
                    var t = node as SyntaxToken;
                    if (t != null)
                    {
                        GetDirectives(t.GetLeadingTrivia(), directives);
                        GetDirectives(t.GetTrailingTrivia(), directives);
                    }
                    else
                    {
                        for (int i = 0, n = node.SlotCount; i < n; i++)
                        {
                            GetDirectives(node.GetSlot(i), directives);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Should only be called during construction.
        /// </summary>
        /// <remarks>
        /// This should probably be an extra constructor parameter, but we don't need more constructor overloads.
        /// </remarks>
        protected void SetFactoryContext(SyntaxFactoryContext context)
        {
            if (context.IsInAsync)
            {
                this.flags |= NodeFlags.FactoryContextIsInAsync;
            }

            if (context.IsInQuery)
            {
                this.flags |= NodeFlags.FactoryContextIsInQuery;
            }
        }

        internal static NodeFlags SetFactoryContext(NodeFlags flags, SyntaxFactoryContext context)
        {
            if (context.IsInAsync)
            {
                flags |= NodeFlags.FactoryContextIsInAsync;
            }

            if (context.IsInQuery)
            {
                flags |= NodeFlags.FactoryContextIsInQuery;
            }

            return flags;
        }

        public override AbstractSyntaxNavigator Navigator
        {
            get
            {
                return SyntaxNavigator.Instance;
            }
        }

        public override GreenNode CreateList(IEnumerable<GreenNode> nodes, bool alwaysCreateListNode)
        {
            if (nodes == null)
            {
                return null;
            }

            var list = nodes.Select(n => (CSharpSyntaxNode)n).ToArray();

            switch (list.Length)
            {
                case 0:
                    return null;
                case 1:
                    if (alwaysCreateListNode)
                    {
                        goto default;
                    }
                    else
                    {
                        return list[0];
                    }
                case 2:
                    return SyntaxList.List(list[0], list[1]);
                case 3:
                    return SyntaxList.List(list[0], list[1], list[2]);
                default:
                    return SyntaxList.List(list);
            }
        }

        public override Microsoft.CodeAnalysis.SyntaxToken CreateSeparator<TNode>(SyntaxNode element)
        {
            return Microsoft.CodeAnalysis.CSharp.SyntaxFactory.Token(SyntaxKind.CommaToken);
        }

        public override bool IsTriviaWithEndOfLine()
        {
            return this.Kind == SyntaxKind.EndOfLineTrivia
                || this.Kind == SyntaxKind.SingleLineCommentTrivia;
        }

        // Use conditional weak table so we always return same identity for structured trivia
        private static readonly ConditionalWeakTable<SyntaxNode, Dictionary<Microsoft.CodeAnalysis.SyntaxTrivia, SyntaxNode>> s_structuresTable
            = new ConditionalWeakTable<SyntaxNode, Dictionary<Microsoft.CodeAnalysis.SyntaxTrivia, SyntaxNode>>();

        /// <summary>
        /// Gets the syntax node represented the structure of this trivia, if any. The HasStructure property can be used to 
        /// determine if this trivia has structure.
        /// </summary>
        /// <returns>
        /// A CSharpSyntaxNode derived from StructuredTriviaSyntax, with the structured view of this trivia node. 
        /// If this trivia node does not have structure, returns null.
        /// </returns>
        /// <remarks>
        /// Some types of trivia have structure that can be accessed as additional syntax nodes.
        /// These forms of trivia include: 
        ///   directives, where the structure describes the structure of the directive.
        ///   documentation comments, where the structure describes the XML structure of the comment.
        ///   skipped tokens, where the structure describes the tokens that were skipped by the parser.
        /// </remarks>

        public override SyntaxNode GetStructure(Microsoft.CodeAnalysis.SyntaxTrivia trivia)
        {
            if (trivia.HasStructure)
            {
                var parent = trivia.Token.Parent;
                if (parent != null)
                {
                    SyntaxNode structure;
                    var structsInParent = s_structuresTable.GetOrCreateValue(parent);
                    lock (structsInParent)
                    {
                        if (!structsInParent.TryGetValue(trivia, out structure))
                        {
                            structure = CSharp.Syntax.StructuredTriviaSyntax.Create(trivia);
                            structsInParent.Add(trivia, structure);
                        }
                    }

                    return structure;
                }
                else
                {
                    return CSharp.Syntax.StructuredTriviaSyntax.Create(trivia);
                }
            }

            return null;
        }
    }
}
