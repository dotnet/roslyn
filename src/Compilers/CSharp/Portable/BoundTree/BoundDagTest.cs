// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
#if DEBUG
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
#endif
    partial class BoundDagTest
    {
        public override bool Equals([NotNullWhen(true)] object? obj) => this.Equals(obj as BoundDagTest);

        private bool Equals(BoundDagTest? other)
        {
            if (other is null || this.Kind != other.Kind)
                return false;
            if (this == other)
                return true;
            if (!this.Input.Equals(other.Input))
                return false;

            switch (this, other)
            {
                case (BoundDagTypeTest x, BoundDagTypeTest y):
                    return x.Type.Equals(y.Type, TypeCompareKind.AllIgnoreOptions);
                case (BoundDagNonNullTest x, BoundDagNonNullTest y):
                    return x.IsExplicitTest == y.IsExplicitTest;
                case (BoundDagExplicitNullTest x, BoundDagExplicitNullTest y):
                    return true;
                case (BoundDagValueTest x, BoundDagValueTest y):
                    return x.Value.Equals(y.Value);
                case (BoundDagRelationalTest x, BoundDagRelationalTest y):
                    return x.Relation == y.Relation && x.Value.Equals(y.Value);
                default:
                    throw ExceptionUtilities.UnexpectedValue(this);
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(((int)Kind).GetHashCode(), Input.GetHashCode());
        }

        public BoundDagTest Update(BoundDagTemp input) => UpdateTestImpl(input);
        public abstract BoundDagTest UpdateTestImpl(BoundDagTemp input);

        public virtual OneOrMany<BoundDagTemp> AllInputs()
        {
            return new OneOrMany<BoundDagTemp>(Input);
        }

#if DEBUG
        internal new string GetDebuggerDisplay()
        {
            switch (this)
            {
                case BoundDagTypeEvaluation a:
                    return $"{a.GetOutputTempDebuggerDisplay()} = ({a.Type}){a.Input.GetDebuggerDisplay()}";
                case BoundDagPropertyEvaluation e:
                    return $"{e.GetOutputTempDebuggerDisplay()} = {e.Input.GetDebuggerDisplay()}.{e.Property.Name}";
                case BoundDagFieldEvaluation e:
                    return $"{e.GetOutputTempDebuggerDisplay()} = {e.Input.GetDebuggerDisplay()}.{e.Field.Name}";
                case BoundDagDeconstructEvaluation d:
                    var result = "(";
                    var first = true;
                    foreach (var param in d.DeconstructMethod.Parameters)
                    {
                        if (!first)
                        {
                            result += ", ";
                        }
                        first = false;
                        result += $"Item{param.Ordinal + 1}";
                    }
                    result += $") {d.GetOutputTempDebuggerDisplay()} = {d.Input.GetDebuggerDisplay()}";
                    return result;
                case BoundDagIndexEvaluation i:
                    return $"{i.GetOutputTempDebuggerDisplay()} = {i.Input.GetDebuggerDisplay()}[{i.Index}]";
                case BoundDagIndexerEvaluation i:
                    return $"{i.GetOutputTempDebuggerDisplay()} = {i.Input.GetDebuggerDisplay()}[{i.Index}]";
                case BoundDagAssignmentEvaluation i:
                    return $"{i.Target.GetDebuggerDisplay()} <-- {i.Input.GetDebuggerDisplay()}";
                case BoundDagEvaluation e:
                    return $"{e.GetOutputTempDebuggerDisplay()} = {e.Kind}({e.Input.GetDebuggerDisplay()})";
                case BoundDagTypeTest b:
                    var typeName = b.Type.TypeKind == TypeKind.Error ? "<error type>" : b.Type.ToString();
                    return $"{b.Input.GetDebuggerDisplay()} is {typeName}";
                case BoundDagValueTest v:
                    return $"{v.Input.GetDebuggerDisplay()} == {v.Value.GetValueToDisplay()}";
                case BoundDagNonNullTest nn:
                    return $"{nn.Input.GetDebuggerDisplay()} != null";
                case BoundDagExplicitNullTest n:
                    return $"{n.Input.GetDebuggerDisplay()} == null";
                case BoundDagRelationalTest r:
                    var operatorName = r.Relation.Operator() switch
                    {
                        BinaryOperatorKind.LessThan => "<",
                        BinaryOperatorKind.LessThanOrEqual => "<=",
                        BinaryOperatorKind.GreaterThan => ">",
                        BinaryOperatorKind.GreaterThanOrEqual => ">=",
                        _ => "??"
                    };
                    return $"{r.Input.GetDebuggerDisplay()} {operatorName} {r.Value.GetValueToDisplay()}";
                default:
                    return $"{this.Kind}({this.Input.GetDebuggerDisplay()})";
            }
        }
#endif
    }

    partial class BoundDagValueTest
    {
        public override BoundDagTest UpdateTestImpl(BoundDagTemp input) => Update(input);
        public new BoundDagValueTest Update(BoundDagTemp input)
        {
            return Update(Value, input);
        }
    }

    partial class BoundDagExplicitNullTest
    {
        public override BoundDagTest UpdateTestImpl(BoundDagTemp input) => Update(input);
    }

    partial class BoundDagNonNullTest
    {
        public override BoundDagTest UpdateTestImpl(BoundDagTemp input) => Update(input);
        public new BoundDagNonNullTest Update(BoundDagTemp input)
        {
            return Update(IsExplicitTest, input);
        }
    }

    partial class BoundDagTypeTest
    {
        public override BoundDagTest UpdateTestImpl(BoundDagTemp input) => Update(input);
        public new BoundDagTypeTest Update(BoundDagTemp input)
        {
            return Update(Type, input);
        }
    }
}
