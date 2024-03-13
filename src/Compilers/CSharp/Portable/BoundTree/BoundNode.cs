// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    internal abstract partial class BoundNode
    {
        private readonly BoundKind _kind;
        private BoundNodeAttributes _attributes;

        /// <summary>
        /// Sequence points permit Syntax to be null.  But all other contexts require a non-null Syntax,
        /// so we annotate it for the majority of uses.
        /// </summary>
        public readonly SyntaxNode Syntax;

        [Flags()]
        private enum BoundNodeAttributes : short
        {
            HasErrors = 1 << 0,
            CompilerGenerated = 1 << 1,
            IsSuppressed = 1 << 2,

            // Bit 3: 1 if the node has maybe-null state, 0 if the node is not null
            // Bits 4 and 5: 01 if the node is not annotated, 10 if the node is annotated, 11 if the node is disabled
            TopLevelFlowStateMaybeNull = 1 << 3,
            TopLevelNotAnnotated = 1 << 4,
            TopLevelAnnotated = 1 << 5,
            TopLevelNone = TopLevelAnnotated | TopLevelNotAnnotated,
            TopLevelAnnotationMask = TopLevelNone,

            /// <summary>
            /// Captures the fact that consumers of the node already checked the state of the WasCompilerGenerated bit.
            /// Allows to assert on attempts to set WasCompilerGenerated bit after that.
            /// </summary>
            WasCompilerGeneratedIsChecked = 1 << 6,
            WasTopLevelNullabilityChecked = 1 << 7,

            /// <summary>
            /// Captures the fact that the node was either converted to some type, or converted to its natural
            /// type.  This is used to check the fact that every rvalue must pass through one of the two,
            /// so that expressions like tuple literals and switch expressions can reliably be rewritten once
            /// the target type is known.
            /// </summary>
            WasConverted = 1 << 8,

            ParamsArrayOrCollection = 1 << 9,

            AttributesPreservedInClone = HasErrors | CompilerGenerated | IsSuppressed | WasConverted | ParamsArrayOrCollection,
        }

        protected new BoundNode MemberwiseClone()
        {
            var result = (BoundNode)base.MemberwiseClone();
            result._attributes &= BoundNodeAttributes.AttributesPreservedInClone;
            return result;
        }

        protected BoundNode(BoundKind kind, SyntaxNode syntax)
        {
            Debug.Assert(
                kind == BoundKind.SequencePoint ||
                kind == BoundKind.SequencePointExpression ||
                kind == (BoundKind)byte.MaxValue || // used in SpillSequenceSpiller
                syntax != null);

            _kind = kind;
            this.Syntax = syntax;
        }

        protected BoundNode(BoundKind kind, SyntaxNode syntax, bool hasErrors)
            : this(kind, syntax)
        {
            if (hasErrors)
            {
                _attributes = BoundNodeAttributes.HasErrors;
            }
        }

        /// <summary>
        /// Determines if a bound node, or associated syntax or type has an error (not a warning) 
        /// diagnostic associated with it.
        /// 
        /// Typically used in the binder as a way to prevent cascading errors. 
        /// In most other cases a more lightweight HasErrors should be used.
        /// </summary>
        public bool HasAnyErrors
        {
            get
            {
                // NOTE: check Syntax rather than WasCompilerGenerated because sequence points can have null syntax.
                if (this.HasErrors || this.Syntax != null && this.Syntax.HasErrors)
                {
                    return true;
                }
                var expression = this as BoundExpression;
                return expression?.Type?.IsErrorType() == true;
            }
        }

        /// <summary>
        /// Determines if a bound node, or any child, grandchild, etc has an error (not warning)
        /// diagnostic associated with it. The HasError bit is initially set for a node by providing it
        /// to the node constructor. If any child nodes of a node have
        /// the HasErrors bit set, then it is automatically set to true on the parent bound node.
        /// 
        /// HasErrors indicates that the tree is not emittable and used to short-circuit lowering/emit stages.
        /// NOTE: not having HasErrors does not guarantee that we do not have any diagnostic associated
        ///       with corresponding syntax or type.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return (_attributes & BoundNodeAttributes.HasErrors) != 0;
            }
            private set
            {
                if (value)
                {
                    _attributes |= BoundNodeAttributes.HasErrors;
                }
                else
                {
                    Debug.Assert((_attributes & BoundNodeAttributes.HasErrors) == 0,
                        "HasErrors flag should not be reset here");
                }
            }
        }

        public SyntaxTree? SyntaxTree
        {
            get
            {
                return Syntax?.SyntaxTree;
            }
        }

        protected void CopyAttributes(BoundNode original)
        {
            this.WasCompilerGenerated = original.WasCompilerGenerated;

            Debug.Assert(original is BoundExpression || !original.IsSuppressed);
            this.IsSuppressed = original.IsSuppressed;

            if (original.IsParamsArrayOrCollection)
            {
                this.IsParamsArrayOrCollection = true;
            }

#if DEBUG
            this.WasConverted = original.WasConverted;
#endif
        }

        /// <remarks>
        /// NOTE: not generally set in rewriters.
        /// </remarks>
        public bool WasCompilerGenerated
        {
            get
            {
#if DEBUG
                _attributes |= BoundNodeAttributes.WasCompilerGeneratedIsChecked;
#endif
                return (_attributes & BoundNodeAttributes.CompilerGenerated) != 0;
            }
            internal set
            {
#if DEBUG
                Debug.Assert((_attributes & BoundNodeAttributes.WasCompilerGeneratedIsChecked) == 0,
                    "compiler generated flag should not be set after reading it");
#endif

                if (value)
                {
                    _attributes |= BoundNodeAttributes.CompilerGenerated;
                }
                else
                {
                    Debug.Assert((_attributes & BoundNodeAttributes.CompilerGenerated) == 0,
                        "compiler generated flag should not be reset here");
                }
            }
        }

        // PERF: it is very uncommon for a flag being forcibly reset 
        //       so we do not support it in general (making the commonly used implementation simpler) 
        //       and instead have a special method to do resetting.
        public void ResetCompilerGenerated(bool newCompilerGenerated)
        {
#if DEBUG
            Debug.Assert((_attributes & BoundNodeAttributes.WasCompilerGeneratedIsChecked) == 0,
                "compiler generated flag should not be set after reading it");
#endif
            if (newCompilerGenerated)
            {
                _attributes |= BoundNodeAttributes.CompilerGenerated;
            }
            else
            {
                _attributes &= ~BoundNodeAttributes.CompilerGenerated;
            }
        }

        /// <summary>
        /// Top level nullability for the node. This should not be used by flow analysis.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        protected NullabilityInfo TopLevelNullability
        {
            get
            {
#if DEBUG
                _attributes |= BoundNodeAttributes.WasTopLevelNullabilityChecked;
#endif

                // This is broken out into a separate property so the debugger can display the
                // top level nullability without setting the _attributes flag and interfering
                // with the normal operation of tests.
                return TopLevelNullabilityCore;
            }
            set
            {
#if DEBUG
                Debug.Assert((_attributes & BoundNodeAttributes.WasTopLevelNullabilityChecked) == 0,
                    "bound node nullability should not be set after reading it");
#endif
                _attributes &= ~(BoundNodeAttributes.TopLevelAnnotationMask | BoundNodeAttributes.TopLevelFlowStateMaybeNull);

                _attributes |= value.Annotation switch
                {
                    CodeAnalysis.NullableAnnotation.Annotated => BoundNodeAttributes.TopLevelAnnotated,
                    CodeAnalysis.NullableAnnotation.NotAnnotated => BoundNodeAttributes.TopLevelNotAnnotated,
                    CodeAnalysis.NullableAnnotation.None => BoundNodeAttributes.TopLevelNone,
                    var a => throw ExceptionUtilities.UnexpectedValue(a),
                };

                switch (value.FlowState)
                {
                    case CodeAnalysis.NullableFlowState.MaybeNull:
                        _attributes |= BoundNodeAttributes.TopLevelFlowStateMaybeNull;
                        break;

                    case CodeAnalysis.NullableFlowState.NotNull:
                        // Not needed: unset is NotNull
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(value.FlowState);
                }
            }
        }

        /// <summary>
        /// This is for debugger display use only: <see cref="TopLevelNullability"/> will set the BoundNodeAttributes.WasTopLevelNullabilityChecked
        /// bit in the boundnode properties, which will break debugging. This allows the debugger to display the current value without setting the bit.
        /// </summary>
        private NullabilityInfo TopLevelNullabilityCore
        {
            get
            {
                if ((_attributes & BoundNodeAttributes.TopLevelAnnotationMask) == 0)
                {
                    return default;
                }

                var annotation = (_attributes & BoundNodeAttributes.TopLevelAnnotationMask) switch
                {
                    BoundNodeAttributes.TopLevelAnnotated => CodeAnalysis.NullableAnnotation.Annotated,
                    BoundNodeAttributes.TopLevelNotAnnotated => CodeAnalysis.NullableAnnotation.NotAnnotated,
                    BoundNodeAttributes.TopLevelNone => CodeAnalysis.NullableAnnotation.None,
                    var mask => throw ExceptionUtilities.UnexpectedValue(mask)
                };

                var flowState = (_attributes & BoundNodeAttributes.TopLevelFlowStateMaybeNull) == 0 ? CodeAnalysis.NullableFlowState.NotNull : CodeAnalysis.NullableFlowState.MaybeNull;

                return new NullabilityInfo(annotation, flowState);
            }
        }

        public bool IsSuppressed
        {
            get
            {
                return (_attributes & BoundNodeAttributes.IsSuppressed) != 0;
            }
            protected set
            {
                Debug.Assert((_attributes & BoundNodeAttributes.IsSuppressed) == 0, "flag should not be set twice or reset");
                if (value)
                {
                    _attributes |= BoundNodeAttributes.IsSuppressed;
                }
            }
        }

#if DEBUG
        /// <summary>
        /// WasConverted flag is used for debugging purposes only (not to direct the behavior of semantic analysis).
        /// It is used on BoundLocal and BoundParameter to check that every such rvalue that has not been converted to
        /// some type has been converted to its natural type.
        /// </summary>
        public bool WasConverted
        {
            get
            {
                return (_attributes & BoundNodeAttributes.WasConverted) != 0;
            }
            protected set
            {
                Debug.Assert((_attributes & BoundNodeAttributes.WasConverted) == 0, "WasConverted flag should not be set twice or reset");
                if (value)
                {
                    _attributes |= BoundNodeAttributes.WasConverted;
                }
            }
        }
#endif

        public bool IsParamsArrayOrCollection
        {
            get
            {
                return (_attributes & BoundNodeAttributes.ParamsArrayOrCollection) != 0;
            }
            protected set
            {
                Debug.Assert((_attributes & BoundNodeAttributes.ParamsArrayOrCollection) == 0, $"{nameof(BoundNodeAttributes.ParamsArrayOrCollection)} flag should not be set twice or reset");
                Debug.Assert(value || !IsParamsArrayOrCollection);
                Debug.Assert(!value ||
                             this is BoundArrayCreation { Bounds: [BoundLiteral { WasCompilerGenerated: true }], InitializerOpt: BoundArrayInitialization { WasCompilerGenerated: true }, WasCompilerGenerated: true } or
                                     BoundUnconvertedCollectionExpression { WasCompilerGenerated: true } or
                                     BoundCollectionExpression { WasCompilerGenerated: true, UnconvertedCollectionExpression.IsParamsArrayOrCollection: true } or
                                     BoundConversion { Operand: BoundCollectionExpression { IsParamsArrayOrCollection: true } });
                Debug.Assert(!value ||
                             this is not BoundUnconvertedCollectionExpression collection ||
                             ImmutableArray<BoundNode>.CastUp(collection.Elements.CastArray<BoundExpression>()) == collection.Elements);

                if (value)
                {
                    _attributes |= BoundNodeAttributes.ParamsArrayOrCollection;
                }
            }
        }

        public BoundKind Kind
        {
            get
            {
                return _kind;
            }
        }

        public virtual BoundNode? Accept(BoundTreeVisitor visitor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Return a clone of the current node with the HasErrors flag set.
        /// </summary>
        internal BoundNode WithHasErrors()
        {
            if (this.HasErrors)
                return this;

            BoundNode clone = MemberwiseClone();
            clone.HasErrors = true;
            return clone;
        }

#if DEBUG
        private class MyTreeDumper : TreeDumper
        {
            private MyTreeDumper() : base() { }

            public static new string DumpCompact(TreeDumperNode root)
            {
                return new MyTreeDumper().DoDumpCompact(root);
            }

            protected override string DumperString(object o)
            {
                return (o is SynthesizedLocal l) ? l.DumperString() : base.DumperString(o);
            }
        }

        internal virtual string Dump()
        {
            return MyTreeDumper.DumpCompact(BoundTreeDumperNodeProducer.MakeTree(this));
        }
#endif

        internal string GetDebuggerDisplay()
        {
            var result = GetType().Name;
            if (Syntax != null)
            {
                result += " " + Syntax.ToString();
            }
            return result;
        }

        [Conditional("DEBUG")]
        public void CheckLocalsDefined()
        {
#if DEBUG
            LocalsScanner.CheckLocalsDefined(this);
#endif
        }

        public static Conversion GetConversion(BoundExpression? conversion, BoundValuePlaceholder? placeholder)
        {
            switch (conversion)
            {
                case null:
                    return Conversion.NoConversion;

                case BoundConversion boundConversion:

                    if ((object)boundConversion.Operand == placeholder)
                    {
                        return boundConversion.Conversion;
                    }

                    if (!boundConversion.Conversion.IsUserDefined)
                    {
                        boundConversion = (BoundConversion)boundConversion.Operand;
                    }

                    if (boundConversion.Conversion.IsUserDefined)
                    {
                        BoundConversion next;

                        if ((object)boundConversion.Operand == placeholder ||
                            (object)(next = (BoundConversion)boundConversion.Operand).Operand == placeholder ||
                            (object)((BoundConversion)next.Operand).Operand == placeholder)
                        {
                            return boundConversion.Conversion;
                        }
                    }

                    goto default;

                case BoundValuePlaceholder valuePlaceholder when (object)valuePlaceholder == placeholder:
                    return Conversion.Identity;

                default:
                    throw ExceptionUtilities.UnexpectedValue(conversion);
            }
        }

#if DEBUG
        private class LocalsScanner : BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator
        {
            public readonly PooledHashSet<LocalSymbol> DeclaredLocals = PooledHashSet<LocalSymbol>.GetInstance();

            private LocalsScanner()
            {
            }

            public static void CheckLocalsDefined(BoundNode root)
            {
                var localsScanner = new LocalsScanner();
                localsScanner.Visit(root);
                localsScanner.Free();
            }

            private void AddAll(ImmutableArray<LocalSymbol> locals)
            {
                foreach (var local in locals)
                {
                    if (!DeclaredLocals.Add(local))
                    {
                        Debug.Assert(false, "duplicate local " + local.GetDebuggerDisplay());
                    }
                }
            }

            private void RemoveAll(ImmutableArray<LocalSymbol> locals)
            {
                foreach (var local in locals)
                {
                    if (!DeclaredLocals.Remove(local))
                    {
                        Debug.Assert(false, "missing local " + local.GetDebuggerDisplay());
                    }
                }
            }

            private void CheckDeclared(LocalSymbol local)
            {
                if (!DeclaredLocals.Contains(local))
                {
                    Debug.Assert(false, "undeclared local " + local.GetDebuggerDisplay());
                }
            }

            public override BoundNode? VisitFieldEqualsValue(BoundFieldEqualsValue node)
            {
                AddAll(node.Locals);
                base.VisitFieldEqualsValue(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitPropertyEqualsValue(BoundPropertyEqualsValue node)
            {
                AddAll(node.Locals);
                base.VisitPropertyEqualsValue(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitParameterEqualsValue(BoundParameterEqualsValue node)
            {
                AddAll(node.Locals);
                base.VisitParameterEqualsValue(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitBlock(BoundBlock node)
            {
                var instrumentation = node.Instrumentation;
                if (instrumentation != null)
                {
                    foreach (var local in instrumentation.Locals)
                    {
                        var added = DeclaredLocals.Add(local);
                        Debug.Assert(added);
                    }

                    _ = Visit(instrumentation.Prologue);
                }

                AddAll(node.Locals);
                base.VisitBlock(node);
                RemoveAll(node.Locals);

                if (instrumentation != null)
                {
                    _ = Visit(instrumentation.Epilogue);

                    foreach (var local in instrumentation.Locals)
                    {
                        var removed = DeclaredLocals.Remove(local);
                        Debug.Assert(removed);
                    }
                }

                return null;
            }

            public override BoundNode? VisitLocalDeclaration(BoundLocalDeclaration node)
            {
                CheckDeclared(node.LocalSymbol);
                base.VisitLocalDeclaration(node);
                return null;
            }

            public override BoundNode? VisitSequence(BoundSequence node)
            {
                AddAll(node.Locals);
                base.VisitSequence(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitSpillSequence(BoundSpillSequence node)
            {
                AddAll(node.Locals);
                base.VisitSpillSequence(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitSwitchStatement(BoundSwitchStatement node)
            {
                AddAll(node.InnerLocals);
                base.VisitSwitchStatement(node);
                RemoveAll(node.InnerLocals);
                return null;
            }

            public override BoundNode? VisitSwitchExpressionArm(BoundSwitchExpressionArm node)
            {
                AddAll(node.Locals);
                base.VisitSwitchExpressionArm(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitSwitchSection(BoundSwitchSection node)
            {
                AddAll(node.Locals);
                base.VisitSwitchSection(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitDoStatement(BoundDoStatement node)
            {
                AddAll(node.Locals);
                base.VisitDoStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitWhileStatement(BoundWhileStatement node)
            {
                AddAll(node.Locals);
                base.VisitWhileStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitForStatement(BoundForStatement node)
            {
                AddAll(node.OuterLocals);
                this.Visit(node.Initializer);
                AddAll(node.InnerLocals);
                this.Visit(node.Condition);
                this.Visit(node.Increment);
                this.Visit(node.Body);
                RemoveAll(node.InnerLocals);
                RemoveAll(node.OuterLocals);
                return null;
            }

            public override BoundNode? VisitForEachStatement(BoundForEachStatement node)
            {
                AddAll(node.IterationVariables);
                base.VisitForEachStatement(node);
                RemoveAll(node.IterationVariables);
                return null;
            }

            public override BoundNode? VisitUsingStatement(BoundUsingStatement node)
            {
                AddAll(node.Locals);
                base.VisitUsingStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitIfStatement(BoundIfStatement node)
            {
                while (true)
                {
                    Visit(node.Condition);
                    Visit(node.Consequence);
                    var alternative = node.AlternativeOpt;
                    if (alternative is null)
                    {
                        break;
                    }
                    if (alternative is BoundIfStatement elseIfStatement)
                    {
                        node = elseIfStatement;
                    }
                    else
                    {
                        Visit(alternative);
                        break;
                    }
                }
                return null;
            }

            public override BoundNode? VisitFixedStatement(BoundFixedStatement node)
            {
                AddAll(node.Locals);
                base.VisitFixedStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitCatchBlock(BoundCatchBlock node)
            {
                AddAll(node.Locals);
                base.VisitCatchBlock(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode? VisitLocal(BoundLocal node)
            {
                CheckDeclared(node.LocalSymbol);
                base.VisitLocal(node);
                return null;
            }

            public override BoundNode? VisitPseudoVariable(BoundPseudoVariable node)
            {
                CheckDeclared(node.LocalSymbol);
                base.VisitPseudoVariable(node);
                return null;
            }

            public override BoundNode? VisitConstructorMethodBody(BoundConstructorMethodBody node)
            {
                AddAll(node.Locals);
                base.VisitConstructorMethodBody(node);
                RemoveAll(node.Locals);
                return null;
            }

            public void Free()
            {
                DeclaredLocals.Free();
            }
        }
#endif
    }
}
