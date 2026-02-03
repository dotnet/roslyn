// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagEvaluation
    {
        public sealed override bool Equals([NotNullWhen(true)] object? obj) => obj is BoundDagEvaluation other && this.Equals(other);
        public virtual bool Equals(BoundDagEvaluation other)
        {
            return this == other ||
                this.IsEquivalentTo(other) &&
                this.Input.Equals(other.Input);
        }

        /// <summary>
        /// Check if this is equivalent to the <paramref name="other"/> node, ignoring the input.
        /// </summary>
        public virtual bool IsEquivalentTo(BoundDagEvaluation other)
        {
            return this == other ||
               this.Kind == other.Kind &&
               Symbol.Equals(this.Symbol, other.Symbol, TypeCompareKind.AllIgnoreOptions);
        }

        private Symbol? Symbol
        {
            get
            {
                var result = this switch
                {
                    BoundDagFieldEvaluation e => e.Field.CorrespondingTupleField ?? e.Field,
                    BoundDagPropertyEvaluation e => e.Property,
                    BoundDagTypeEvaluation e => e.Type,
                    BoundDagDeconstructEvaluation e => e.DeconstructMethod,
                    BoundDagIndexEvaluation e => e.Property,
                    BoundDagSliceEvaluation e => getSymbolFromIndexerAccess(e.IndexerAccess),
                    BoundDagIndexerEvaluation e => getSymbolFromIndexerAccess(e.IndexerAccess),
                    BoundDagAssignmentEvaluation => null,
                    _ => throw ExceptionUtilities.UnexpectedValue(this.Kind)
                };

                Debug.Assert(result is not null || this is BoundDagAssignmentEvaluation);
                return result;

                static Symbol? getSymbolFromIndexerAccess(BoundExpression indexerAccess)
                {
                    switch (indexerAccess)
                    {
                        // array[Range]
                        case BoundArrayAccess arrayAccess:
                            return arrayAccess.Expression.Type;

                        // array[Index]
                        case BoundImplicitIndexerAccess { IndexerOrSliceAccess: BoundArrayAccess arrayAccess }:
                            return arrayAccess.Expression.Type;

                        default:
                            return Binder.GetIndexerOrImplicitIndexerSymbol(indexerAccess);
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Input.GetHashCode(), this.Symbol?.GetHashCode() ?? 0);
        }

        public sealed override BoundDagTest UpdateTestImpl(BoundDagTemp input) => Update(input);

        public abstract BoundDagTemp MakeResultTemp();
        public new BoundDagEvaluation Update(BoundDagTemp input) => UpdateEvaluationImpl(input);
        public abstract BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input);

        public virtual OneOrMany<BoundDagTemp> AllOutputs()
        {
            return new OneOrMany<BoundDagTemp>(MakeResultTemp());
        }

#if DEBUG
        private int _id = -1;

        public int Id
        {
            get
            {
                return _id;
            }
            internal set
            {
                RoslynDebug.Assert(value > 0, "Id must be positive but was set to " + value);
                RoslynDebug.Assert(_id == -1, $"Id was set to {_id} and set again to {value}");
                _id = value;
            }
        }

        internal string GetOutputTempDebuggerDisplay()
        {
            var id = Id;
            return id switch
            {
                -1 => "<uninitialized>",

                // Note that we never expect to create an evaluation with id 0
                // To do so would imply that dag evaluation assigns to the original input
                0 => "<error>",

                _ => $"t{id}"
            };
        }
#endif
    }

    partial class BoundDagTypeEvaluation
    {
        public override BoundDagTemp MakeResultTemp()
        {
            return new BoundDagTemp(Syntax, Type, this);
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagTypeEvaluation Update(BoundDagTemp input)
        {
            return Update(Type, input);
        }
    }

    partial class BoundDagFieldEvaluation
    {
        public override BoundDagTemp MakeResultTemp()
        {
            return new BoundDagTemp(Syntax, Field.Type, this);
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagFieldEvaluation Update(BoundDagTemp input)
        {
            return Update(Field, input);
        }
    }

    partial class BoundDagPropertyEvaluation
    {
        public override BoundDagTemp MakeResultTemp()
        {
            return new BoundDagTemp(Syntax, Property.Type, this);
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagPropertyEvaluation Update(BoundDagTemp input)
        {
            return Update(Property, IsLengthOrCount, input);
        }
    }

    partial class BoundDagIndexEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool IsEquivalentTo(BoundDagEvaluation obj)
        {
            return base.IsEquivalentTo(obj) &&
                // base.IsEquivalentTo checks the kind field, so the following cast is safe
                this.Index == ((BoundDagIndexEvaluation)obj).Index;
        }

        public override BoundDagTemp MakeResultTemp()
        {
            Debug.Assert(Property.Type.IsObjectType());
            return new BoundDagTemp(Syntax, Property.Type, this);
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagIndexEvaluation Update(BoundDagTemp input)
        {
            return Update(Property, Index, input);
        }
    }

    partial class BoundDagIndexerEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool IsEquivalentTo(BoundDagEvaluation obj)
        {
            return base.IsEquivalentTo(obj) &&
                this.Index == ((BoundDagIndexerEvaluation)obj).Index;
        }

        public override bool Equals(BoundDagEvaluation other)
        {
            return base.Equals(other) && LengthTemp.Equals(((BoundDagIndexerEvaluation)other).LengthTemp);
        }

        private partial void Validate()
        {
            Debug.Assert(IndexerAccess is BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess);
        }

        public override BoundDagTemp MakeResultTemp()
        {
            return new BoundDagTemp(Syntax, IndexerType, this);
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagIndexerEvaluation Update(BoundDagTemp input)
        {
            return Update(IndexerType, LengthTemp, Index, IndexerAccess, ReceiverPlaceholder, ArgumentPlaceholder, input);
        }

        public BoundDagIndexerEvaluation Update(BoundDagTemp lengthTemp, BoundDagTemp input)
        {
            return Update(IndexerType, lengthTemp, Index, IndexerAccess, ReceiverPlaceholder, ArgumentPlaceholder, input);
        }

        public override OneOrMany<BoundDagTemp> AllInputs()
        {
            return new OneOrMany<BoundDagTemp>([Input, LengthTemp]);
        }
    }

    partial class BoundDagSliceEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.StartIndex ^ this.EndIndex;
        public override bool IsEquivalentTo(BoundDagEvaluation obj)
        {
            return base.IsEquivalentTo(obj) &&
                (BoundDagSliceEvaluation)obj is var e &&
                this.StartIndex == e.StartIndex && this.EndIndex == e.EndIndex;
        }

        public override bool Equals(BoundDagEvaluation other)
        {
            return base.Equals(other) && LengthTemp.Equals(((BoundDagSliceEvaluation)other).LengthTemp);
        }

        private partial void Validate()
        {
            Debug.Assert(IndexerAccess is BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess);
        }

        public override BoundDagTemp MakeResultTemp()
        {
            return new BoundDagTemp(Syntax, SliceType, this);
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagSliceEvaluation Update(BoundDagTemp input)
        {
            return Update(SliceType, LengthTemp, StartIndex, EndIndex, IndexerAccess, ReceiverPlaceholder, ArgumentPlaceholder, input);
        }

        public BoundDagSliceEvaluation Update(BoundDagTemp lengthTemp, BoundDagTemp input)
        {
            return Update(SliceType, lengthTemp, StartIndex, EndIndex, IndexerAccess, ReceiverPlaceholder, ArgumentPlaceholder, input);
        }

        public override OneOrMany<BoundDagTemp> AllInputs()
        {
            return new OneOrMany<BoundDagTemp>([Input, LengthTemp]);
        }
    }

    partial class BoundDagAssignmentEvaluation
    {
        public override int GetHashCode() => Hash.Combine(base.GetHashCode(), this.Target.GetHashCode());
        public override bool IsEquivalentTo(BoundDagEvaluation obj)
        {
            return base.IsEquivalentTo(obj) &&
                this.Target.Equals(((BoundDagAssignmentEvaluation)obj).Target);
        }

        public override BoundDagTemp MakeResultTemp()
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagAssignmentEvaluation Update(BoundDagTemp input)
        {
            return Update(Target, input);
        }
    }

    partial class BoundDagDeconstructEvaluation
    {
        public ArrayBuilder<BoundDagTemp> MakeOutParameterTemps()
        {
            MethodSymbol method = DeconstructMethod;
            int extensionExtra = method.IsStatic ? 1 : 0;
            int count = method.ParameterCount - extensionExtra;
            var builder = ArrayBuilder<BoundDagTemp>.GetInstance(count);
            for (int i = 0; i < count; i++)
            {
                ParameterSymbol parameter = method.Parameters[i + extensionExtra];
                Debug.Assert(parameter.RefKind == RefKind.Out);
                builder.Add(new BoundDagTemp(Syntax, parameter.Type, this, i));
            }

            return builder;
        }

        public override BoundDagTemp MakeResultTemp()
        {
            throw ExceptionUtilities.Unreachable();
        }

        public override BoundDagEvaluation UpdateEvaluationImpl(BoundDagTemp input) => Update(input);
        public new BoundDagDeconstructEvaluation Update(BoundDagTemp input)
        {
            return Update(DeconstructMethod, input);
        }

        public override OneOrMany<BoundDagTemp> AllOutputs()
        {
            var builder = MakeOutParameterTemps();

            if (builder is [var one])
            {
                builder.Free();
                return new OneOrMany<BoundDagTemp>(one);
            }

            return new OneOrMany<BoundDagTemp>(builder.ToImmutableAndFree());
        }
    }
}
