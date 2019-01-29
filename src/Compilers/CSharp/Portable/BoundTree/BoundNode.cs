// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public readonly SyntaxNode Syntax;

        [Flags()]
        private enum BoundNodeAttributes : byte
        {
            HasErrors = 0b0001,
            CompilerGenerated = 0b0010,
            TopLevelNullableUnset = 0b0000,
            TopLevelNullable = 0b0100,
            TopLevelNonNullable = 0b1000,
            TopLevelUnknown = 0b1100,
            TopLevelNullabilityMask = 0b1100,
#if DEBUG
            /// <summary>
            /// Captures the fact that consumers of the node already checked the state of the WasCompilerGenerated bit.
            /// Allows to assert on attempts to set WasCompilerGenerated bit after that.
            /// </summary>
            WasCompilerGeneratedIsChecked = 0b010000,
            WasTopLevelNullabilityChecked = 0b100000
#endif
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
                return expression != null && !ReferenceEquals(expression.Type, null) && expression.Type.IsErrorType();
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
        }

        public SyntaxTree SyntaxTree
        {
            get
            {
                return Syntax?.SyntaxTree;
            }
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
        /// Top level nullability for the node. This should not be used by flow analysis: it
        /// is lossy, and loses the distinction between NotAnnotated and NotNullable.
        /// </summary>
        protected NullableAnnotation TopLevelNullability
        {
            get
            {
#if DEBUG
                _attributes |= BoundNodeAttributes.WasTopLevelNullabilityChecked;
#endif
                switch (_attributes & BoundNodeAttributes.TopLevelNullabilityMask)
                {
                    case BoundNodeAttributes.TopLevelNullable:
                        return NullableAnnotation.Nullable;

                    case BoundNodeAttributes.TopLevelNonNullable:
                        return NullableAnnotation.NotNullable;

                    case BoundNodeAttributes.TopLevelUnknown:
                        return NullableAnnotation.Unknown;

                    case BoundNodeAttributes.TopLevelNullableUnset:
                        return NullableAnnotation.NotComputed;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(_attributes);
                }
            }
            set
            {
#if DEBUG
                Debug.Assert((_attributes & BoundNodeAttributes.WasTopLevelNullabilityChecked) == 0,
                    "bound node nullability should not be set after reading it");
#endif
                // TODO: Will likely need to handle generics specially in this conversion
                _attributes &= ~BoundNodeAttributes.TopLevelNullabilityMask;
                switch (value)
                {
                    case NullableAnnotation.Annotated:
                    case NullableAnnotation.Nullable:
                        _attributes |= BoundNodeAttributes.TopLevelNullable;
                        break;

                    case NullableAnnotation.NotAnnotated:
                    case NullableAnnotation.NotNullable:
                        _attributes |= BoundNodeAttributes.TopLevelNonNullable;
                        break;

                    case NullableAnnotation.Unknown:
                        _attributes |= BoundNodeAttributes.TopLevelUnknown;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(value);
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

        public virtual BoundNode Accept(BoundTreeVisitor visitor)
        {
            throw new NotImplementedException();
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

            public override BoundNode VisitFieldEqualsValue(BoundFieldEqualsValue node)
            {
                AddAll(node.Locals);
                base.VisitFieldEqualsValue(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitPropertyEqualsValue(BoundPropertyEqualsValue node)
            {
                AddAll(node.Locals);
                base.VisitPropertyEqualsValue(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitParameterEqualsValue(BoundParameterEqualsValue node)
            {
                AddAll(node.Locals);
                base.VisitParameterEqualsValue(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitBlock(BoundBlock node)
            {
                AddAll(node.Locals);
                base.VisitBlock(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitLocalDeclaration(BoundLocalDeclaration node)
            {
                CheckDeclared(node.LocalSymbol);
                base.VisitLocalDeclaration(node);
                return null;
            }

            public override BoundNode VisitSequence(BoundSequence node)
            {
                AddAll(node.Locals);
                base.VisitSequence(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitSpillSequence(BoundSpillSequence node)
            {
                AddAll(node.Locals);
                base.VisitSpillSequence(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitSwitchStatement(BoundSwitchStatement node)
            {
                AddAll(node.InnerLocals);
                base.VisitSwitchStatement(node);
                RemoveAll(node.InnerLocals);
                return null;
            }

            public override BoundNode VisitSwitchExpressionArm(BoundSwitchExpressionArm node)
            {
                AddAll(node.Locals);
                base.VisitSwitchExpressionArm(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitSwitchSection(BoundSwitchSection node)
            {
                AddAll(node.Locals);
                base.VisitSwitchSection(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitDoStatement(BoundDoStatement node)
            {
                AddAll(node.Locals);
                base.VisitDoStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitWhileStatement(BoundWhileStatement node)
            {
                AddAll(node.Locals);
                base.VisitWhileStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitForStatement(BoundForStatement node)
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

            public override BoundNode VisitForEachStatement(BoundForEachStatement node)
            {
                AddAll(node.IterationVariables);
                base.VisitForEachStatement(node);
                RemoveAll(node.IterationVariables);
                return null;
            }

            public override BoundNode VisitUsingStatement(BoundUsingStatement node)
            {
                AddAll(node.Locals);
                base.VisitUsingStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitFixedStatement(BoundFixedStatement node)
            {
                AddAll(node.Locals);
                base.VisitFixedStatement(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitCatchBlock(BoundCatchBlock node)
            {
                AddAll(node.Locals);
                base.VisitCatchBlock(node);
                RemoveAll(node.Locals);
                return null;
            }

            public override BoundNode VisitLocal(BoundLocal node)
            {
                CheckDeclared(node.LocalSymbol);
                base.VisitLocal(node);
                return null;
            }

            public override BoundNode VisitPseudoVariable(BoundPseudoVariable node)
            {
                CheckDeclared(node.LocalSymbol);
                base.VisitPseudoVariable(node);
                return null;
            }

            public override BoundNode VisitConstructorMethodBody(BoundConstructorMethodBody node)
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
