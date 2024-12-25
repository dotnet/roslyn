// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Syntax.InternalSyntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract partial class GreenNode
    {
        private string GetDebuggerDisplay()
        {
            return this.GetType().Name + " " + this.KindText + " " + this.ToString();
        }

        internal const int ListKind = 1;

        // Pack the kind, node-flags, slot-count, and full-width into 64bits. Note: if we need more bits in the future
        // (say for additional node-flags), we can always directly use a packed int64 here, and manage where all these
        // bits go manually.

        /// <summary>
        /// Value used to indicate the slot count was too large to be encoded directly in our <see cref="_nodeFlagsAndSlotCount"/>
        /// value.  Callers will have to store the value elsewhere and retrieve the full value themselves.
        /// </summary>
        protected const int SlotCountTooLarge = 0b0000000000001111;

        private readonly ushort _kind;
        private NodeFlagsAndSlotCount _nodeFlagsAndSlotCount;
        private int _fullWidth;

        private static readonly ConditionalWeakTable<GreenNode, DiagnosticInfo[]> s_diagnosticsTable =
            new ConditionalWeakTable<GreenNode, DiagnosticInfo[]>();

        private static readonly ConditionalWeakTable<GreenNode, SyntaxAnnotation[]> s_annotationsTable =
            new ConditionalWeakTable<GreenNode, SyntaxAnnotation[]>();

        private static readonly DiagnosticInfo[] s_noDiagnostics = Array.Empty<DiagnosticInfo>();
        private static readonly SyntaxAnnotation[] s_noAnnotations = Array.Empty<SyntaxAnnotation>();
        private static readonly IEnumerable<SyntaxAnnotation> s_noAnnotationsEnumerable = SpecializedCollections.EmptyEnumerable<SyntaxAnnotation>();

        protected GreenNode(ushort kind)
        {
            _kind = kind;
        }

        protected GreenNode(ushort kind, int fullWidth)
        {
            _kind = kind;
            _fullWidth = fullWidth;
        }

        protected GreenNode(ushort kind, DiagnosticInfo[]? diagnostics, int fullWidth)
        {
            _kind = kind;
            _fullWidth = fullWidth;
            if (diagnostics?.Length > 0)
            {
                SetFlags(NodeFlags.ContainsDiagnostics);
                s_diagnosticsTable.Add(this, diagnostics);
            }
        }

        protected GreenNode(ushort kind, DiagnosticInfo[]? diagnostics)
        {
            _kind = kind;
            if (diagnostics?.Length > 0)
            {
                SetFlags(NodeFlags.ContainsDiagnostics);
                s_diagnosticsTable.Add(this, diagnostics);
            }
        }

        protected GreenNode(ushort kind, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[]? annotations) :
            this(kind, diagnostics)
        {
            if (annotations?.Length > 0)
            {
                foreach (var annotation in annotations)
                {
                    if (annotation == null) throw new ArgumentException(paramName: nameof(annotations), message: "" /*CSharpResources.ElementsCannotBeNull*/);
                }

                SetFlags(NodeFlags.HasAnnotationsDirectly | NodeFlags.ContainsAnnotations);
                s_annotationsTable.Add(this, annotations);
            }
        }

        protected GreenNode(ushort kind, DiagnosticInfo[]? diagnostics, SyntaxAnnotation[]? annotations, int fullWidth) :
            this(kind, diagnostics, fullWidth)
        {
            if (annotations?.Length > 0)
            {
                foreach (var annotation in annotations)
                {
                    if (annotation == null) throw new ArgumentException(paramName: nameof(annotations), message: "" /*CSharpResources.ElementsCannotBeNull*/);
                }

                SetFlags(NodeFlags.HasAnnotationsDirectly | NodeFlags.ContainsAnnotations);
                s_annotationsTable.Add(this, annotations);
            }
        }

        protected void AdjustFlagsAndWidth(GreenNode node)
        {
            RoslynDebug.Assert(node != null, "PERF: caller must ensure that node!=null, we do not want to re-check that here.");
            SetFlags(node.Flags & NodeFlags.InheritMask);
            _fullWidth += node._fullWidth;
        }

        public abstract string Language { get; }

        #region Kind 
        public int RawKind
        {
            get { return _kind; }
        }

        public bool IsList
        {
            get
            {
                return RawKind == ListKind;
            }
        }

        public abstract string KindText { get; }

        public virtual bool IsStructuredTrivia => false;
        public virtual bool IsDirective => false;
        public virtual bool IsToken => false;
        public virtual bool IsTrivia => false;
        public virtual bool IsSkippedTokensTrivia => false;
        public virtual bool IsDocumentationCommentTrivia => false;

        #endregion

        #region Slots 
        public int SlotCount
        {
            get
            {
                var count = _nodeFlagsAndSlotCount.SmallSlotCount;
                return count == SlotCountTooLarge ? GetSlotCount() : count;
            }

            protected set
            {
                Debug.Assert(value <= byte.MaxValue);
                _nodeFlagsAndSlotCount.SmallSlotCount = (byte)value;
            }
        }

        internal abstract GreenNode? GetSlot(int index);

        internal GreenNode GetRequiredSlot(int index)
        {
            var node = GetSlot(index);
            RoslynDebug.Assert(node is object);
            return node;
        }

        /// <summary>
        /// Called when <see cref="NodeFlagsAndSlotCount.SmallSlotCount"/> returns a value of <see cref="SlotCountTooLarge"/>.
        /// </summary>
        protected virtual int GetSlotCount()
        {
            // This should only be called for nodes that couldn't store their slot count effectively in our
            // _nodeFlagsAndSlotCount field.  The only nodes that cannot do that are the `WithManyChildren` list types.
            // All of which should be subclassing this method.
            throw ExceptionUtilities.Unreachable();
        }

        public virtual int GetSlotOffset(int index)
        {
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

        internal Syntax.InternalSyntax.ChildSyntaxList ChildNodesAndTokens()
        {
            return new Syntax.InternalSyntax.ChildSyntaxList(this);
        }

        /// <summary>
        /// Enumerates all green nodes of the tree rooted by this node (including this node).  This includes normal
        /// nodes, list nodes, and tokens.  The nodes will be returned in depth-first order.  This will not descend 
        /// into trivia or structured trivia.
        /// </summary>
        public NodeEnumerable EnumerateNodes()
            => new NodeEnumerable(this);

        /// <summary>
        /// Find the slot that contains the given offset.
        /// </summary>
        /// <param name="offset">The target offset. Must be between 0 and <see cref="FullWidth"/>.</param>
        /// <returns>The slot index of the slot containing the given offset.</returns>
        /// <remarks>
        /// The base implementation is a linear search. This should be overridden
        /// if a derived class can implement it more efficiently.
        /// </remarks>
        public virtual int FindSlotIndexContainingOffset(int offset)
        {
            Debug.Assert(0 <= offset && offset < FullWidth);

            int i;
            int accumulatedWidth = 0;
            for (i = 0; ; i++)
            {
                Debug.Assert(i < SlotCount);
                var child = GetSlot(i);
                if (child != null)
                {
                    accumulatedWidth += child.FullWidth;
                    if (offset < accumulatedWidth)
                    {
                        break;
                    }
                }
            }

            return i;
        }

        #endregion

        #region Flags 

        /// <summary>
        /// Special flags a node can have.  Note: while this is typed as being `ushort`, we can only practically use 12
        /// of those 16 bits as we use the remaining 4 bits to store the slot count of a node.
        /// </summary>
        [Flags]
        internal enum NodeFlags : ushort
        {
            None = 0,
            /// <summary>
            /// If this node is missing or not.  We use a non-zero value for the not-missing case so that this value
            /// automatically merges upwards when building parent nodes.  In other words, once we have one node that is
            /// not-missing, all nodes above it are definitely not-missing as well.
            /// </summary>
            IsNotMissing = 1 << 0,
            /// <summary>
            /// If this node directly has annotations (not its descendants).  <see cref="ContainsAnnotations"/> can be
            /// used to determine if a node or any of its descendants has annotations.
            /// </summary>
            HasAnnotationsDirectly = 1 << 1,

            FactoryContextIsInAsync = 1 << 2,
            FactoryContextIsInQuery = 1 << 3,
            FactoryContextIsInIterator = FactoryContextIsInQuery,  // VB does not use "InQuery", but uses "InIterator" instead
            FactoryContextIsInFieldKeywordContext = 1 << 4,

            // Flags that are inherited upwards when building parent nodes.  They should all start with "Contains" to
            // indicate that the information could be found on it or anywhere in its children.

            /// <summary>
            /// If this node, or any of its descendants has annotations attached to them.
            /// </summary>
            ContainsAnnotations = 1 << 5,
            /// <summary>
            /// If this node, or any of its descendants has attributes attached to it.
            /// </summary>
            ContainsAttributes = 1 << 6,
            ContainsDiagnostics = 1 << 7,
            ContainsDirectives = 1 << 8,
            ContainsSkippedText = 1 << 9,
            ContainsStructuredTrivia = 1 << 10,

            InheritMask = IsNotMissing | ContainsAnnotations | ContainsAttributes | ContainsDiagnostics | ContainsDirectives | ContainsSkippedText | ContainsStructuredTrivia,
        }

        internal NodeFlags Flags
        {
            get { return this._nodeFlagsAndSlotCount.NodeFlags; }
        }

        internal void SetFlags(NodeFlags flags)
        {
            _nodeFlagsAndSlotCount.NodeFlags |= flags;
        }

        internal void ClearFlags(NodeFlags flags)
        {
            _nodeFlagsAndSlotCount.NodeFlags &= ~flags;
        }

        internal bool IsMissing
        {
            get
            {
                // flag has reversed meaning hence "=="
                return (this.Flags & NodeFlags.IsNotMissing) == 0;
            }
        }

        internal bool ParsedInAsync
        {
            get
            {
                return (this.Flags & NodeFlags.FactoryContextIsInAsync) != 0;
            }
        }

        internal bool ParsedInQuery
        {
            get
            {
                return (this.Flags & NodeFlags.FactoryContextIsInQuery) != 0;
            }
        }

        internal bool ParsedInIterator
        {
            get
            {
                return (this.Flags & NodeFlags.FactoryContextIsInIterator) != 0;
            }
        }

        internal bool ParsedInFieldKeywordContext
        {
            get
            {
                return (this.Flags & NodeFlags.FactoryContextIsInFieldKeywordContext) != 0;
            }
        }

        public bool ContainsSkippedText
        {
            get
            {
                return (this.Flags & NodeFlags.ContainsSkippedText) != 0;
            }
        }

        public bool ContainsStructuredTrivia
        {
            get
            {
                return (this.Flags & NodeFlags.ContainsStructuredTrivia) != 0;
            }
        }

        public bool ContainsDirectives
        {
            get
            {
                return (this.Flags & NodeFlags.ContainsDirectives) != 0;
            }
        }

        public bool ContainsAttributes
        {
            get
            {
                return (this.Flags & NodeFlags.ContainsAttributes) != 0;
            }
        }

        public bool ContainsDiagnostics
        {
            get
            {
                return (this.Flags & NodeFlags.ContainsDiagnostics) != 0;
            }
        }

        public bool ContainsAnnotations
        {
            get
            {
                return (this.Flags & NodeFlags.ContainsAnnotations) != 0;
            }
        }

        public bool HasAnnotationsDirectly
        {
            get
            {
                return (this.Flags & NodeFlags.HasAnnotationsDirectly) != 0;
            }
        }

        #endregion

        #region Spans
        public int FullWidth
        {
            get
            {
                return _fullWidth;
            }

            protected set
            {
                _fullWidth = value;
            }
        }

        public virtual int Width
        {
            get
            {
                return _fullWidth - this.GetLeadingTriviaWidth() - this.GetTrailingTriviaWidth();
            }
        }

        public virtual int GetLeadingTriviaWidth()
        {
            return this.FullWidth != 0 ?
                this.GetFirstTerminal()!.GetLeadingTriviaWidth() :
                0;
        }

        public virtual int GetTrailingTriviaWidth()
        {
            return this.FullWidth != 0 ?
                this.GetLastTerminal()!.GetTrailingTriviaWidth() :
                0;
        }

        public bool HasLeadingTrivia
        {
            get
            {
                return this.GetLeadingTriviaWidth() != 0;
            }
        }

        public bool HasTrailingTrivia
        {
            get
            {
                return this.GetTrailingTriviaWidth() != 0;
            }
        }
        #endregion

        #region Annotations 
        public bool HasAnnotations(string annotationKind)
        {
            var annotations = this.GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }

            foreach (var a in annotations)
            {
                if (a.Kind == annotationKind)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnnotations(IEnumerable<string> annotationKinds)
        {
            var annotations = this.GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }

            foreach (var a in annotations)
            {
                if (annotationKinds.Contains(a.Kind))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasAnnotation([NotNullWhen(true)] SyntaxAnnotation? annotation)
        {
            var annotations = this.GetAnnotations();
            if (annotations == s_noAnnotations)
            {
                return false;
            }

            foreach (var a in annotations)
            {
                if (a == annotation)
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerable<SyntaxAnnotation> GetAnnotations(string annotationKind)
        {
            if (string.IsNullOrWhiteSpace(annotationKind))
            {
                throw new ArgumentNullException(nameof(annotationKind));
            }

            var annotations = this.GetAnnotations();

            if (annotations == s_noAnnotations)
            {
                return s_noAnnotationsEnumerable;
            }

            return GetAnnotationsSlow(annotations, annotationKind);
        }

        private static IEnumerable<SyntaxAnnotation> GetAnnotationsSlow(SyntaxAnnotation[] annotations, string annotationKind)
        {
            foreach (var annotation in annotations)
            {
                if (annotation.Kind == annotationKind)
                {
                    yield return annotation;
                }
            }
        }

        public IEnumerable<SyntaxAnnotation> GetAnnotations(IEnumerable<string> annotationKinds)
        {
            if (annotationKinds == null)
            {
                throw new ArgumentNullException(nameof(annotationKinds));
            }

            var annotations = this.GetAnnotations();

            if (annotations == s_noAnnotations)
            {
                return s_noAnnotationsEnumerable;
            }

            return GetAnnotationsSlow(annotations, annotationKinds);
        }

        private static IEnumerable<SyntaxAnnotation> GetAnnotationsSlow(SyntaxAnnotation[] annotations, IEnumerable<string> annotationKinds)
        {
            foreach (var annotation in annotations)
            {
                if (annotationKinds.Contains(annotation.Kind))
                {
                    yield return annotation;
                }
            }
        }

        public SyntaxAnnotation[] GetAnnotations()
        {
            if (!this.HasAnnotationsDirectly)
                return s_noAnnotations;

            var found = s_annotationsTable.TryGetValue(this, out var annotations);
            Debug.Assert(found, "We must be able to find annotations since we had the bit set on ourselves");
            Debug.Assert(annotations != null, "annotations should not be null");
            Debug.Assert(annotations != s_noAnnotations, "annotations should not be s_noAnnotations");
            Debug.Assert(annotations.Length != 0, "annotations should be non-empty");
            return annotations;
        }

        internal abstract GreenNode SetAnnotations(SyntaxAnnotation[]? annotations);

        #endregion

        #region Diagnostics
        internal DiagnosticInfo[] GetDiagnostics()
        {
            if (this.ContainsDiagnostics)
            {
                DiagnosticInfo[]? diags;
                if (s_diagnosticsTable.TryGetValue(this, out diags))
                {
                    return diags;
                }
            }

            return s_noDiagnostics;
        }

        internal abstract GreenNode SetDiagnostics(DiagnosticInfo[]? diagnostics);
        #endregion

        #region Text

        public virtual string ToFullString()
        {
            var sb = PooledStringBuilder.GetInstance();
            var writer = new System.IO.StringWriter(sb.Builder, System.Globalization.CultureInfo.InvariantCulture);
            this.WriteTo(writer, leading: true, trailing: true);
            return sb.ToStringAndFree();
        }

        public override string ToString()
        {
            var sb = PooledStringBuilder.GetInstance();
            var writer = new System.IO.StringWriter(sb.Builder, System.Globalization.CultureInfo.InvariantCulture);
            this.WriteTo(writer, leading: false, trailing: false);
            return sb.ToStringAndFree();
        }

        public void WriteTo(System.IO.TextWriter writer)
        {
            this.WriteTo(writer, leading: true, trailing: true);
        }

        protected internal void WriteTo(TextWriter writer, bool leading, bool trailing)
        {
            // Use an actual stack so we can write out deeply recursive structures without overflowing.
            var stack = ArrayBuilder<(GreenNode node, bool leading, bool trailing)>.GetInstance();
            stack.Push((this, leading, trailing));

            // Separated out stack processing logic so that it does not unintentionally refer to 
            // "this", "leading" or "trailing".
            processStack(writer, stack);
            stack.Free();
            return;

            static void processStack(
                TextWriter writer,
                ArrayBuilder<(GreenNode node, bool leading, bool trailing)> stack)
            {
                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    var currentNode = current.node;
                    var currentLeading = current.leading;
                    var currentTrailing = current.trailing;

                    if (currentNode.IsToken)
                    {
                        currentNode.WriteTokenTo(writer, currentLeading, currentTrailing);
                        continue;
                    }

                    if (currentNode.IsTrivia)
                    {
                        currentNode.WriteTriviaTo(writer);
                        continue;
                    }

                    var firstIndex = GetFirstNonNullChildIndex(currentNode);
                    var lastIndex = GetLastNonNullChildIndex(currentNode);

                    for (var i = lastIndex; i >= firstIndex; i--)
                    {
                        var child = currentNode.GetSlot(i);
                        if (child != null)
                        {
                            var first = i == firstIndex;
                            var last = i == lastIndex;
                            stack.Push((child, currentLeading | !first, currentTrailing | !last));
                        }
                    }
                }
            }
        }

        private static int GetFirstNonNullChildIndex(GreenNode node)
        {
            int n = node.SlotCount;
            int firstIndex = 0;
            for (; firstIndex < n; firstIndex++)
            {
                var child = node.GetSlot(firstIndex);
                if (child != null)
                {
                    break;
                }
            }

            return firstIndex;
        }

        private static int GetLastNonNullChildIndex(GreenNode node)
        {
            int n = node.SlotCount;
            int lastIndex = n - 1;
            for (; lastIndex >= 0; lastIndex--)
            {
                var child = node.GetSlot(lastIndex);
                if (child != null)
                {
                    break;
                }
            }

            return lastIndex;
        }

        protected virtual void WriteTriviaTo(TextWriter writer)
        {
            throw new NotImplementedException();
        }

        protected virtual void WriteTokenTo(TextWriter writer, bool leading, bool trailing)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Tokens 

        public virtual int RawContextualKind { get { return this.RawKind; } }
        public virtual object? GetValue() { return null; }
        public virtual string GetValueText() { return string.Empty; }
        public virtual GreenNode? GetLeadingTriviaCore() { return null; }
        public virtual GreenNode? GetTrailingTriviaCore() { return null; }

        public virtual GreenNode WithLeadingTrivia(GreenNode? trivia)
        {
            return this;
        }

        public virtual GreenNode WithTrailingTrivia(GreenNode? trivia)
        {
            return this;
        }

        internal GreenNode? GetFirstTerminal()
        {
            GreenNode? node = this;

            do
            {
                GreenNode? firstChild = null;
                for (int i = 0, n = node.SlotCount; i < n; i++)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        firstChild = child;
                        break;
                    }
                }
                node = firstChild;
            }
            // Note: it's ok to examine SmallSlotCount here.  All we're trying to do is make sure we have at least one
            // child.  And SmallSlotCount works both for small counts and large counts.  This avoids an unnecessary
            // virtual call for large list nodes.
            while (node?._nodeFlagsAndSlotCount.SmallSlotCount > 0);

            return node;
        }

        internal GreenNode? GetLastTerminal()
        {
            GreenNode? node = this;

            do
            {
                GreenNode? lastChild = null;
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null)
                    {
                        lastChild = child;
                        break;
                    }
                }
                node = lastChild;
            }
            // Note: it's ok to examine SmallSlotCount here.  All we're trying to do is make sure we have at least one
            // child.  And SmallSlotCount works both for small counts and large counts.  This avoids an unnecessary
            // virtual call for large list nodes.
            while (node?._nodeFlagsAndSlotCount.SmallSlotCount > 0);

            return node;
        }

        internal GreenNode? GetLastNonmissingTerminal()
        {
            GreenNode? node = this;

            do
            {
                GreenNode? nonmissingChild = null;
                for (int i = node.SlotCount - 1; i >= 0; i--)
                {
                    var child = node.GetSlot(i);
                    if (child != null && !child.IsMissing)
                    {
                        nonmissingChild = child;
                        break;
                    }
                }
                node = nonmissingChild;
            }
            // Note: it's ok to examine SmallSlotCount here.  All we're trying to do is make sure we have at least one
            // child.  And SmallSlotCount works both for small counts and large counts.  This avoids an unnecessary
            // virtual call for large list nodes.
            while (node?._nodeFlagsAndSlotCount.SmallSlotCount > 0);

            return node;
        }
        #endregion

        #region Equivalence 
        public virtual bool IsEquivalentTo([NotNullWhen(true)] GreenNode? other)
        {
            if (this == other)
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            return EquivalentToInternal(this, other);
        }

        private static bool EquivalentToInternal(GreenNode node1, GreenNode node2)
        {
            if (node1.RawKind != node2.RawKind)
            {
                // A single-element list is usually represented as just a single node,
                // but can be represented as a List node with one child. Move to that
                // child if necessary.
                if (node1.IsList && node1.SlotCount == 1)
                {
                    node1 = node1.GetRequiredSlot(0);
                }

                if (node2.IsList && node2.SlotCount == 1)
                {
                    node2 = node2.GetRequiredSlot(0);
                }

                if (node1.RawKind != node2.RawKind)
                {
                    return false;
                }
            }

            if (node1._fullWidth != node2._fullWidth)
            {
                return false;
            }

            var n = node1.SlotCount;
            if (n != node2.SlotCount)
            {
                return false;
            }

            for (int i = 0; i < n; i++)
            {
                var node1Child = node1.GetSlot(i);
                var node2Child = node2.GetSlot(i);
                if (node1Child != null && node2Child != null && !node1Child.IsEquivalentTo(node2Child))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        public abstract SyntaxNode GetStructure(SyntaxTrivia parentTrivia);

        #region Factories 

        public abstract SyntaxToken CreateSeparator(SyntaxNode element);
        public abstract bool IsTriviaWithEndOfLine(); // trivia node has end of line

        /*
         * There are 3 overloads of this, because most callers already know what they have is a List<T> and only transform it.
         * In those cases List<TFrom> performs much better.
         * In other cases, the type is unknown / is IEnumerable<T>, where we try to find the best match.
         * There is another overload for IReadOnlyList, since most collections already implement this, so checking for it will
         * perform better then copying to a List<T>, though not as good as List<T> directly.
         */
        public static GreenNode? CreateList<TFrom>(IEnumerable<TFrom>? enumerable, Func<TFrom, GreenNode> select)
            => enumerable switch
            {
                null => null,
                List<TFrom> l => CreateList(l, select),
                IReadOnlyList<TFrom> l => CreateList(l, select),
                _ => CreateList(enumerable.ToList(), select)
            };

        public static GreenNode? CreateList<TFrom>(List<TFrom> list, Func<TFrom, GreenNode> select)
        {
            switch (list.Count)
            {
                case 0:
                    return null;
                case 1:
                    return select(list[0]);
                case 2:
                    return Syntax.InternalSyntax.SyntaxList.List(select(list[0]), select(list[1]));
                case 3:
                    return Syntax.InternalSyntax.SyntaxList.List(select(list[0]), select(list[1]), select(list[2]));
                default:
                    {
                        var array = new ArrayElement<GreenNode>[list.Count];
                        for (int i = 0; i < array.Length; i++)
                            array[i].Value = select(list[i]);
                        return Syntax.InternalSyntax.SyntaxList.List(array);
                    }
            }
        }

        public static GreenNode? CreateList<TFrom>(IReadOnlyList<TFrom> list, Func<TFrom, GreenNode> select)
        {
            switch (list.Count)
            {
                case 0:
                    return null;
                case 1:
                    return select(list[0]);
                case 2:
                    return Syntax.InternalSyntax.SyntaxList.List(select(list[0]), select(list[1]));
                case 3:
                    return Syntax.InternalSyntax.SyntaxList.List(select(list[0]), select(list[1]), select(list[2]));
                default:
                    {
                        var array = new ArrayElement<GreenNode>[list.Count];
                        for (int i = 0; i < array.Length; i++)
                            array[i].Value = select(list[i]);
                        return Syntax.InternalSyntax.SyntaxList.List(array);
                    }
            }
        }

        public SyntaxNode CreateRed()
        {
            return CreateRed(null, 0);
        }

        internal abstract SyntaxNode CreateRed(SyntaxNode? parent, int position);

        #endregion

        #region Caching

        internal const int MaxCachedChildNum = 3;

        internal bool IsCacheable
        {
            get
            {
                return ((this.Flags & NodeFlags.InheritMask) == NodeFlags.IsNotMissing) &&
                    this.SlotCount <= GreenNode.MaxCachedChildNum;
            }
        }

        internal int GetCacheHash()
        {
            Debug.Assert(this.IsCacheable);

            int code = (int)(this.Flags) ^ this.RawKind;
            int cnt = this.SlotCount;
            for (int i = 0; i < cnt; i++)
            {
                var child = GetSlot(i);
                if (child != null)
                {
                    code = Hash.Combine(RuntimeHelpers.GetHashCode(child), code);
                }
            }

            return code & Int32.MaxValue;
        }

        internal bool IsCacheEquivalent(int kind, NodeFlags flags, GreenNode? child1)
        {
            Debug.Assert(this.IsCacheable);

            return this.RawKind == kind &&
                this.Flags == flags &&
                this.SlotCount == 1 &&
                this.GetSlot(0) == child1;
        }

        internal bool IsCacheEquivalent(int kind, NodeFlags flags, GreenNode? child1, GreenNode? child2)
        {
            Debug.Assert(this.IsCacheable);

            return this.RawKind == kind &&
                this.Flags == flags &&
                this.SlotCount == 2 &&
                this.GetSlot(0) == child1 &&
                this.GetSlot(1) == child2;
        }

        internal bool IsCacheEquivalent(int kind, NodeFlags flags, GreenNode? child1, GreenNode? child2, GreenNode? child3)
        {
            Debug.Assert(this.IsCacheable);

            return this.RawKind == kind &&
                this.Flags == flags &&
                this.SlotCount == 3 &&
                this.GetSlot(0) == child1 &&
                this.GetSlot(1) == child2 &&
                this.GetSlot(2) == child3;
        }
        #endregion //Caching

        /// <summary>
        /// Add an error to the given node, creating a new node that is the same except it has no parent,
        /// and has the given error attached to it. The error span is the entire span of this node.
        /// </summary>
        /// <param name="err">The error to attach to this node</param>
        /// <returns>A new node, with no parent, that has this error added to it.</returns>
        /// <remarks>Since nodes are immutable, the only way to create nodes with errors attached is to create a node without an error,
        /// then add an error with this method to create another node.</remarks>
        internal GreenNode AddError(DiagnosticInfo err)
        {
            DiagnosticInfo[] errorInfos;

            // If the green node already has errors, add those on.
            if (GetDiagnostics() == null)
            {
                errorInfos = new[] { err };
            }
            else
            {
                // Add the error to the error list.
                errorInfos = GetDiagnostics();
                var length = errorInfos.Length;
                Array.Resize(ref errorInfos, length + 1);
                errorInfos[length] = err;
            }

            // Get a new green node with the errors added on.
            return SetDiagnostics(errorInfos);
        }
    }
}
