// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundRecursivePattern
    {
        internal BoundRecursivePattern WithDeconstruction(ImmutableArray<BoundPositionalSubpattern> deconstruction)
        {
            return this.Update(this.DeclaredType, this.DeconstructMethod, deconstruction, this.Properties, this.IsExplicitNotNullTest,
                this.Variable, this.VariableAccess, this.InputType, this.NarrowedType);
        }

        internal BoundRecursivePattern WithProperties(ImmutableArray<BoundPropertySubpattern> properties)
        {
            return this.Update(this.DeclaredType, this.DeconstructMethod, this.Deconstruction, properties, this.IsExplicitNotNullTest,
                this.Variable, this.VariableAccess, this.InputType, this.NarrowedType);
        }
    }
}
