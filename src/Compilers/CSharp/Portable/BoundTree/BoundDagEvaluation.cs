// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagEvaluation
    {
        public sealed override bool Equals([NotNullWhen(true)] object? obj) => obj is BoundDagEvaluation other && this.Equals(other);
        public bool Equals(BoundDagEvaluation other)
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

    partial class BoundDagIndexEvaluation
    {
        public override int GetHashCode() => base.GetHashCode() ^ this.Index;
        public override bool IsEquivalentTo(BoundDagEvaluation obj)
        {
            return base.IsEquivalentTo(obj) &&
                // base.IsEquivalentTo checks the kind field, so the following cast is safe
                this.Index == ((BoundDagIndexEvaluation)obj).Index;
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

        private partial void Validate()
        {
            Debug.Assert(IndexerAccess is BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess);
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

        private partial void Validate()
        {
            Debug.Assert(IndexerAccess is BoundIndexerAccess or BoundImplicitIndexerAccess or BoundArrayAccess);
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
    }
}
