// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class BoundNode
    {
        private readonly BoundKind kind;
        private BoundNodeAttributes attributes;

        public readonly CSharpSyntaxNode Syntax;

        [Flags()]
        private enum BoundNodeAttributes : byte
        {
            HasErrors = 1 << 0,
            CompilerGenerated = 1 << 1,
#if DEBUG
            /// <summary>
            /// Captures the fact that consumers of the node already checked the state of the WasCompilerGenerated bit.
            /// Allows to assert on attempts to set WasCompilerGenerated bit after that.
            /// </summary>
            WasCompilerGeneratedIsChecked = 1 << 2,
#endif
        }

        protected BoundNode(BoundKind kind, CSharpSyntaxNode syntax)
        {
            Debug.Assert(kind == BoundKind.SequencePoint || syntax != null);

            this.kind = kind;
            this.Syntax = syntax;
        }

        protected BoundNode(BoundKind kind, CSharpSyntaxNode syntax, bool hasErrors) :
            this(kind, syntax)
        {
            if (hasErrors)
            {
                this.attributes = BoundNodeAttributes.HasErrors;
            }
        }

        /// <summary>
        /// Determines if a bound node, or associated syntax or type has an error (not a waring) 
        /// diagnostic associated with it.
        /// 
        /// Typically used in the binder as a way to prevent cascading errors. 
        /// In most other cases a more lightweigth HasErrors should be used.
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
        /// HasErrors indicates that the tree is not emittable and used to shortcircuit lowering/emit stages.
        /// NOTE: not having HasErrors does not guarantee that we do not have any diagnostic associated
        ///       with corresponding syntax or type.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return (attributes & BoundNodeAttributes.HasErrors) != 0;
            }
        }

        public SyntaxTree SyntaxTree
        {
            get
            {
                var syntax = Syntax;
                return syntax == null ? null : syntax.SyntaxTree;
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
                attributes |= BoundNodeAttributes.WasCompilerGeneratedIsChecked;
#endif
                return (attributes & BoundNodeAttributes.CompilerGenerated) != 0;
            }
            internal set
            {
#if DEBUG
                Debug.Assert((attributes & BoundNodeAttributes.WasCompilerGeneratedIsChecked) == 0,
                    "compiler generated flag should not be set after reading it");
#endif

                if (value)
                {
                    attributes |= BoundNodeAttributes.CompilerGenerated;
                }
                else
                {
                    Debug.Assert((attributes & BoundNodeAttributes.CompilerGenerated) == 0,
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
            Debug.Assert((attributes & BoundNodeAttributes.WasCompilerGeneratedIsChecked) == 0,
                "compiler generated flag should not be set after reading it");
#endif
            if (newCompilerGenerated)
            {
                attributes |= BoundNodeAttributes.CompilerGenerated;
            }
            else
            {
                attributes &= ~BoundNodeAttributes.CompilerGenerated;
            }
        }


        public BoundKind Kind
        {
            get
            {
                return this.kind;
            }
        }

        public virtual BoundNode Accept(BoundTreeVisitor visitor)
        {
            throw new NotImplementedException();
        }

#if DEBUG
        internal virtual string Dump()
        {
            return TreeDumper.DumpCompact(BoundTreeDumperNodeProducer.MakeTree(this));
        }
#endif
    }
}