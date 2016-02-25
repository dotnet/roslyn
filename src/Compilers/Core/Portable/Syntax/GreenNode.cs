// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract class GreenNode : IObjectWritable, IObjectReadable
    {
        private string GetDebuggerDisplay()
        {
            return this.GetType().Name + " " + this.KindText + " " + this.ToString();
        }

        internal const int ListKind = 1;

        private readonly ushort _kind;
        protected NodeFlags flags;
        private byte _slotCount;
        private int _fullWidth;

        private static readonly ConditionalWeakTable<GreenNode, DiagnosticInfo[]> s_diagnosticsTable =
            new ConditionalWeakTable<GreenNode, DiagnosticInfo[]>();

        private static readonly ConditionalWeakTable<GreenNode, SyntaxAnnotation[]> s_annotationsTable =
            new ConditionalWeakTable<GreenNode, SyntaxAnnotation[]>();

        private static readonly DiagnosticInfo[] s_noDiagnostics = SpecializedCollections.EmptyArray<DiagnosticInfo>();
        private static readonly SyntaxAnnotation[] s_noAnnotations = SpecializedCollections.EmptyArray<SyntaxAnnotation>();
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

        protected GreenNode(ushort kind, DiagnosticInfo[] diagnostics, int fullWidth)
        {
            _kind = kind;
            _fullWidth = fullWidth;
            if (diagnostics?.Length > 0)
            {
                this.flags |= NodeFlags.ContainsDiagnostics;
                s_diagnosticsTable.Add(this, diagnostics);
            }
        }

        protected GreenNode(ushort kind, DiagnosticInfo[] diagnostics)
        {
            _kind = kind;
            if (diagnostics?.Length > 0)
            {
                this.flags |= NodeFlags.ContainsDiagnostics;
                s_diagnosticsTable.Add(this, diagnostics);
            }
        }

        protected GreenNode(ushort kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations) :
            this(kind, diagnostics)
        {
            if (annotations?.Length > 0)
            {
                foreach (var annotation in annotations)
                {
                    if (annotation == null) throw new ArgumentException(paramName: nameof(annotations), message: "" /*CSharpResources.ElementsCannotBeNull*/);
                }

                this.flags |= NodeFlags.ContainsAnnotations;
                s_annotationsTable.Add(this, annotations);
            }
        }

        protected GreenNode(ushort kind, DiagnosticInfo[] diagnostics, SyntaxAnnotation[] annotations, int fullWidth) :
            this(kind, diagnostics, fullWidth)
        {
            if (annotations?.Length > 0)
            {
                foreach (var annotation in annotations)
                {
                    if (annotation == null) throw new ArgumentException(paramName: nameof(annotations), message: "" /*CSharpResources.ElementsCannotBeNull*/);
                }

                this.flags |= NodeFlags.ContainsAnnotations;
                s_annotationsTable.Add(this, annotations);
            }
        }

        protected void AdjustFlagsAndWidth(GreenNode node)
        {
            Debug.Assert(node != null, "PERF: caller must ensure that node!=null, we do not want to re-check that here.");
            this.flags |= (node.flags & NodeFlags.InheritMask);
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
        public virtual bool IsStructuredTrivia { get { return false; } }
        public virtual bool IsDirective { get { return false; } }
        public virtual bool IsToken { get { return false; } }

        #endregion

        #region Slots 
        public int SlotCount
        {
            get
            {
                int count = _slotCount;
                if (count == byte.MaxValue)
                {
                    count = GetSlotCount();
                }

                return count;
            }

            protected set
            {
                _slotCount = (byte)value;
            }
        }

        internal abstract GreenNode GetSlot(int index);

        // for slot counts >= byte.MaxValue
        protected virtual int GetSlotCount()
        {
            return _slotCount;
        }

        public abstract int GetSlotOffset(int index);

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
        [Flags]
        internal enum NodeFlags : ushort
        {
            None = 0,
            ContainsDiagnostics = 1 << 0,
            ContainsStructuredTrivia = 1 << 1,
            ContainsDirectives = 1 << 2,
            ContainsSkippedText = 1 << 3,
            ContainsAnnotations = 1 << 4,
            IsNotMissing = 1 << 5,

            FactoryContextIsInAsync = 1 << 6,
            FactoryContextIsInQuery = 1 << 7,
            FactoryContextIsInIterator = FactoryContextIsInQuery,  // VB does not use "InQuery", but uses "InIterator" instead
            FactoryContextIsInReplace = 1 << 8,

            InheritMask = ContainsDiagnostics | ContainsStructuredTrivia | ContainsDirectives | ContainsSkippedText | ContainsAnnotations | IsNotMissing,
        }

        internal NodeFlags Flags
        {
            get { return this.flags; }
        }

        internal void SetFlags(NodeFlags flags)
        {
            this.flags |= flags;
        }

        internal void ClearFlags(NodeFlags flags)
        {
            this.flags &= ~flags;
        }

        internal bool IsMissing
        {
            get
            {
                // flag has reversed meaning hence "=="
                return (this.flags & NodeFlags.IsNotMissing) == 0;
            }
        }

        internal bool ParsedInAsync
        {
            get
            {
                return (this.flags & NodeFlags.FactoryContextIsInAsync) != 0;
            }
        }

        internal bool ParsedInQuery
        {
            get
            {
                return (this.flags & NodeFlags.FactoryContextIsInQuery) != 0;
            }
        }

        internal bool ParsedInIterator
        {
            get
            {
                return (this.flags & NodeFlags.FactoryContextIsInIterator) != 0;
            }
        }

        public bool ContainsSkippedText
        {
            get
            {
                return (this.flags & NodeFlags.ContainsSkippedText) != 0;
            }
        }

        public bool ContainsStructuredTrivia
        {
            get
            {
                return (this.flags & NodeFlags.ContainsStructuredTrivia) != 0;
            }
        }

        public bool ContainsDirectives
        {
            get
            {
                return (this.flags & NodeFlags.ContainsDirectives) != 0;
            }
        }

        public bool ContainsDiagnostics
        {
            get
            {
                return (this.flags & NodeFlags.ContainsDiagnostics) != 0;
            }
        }

        public bool ContainsAnnotations
        {
            get
            {
                return (this.flags & NodeFlags.ContainsAnnotations) != 0;
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
                this.GetFirstTerminal().GetLeadingTriviaWidth() :
                0;
        }

        public virtual int GetTrailingTriviaWidth()
        {
            return this.FullWidth != 0 ?
                this.GetLastTerminal().GetTrailingTriviaWidth() :
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

        #region Serialization 
        // use high-bit on Kind to identify serialization of extra info
        private const UInt16 ExtendedSerializationInfoMask = unchecked((UInt16)(1u << 15));

        internal GreenNode(ObjectReader reader)
        {
            var kindBits = reader.ReadUInt16();
            _kind = (ushort)(kindBits & ~ExtendedSerializationInfoMask);

            if ((kindBits & ExtendedSerializationInfoMask) != 0)
            {
                var diagnostics = (DiagnosticInfo[])reader.ReadValue();
                if (diagnostics != null && diagnostics.Length > 0)
                {
                    this.flags |= NodeFlags.ContainsDiagnostics;
                    s_diagnosticsTable.Add(this, diagnostics);
                }

                var annotations = (SyntaxAnnotation[])reader.ReadValue();
                if (annotations != null && annotations.Length > 0)
                {
                    this.flags |= NodeFlags.ContainsAnnotations;
                    s_annotationsTable.Add(this, annotations);
                }
            }
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            this.WriteTo(writer);
        }

        internal virtual void WriteTo(ObjectWriter writer)
        {
            var kindBits = (UInt16)_kind;
            var hasDiagnostics = this.GetDiagnostics().Length > 0;
            var hasAnnotations = this.GetAnnotations().Length > 0;

            if (hasDiagnostics || hasAnnotations)
            {
                kindBits |= ExtendedSerializationInfoMask;
            }

            writer.WriteUInt16(kindBits);

            if (hasDiagnostics || hasAnnotations)
            {
                writer.WriteValue(hasDiagnostics ? this.GetDiagnostics() : null);
                writer.WriteValue(hasAnnotations ? this.GetAnnotations() : null);
            }
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            return this.GetReader();
        }

        internal abstract Func<ObjectReader, object> GetReader();
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

        public bool HasAnnotation(SyntaxAnnotation annotation)
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
            if (this.ContainsAnnotations)
            {
                SyntaxAnnotation[] annotations;
                if (s_annotationsTable.TryGetValue(this, out annotations))
                {
                    System.Diagnostics.Debug.Assert(annotations.Length != 0, "we should return nonempty annotations or NoAnnotations");
                    return annotations;
                }
            }

            return s_noAnnotations;
        }

        internal abstract GreenNode SetAnnotations(SyntaxAnnotation[] annotations);

        #endregion

        #region Diagnostics
        internal DiagnosticInfo[] GetDiagnostics()
        {
            if (this.ContainsDiagnostics)
            {
                DiagnosticInfo[] diags;
                if (s_diagnosticsTable.TryGetValue(this, out diags))
                {
                    return diags;
                }
            }

            return s_noDiagnostics;
        }

        internal abstract GreenNode SetDiagnostics(DiagnosticInfo[] diagnostics);
        #endregion

        #region Text
        public abstract string ToFullString();

        public virtual void WriteTo(System.IO.TextWriter writer)
        {
            this.WriteTo(writer, true, true);
        }

        protected internal virtual void WriteTo(System.IO.TextWriter writer, bool leading, bool trailing)
        {
            bool first = true;
            int n = this.SlotCount;
            int lastIndex = n - 1;
            for (; lastIndex >= 0; lastIndex--)
            {
                var child = this.GetSlot(lastIndex);
                if (child != null)
                {
                    break;
                }
            }

            for (var i = 0; i <= lastIndex; i++)
            {
                var child = this.GetSlot(i);
                if (child != null)
                {
                    child.WriteTo(writer, leading | !first, trailing | (i < lastIndex));
                    first = false;
                }
            }
        }

        #endregion

        #region Tokens 
        public virtual int RawContextualKind { get { return this.RawKind; } }
        public virtual object GetValue() { return null; }
        public virtual string GetValueText() { return string.Empty; }
        public virtual GreenNode GetLeadingTriviaCore() { return null; }
        public virtual GreenNode GetTrailingTriviaCore() { return null; }
        public abstract AbstractSyntaxNavigator Navigator { get; }

        public virtual GreenNode WithLeadingTrivia(GreenNode trivia)
        {
            return this;
        }

        public virtual GreenNode WithTrailingTrivia(GreenNode trivia)
        {
            return this;
        }

        internal GreenNode GetFirstTerminal()
        {
            GreenNode node = this;

            do
            {
                GreenNode firstChild = null;
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
            } while (node?._slotCount > 0);

            return node;
        }

        internal GreenNode GetLastTerminal()
        {
            GreenNode node = this;

            do
            {
                GreenNode lastChild = null;
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
            } while (node?._slotCount > 0);

            return node;
        }

        internal GreenNode GetLastNonmissingTerminal()
        {
            GreenNode node = this;

            do
            {
                GreenNode nonmissingChild = null;
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
            while (node?._slotCount > 0);

            return node;
        }
        #endregion

        #region Equivalence 
        public virtual bool IsEquivalentTo(GreenNode other)
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
                    node1 = node1.GetSlot(0);
                }

                if (node2.IsList && node2.SlotCount == 1)
                {
                    node2 = node2.GetSlot(0);
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

        public abstract GreenNode CreateList(IEnumerable<GreenNode> nodes, bool alwaysCreateListNode = false);
        public abstract SyntaxToken CreateSeparator<TNode>(SyntaxNode element) where TNode : SyntaxNode;
        public abstract bool IsTriviaWithEndOfLine(); // trivia node has end of line

        public SyntaxNode CreateRed()
        {
            return CreateRed(null, 0);
        }

        internal abstract SyntaxNode CreateRed(SyntaxNode parent, int position);

        #endregion

        #region Caching


        internal const int MaxCachedChildNum = 3;

        internal bool IsCacheable
        {
            get
            {
                return ((this.flags & NodeFlags.InheritMask) == NodeFlags.IsNotMissing) &&
                    this.SlotCount <= GreenNode.MaxCachedChildNum;
            }
        }

        internal int GetCacheHash()
        {
            Debug.Assert(this.IsCacheable);

            int code = (int)(this.flags) ^ this.RawKind;
            int cnt = this.SlotCount;
            for (int i = 0; i < cnt; i++)
            {
                var child = GetSlot(i);
                if (child != null)
                {
                    code = Hash.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(child), code);
                }
            }

            return code & Int32.MaxValue;
        }

        internal bool IsCacheEquivalent(int kind, NodeFlags flags, GreenNode child1)
        {
            Debug.Assert(this.IsCacheable);

            return this.RawKind == kind &&
                this.flags == flags &&
                this.GetSlot(0) == child1;
        }

        internal bool IsCacheEquivalent(int kind, NodeFlags flags, GreenNode child1, GreenNode child2)
        {
            Debug.Assert(this.IsCacheable);

            return this.RawKind == kind &&
                this.flags == flags &&
                this.GetSlot(0) == child1 &&
                this.GetSlot(1) == child2;
        }

        internal bool IsCacheEquivalent(int kind, NodeFlags flags, GreenNode child1, GreenNode child2, GreenNode child3)
        {
            Debug.Assert(this.IsCacheable);

            return this.RawKind == kind &&
                this.flags == flags &&
                this.GetSlot(0) == child1 &&
                this.GetSlot(1) == child2 &&
                this.GetSlot(2) == child3;
        }
        #endregion //Caching
    }
}
