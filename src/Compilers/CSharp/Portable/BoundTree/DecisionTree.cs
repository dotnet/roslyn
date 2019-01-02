// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The decision tree is a transient data structure used during initial binding to compute which
    /// cases in a switch are subsumed by previous cases, and in lowering to help produce the lowered
    /// form.
    /// </summary>
    internal abstract class DecisionTree
    {
        /// <summary>
        /// The input expression to this branch of the decision tree.
        /// </summary>
        public readonly BoundExpression Expression;

        /// <summary>
        /// The type of the input at this branch of the decision tree.
        /// </summary>
        public readonly TypeSymbol Type;

        /// <summary>
        /// A temporary variable that is holding the computed input at this branch.
        /// </summary>
        public LocalSymbol Temp;

        /// <summary>
        /// True if this decision tree fully handles all possible values of its input.
        /// </summary>
        public bool MatchIsComplete;

        /// <summary>
        /// The three different kinds of nodes in the decision tree.
        /// </summary>
        public enum DecisionKind
        {
            /// <summary>
            /// For the type <see cref="DecisionTree.ByType"/>
            /// </summary>
            ByType,

            /// <summary>
            /// For the type <see cref="DecisionTree.ByValue"/>
            /// </summary>
            ByValue,

            /// <summary>
            /// For the type <see cref="DecisionTree.Guarded"/>
            /// </summary>
            Guarded
        }

        /// <summary>
        /// The kind of this node in the decision tree.
        /// </summary>
        public abstract DecisionKind Kind { get; }

#if DEBUG
        internal string Dump()
        {
            var builder = new StringBuilder();
            DumpInternal(builder, "");
            return builder.ToString();
        }
        internal abstract void DumpInternal(StringBuilder builder, string indent);
#endif
        public DecisionTree(BoundExpression expression, TypeSymbol type, LocalSymbol temp)
        {
            this.Expression = expression;
            this.Type = type;
            this.Temp = temp;
            Debug.Assert(this.Expression != null);
            Debug.Assert((object)this.Type != null);
        }

        /// <summary>
        /// A decision tree node that branches based on (1) whether the input value is null, (2) the runtime
        /// type of the input expression, and finally (3) a default decision tree if nothing in the previous
        /// cases handles the input.
        /// </summary>
        public class ByType : DecisionTree
        {
            public DecisionTree WhenNull;
            public readonly ArrayBuilder<KeyValuePair<TypeSymbol, DecisionTree>> TypeAndDecision =
                new ArrayBuilder<KeyValuePair<TypeSymbol, DecisionTree>>();
            public DecisionTree Default;
            public override DecisionKind Kind => DecisionKind.ByType;
            public ByType(BoundExpression expression, TypeSymbol type, LocalSymbol temp) : base(expression, type, temp) { }
#if DEBUG
            internal override void DumpInternal(StringBuilder builder, string indent)
            {
                builder.AppendLine($"{indent}ByType");
                if (WhenNull != null)
                {
                    builder.AppendLine($"{indent}  null");
                    WhenNull.DumpInternal(builder, indent + "    ");
                }

                foreach (var kv in TypeAndDecision)
                {
                    builder.AppendLine($"{indent}  {kv.Key}");
                    kv.Value.DumpInternal(builder, indent + "    ");
                }

                if (Default != null)
                {
                    builder.AppendLine($"{indent}  default");
                    Default.DumpInternal(builder, indent + "    ");
                }
            }
#endif
        }

        /// <summary>
        /// A decision tree that, given a non-null input of a type, dispatches based on the
        /// value of that type. The <see cref="ValueAndDecision"/> map will be empty unless the type is a
        /// built-in type because other types do not have constant values. In that case only the
        /// <see cref="Default"/> part of the decision tree is used.
        /// </summary>
        public class ByValue : DecisionTree
        {
            /// <summary>
            /// A map from the constant value to the decision that should be taken when the input has that value.
            /// </summary>
            public readonly Dictionary<object, DecisionTree> ValueAndDecision =
                new Dictionary<object, DecisionTree>();
            /// <summary>
            /// The default decision if no value matches, or the matched value's decision doesn't handle all inputs.
            /// </summary>
            public DecisionTree Default;
            public override DecisionKind Kind => DecisionKind.ByValue;
            public ByValue(BoundExpression expression, TypeSymbol type, LocalSymbol temp) : base(expression, type, temp) { }
#if DEBUG
            internal override void DumpInternal(StringBuilder builder, string indent)
            {
                builder.AppendLine($"{indent}ByValue");
                foreach (var kv in ValueAndDecision)
                {
                    builder.AppendLine($"{indent}  {kv.Key}");
                    kv.Value.DumpInternal(builder, indent + "    ");
                }

                if (Default != null)
                {
                    builder.AppendLine($"{indent}  default");
                    Default.DumpInternal(builder, indent + "    ");
                }
            }
#endif
        }

        /// <summary>
        /// A guarded decision tree, which simply binds a set of variables (this is used to assign to the
        /// pattern variables of the switch case), optionally evaluates a Guard expression (which corresponds
        /// to the <c>when</c> expression of a switch case), and the branches to a given label if the guard
        /// is true (or there is no guard).
        /// </summary>
        public class Guarded : DecisionTree
        {
            /// <summary>
            /// A sequence of bindings to be assigned before evaluation of the guard or jump to the label.
            /// Each one contains the source of the assignment and the destination of the assignment, in that order.
            /// </summary>
            public readonly ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> Bindings;
            /// <summary>
            /// The syntax node corresponding to the switch section.
            /// </summary>
            public readonly SyntaxNode SectionSyntax;
            /// <summary>
            /// The (optional) guard expression.
            /// </summary>
            public readonly BoundExpression Guard;
            /// <summary>
            /// The label to jump to if the guard is true (or there is no guard).
            /// </summary>
            public readonly BoundPatternSwitchLabel Label;
            /// <summary>
            /// The decision tree to use if the Guard evaluates to false.
            /// </summary>
            public DecisionTree Default = null;
            public override DecisionKind Kind => DecisionKind.Guarded;
            public Guarded(
                BoundExpression expression,
                TypeSymbol type,
                ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> bindings,
                SyntaxNode sectionSyntax,
                BoundExpression guard,
                BoundPatternSwitchLabel label)
                : base(expression, type, null)
            {
                this.Guard = guard;
                this.Label = label;
                this.Bindings = bindings;
                this.SectionSyntax = sectionSyntax;
                Debug.Assert(guard?.ConstantValue != ConstantValue.False);
                base.MatchIsComplete =
                    (guard == null) || (guard.ConstantValue == ConstantValue.True);
            }
#if DEBUG
            internal override void DumpInternal(StringBuilder builder, string indent)
            {
                builder.Append($"{indent}Guarded");
                if (Guard != null)
                {
                    builder.Append($" guard={Guard.Syntax.ToString()}");
                }

                builder.AppendLine($" label={Label.Syntax.ToString()}");
            }
#endif
        }
    }
}
