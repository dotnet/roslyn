// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// The decision tree is a transient data structure used during initial binding to compute which
    /// cases in a switch are subsumed by previous cases, and in lowering to help produce the lowered
    /// form.
    /// </summary>
    internal abstract class DecisionTree
    {
        public readonly BoundExpression Expression;
        public readonly TypeSymbol Type;
        public LocalSymbol Temp;
        public bool MatchIsComplete;

        public enum DecisionKind { ByType, ByValue, Guarded }
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
            Debug.Assert(this.Type != null);
        }

        public static DecisionTree Create(BoundExpression expression, TypeSymbol type, Symbol enclosingSymbol)
        {
            Debug.Assert(expression.Type == type);
            LocalSymbol temp = null;
            if (expression.ConstantValue == null)
            {
                // Unless it is a constant, the decision tree acts on a copy of the input expression.
                // We create a temp to represent that copy. Lowering will assign into this temp.
                temp = new SynthesizedLocal(enclosingSymbol as MethodSymbol, type, SynthesizedLocalKind.PatternMatchingTemp, expression.Syntax, false, RefKind.None);
                expression = new BoundLocal(expression.Syntax, temp, null, type);
            }

            if (expression.Type.CanBeAssignedNull())
            {
                // We need the ByType decision tree to separate null from non-null values.
                // Note that, for the purpose of the decision tree (and subsumption), we
                // ignore the fact that the input may be a constant, and therefore always
                // or never null.
                return new ByType(expression, type, temp);
            }
            else
            {
                // If it is a (e.g. builtin) value type, we can switch on its (constant) values.
                // If it isn't a builtin, in practice we will only use the Default part of the
                // ByValue.
                return new ByValue(expression, type, temp);
            }
        }

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

        public class ByValue : DecisionTree
        {
            public readonly Dictionary<object, DecisionTree> ValueAndDecision =
                new Dictionary<object, DecisionTree>();
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

        public class Guarded : DecisionTree
        {
            // A sequence of bindings to be assigned before evaluation of the guard or jump to the label.
            // Each one contains the source of the assignment and the destination of the assignment, in that order.
            public readonly ImmutableArray<KeyValuePair<BoundExpression, BoundExpression>> Bindings;
            public readonly SyntaxNode SectionSyntax;
            public readonly BoundExpression Guard;
            public readonly BoundPatternSwitchLabel Label;
            public DecisionTree Default = null; // decision tree to use if the Guard is false
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
