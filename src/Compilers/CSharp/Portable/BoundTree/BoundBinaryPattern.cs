// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

// TODO2 move those types to dedicated files
namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundBinaryPattern
    {
        internal BoundBinaryPattern WithDisjunction(bool disjunction)
        {
            // TODO2 assert node changed
            return new BoundBinaryPattern(this.Syntax, disjunction, this.Left, this.Right, this.InputType, this.NarrowedType, this.HasErrors);
        }

        internal BoundBinaryPattern WithLeft(BoundPattern left)
        {
            // TODO2 assert node changed
            return new BoundBinaryPattern(this.Syntax, this.Disjunction, left, this.Right, this.InputType, this.NarrowedType, this.HasErrors);
        }

        internal BoundBinaryPattern WithRight(BoundPattern right)
        {
            // TODO2 assert node changed
            return new BoundBinaryPattern(this.Syntax, this.Disjunction, this.Left, right, this.InputType, this.NarrowedType, this.HasErrors);
        }
    }

    internal partial class BoundPropertySubpattern
    {
        internal BoundPropertySubpattern WithPattern(BoundPattern pattern)
        {
            // TODO2 assert node changed
            return new BoundPropertySubpattern(this.Syntax, this.Member, this.IsLengthOrCount, pattern, this.HasErrors);
        }
    }

    internal partial class BoundRecursivePattern
    {
        internal BoundRecursivePattern WithDeconstruction(ImmutableArray<BoundPositionalSubpattern> deconstruction)
        {
            // TODO2 assert node changed
            return new BoundRecursivePattern(this.Syntax, this.DeclaredType, this.DeconstructMethod, deconstruction,
                this.Properties, this.IsExplicitNotNullTest, this.Variable, this.VariableAccess, this.InputType, this.NarrowedType, this.HasErrors);
        }

        internal BoundRecursivePattern WithProperties(ImmutableArray<BoundPropertySubpattern> properties)
        {
            // TODO2 assert node changed
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

        internal BoundPattern WithSyntax(SyntaxNode syntax)
        {
            return new BoundITuplePattern(syntax, this.GetLengthMethod, this.GetItemMethod, this.Subpatterns,
                this.InputType, this.NarrowedType, this.HasErrors);
        }
    }
}
