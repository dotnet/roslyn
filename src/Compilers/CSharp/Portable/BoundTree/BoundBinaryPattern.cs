// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

// TODO2 move those types to dedicated files
namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundBinaryPattern
    {
        // TODO2 do we need this?
        internal BoundBinaryPattern WithLeft(BoundPattern left)
        {
            return new BoundBinaryPattern(this.Syntax, this.Disjunction, left, this.Right, this.InputType, this.NarrowedType, this.HasErrors);
        }

        internal BoundBinaryPattern WithRight(BoundPattern right)
        {
            return new BoundBinaryPattern(this.Syntax, this.Disjunction, this.Left, right, this.InputType, this.NarrowedType, this.HasErrors);
        }
    }

    internal partial class BoundPropertySubpattern
    {
        internal BoundPropertySubpattern WithPattern(BoundPattern pattern)
        {
            return new BoundPropertySubpattern(this.Syntax, this.Member, this.IsLengthOrCount, pattern, this.HasErrors);
        }
    }

    internal partial class BoundRecursivePattern
    {
        internal BoundRecursivePattern WithDeconstruction(ImmutableArray<BoundPositionalSubpattern> deconstruction)
        {
            return new BoundRecursivePattern(this.Syntax, this.DeclaredType, this.DeconstructMethod, deconstruction,
                this.Properties, this.IsExplicitNotNullTest, this.Variable, this.VariableAccess, this.InputType, this.NarrowedType, this.HasErrors);
        }

        internal BoundRecursivePattern WithProperties(ImmutableArray<BoundPropertySubpattern> properties)
        {
            return new BoundRecursivePattern(this.Syntax, this.DeclaredType, this.DeconstructMethod, this.Deconstruction, properties,
                this.IsExplicitNotNullTest, this.Variable, this.VariableAccess, this.InputType, this.NarrowedType, this.HasErrors);
        }
    }

    internal partial class BoundPositionalSubpattern
    {
        internal BoundPositionalSubpattern WithPattern(BoundPattern pattern)
        {
            return new BoundPositionalSubpattern(this.Syntax, this.Symbol, pattern, this.HasErrors);
        }
    }

    internal partial class BoundITuplePattern
    {
        internal BoundITuplePattern WithSubpatterns(ImmutableArray<BoundPositionalSubpattern> subpatterns)
        {
            return new BoundITuplePattern(this.Syntax, this.GetLengthMethod, this.GetItemMethod, subpatterns,
                this.InputType, this.NarrowedType, this.HasErrors);
        }
    }
}
