// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundPattern
    {
        /// <summary>
        /// The type to which we attempt to convert the input in order to match this pattern.
        /// </summary>
        public virtual TypeSymbol ConvertedType => InputType;
    }

    internal partial class BoundConstantPattern
    {
        public override TypeSymbol ConvertedType => Value.Type;
    }

    internal partial class BoundDeclarationPattern
    {
        public override TypeSymbol ConvertedType => DeclaredType?.Type ?? InputType;
    }

    internal partial class BoundRecursivePattern
    {
        public override TypeSymbol ConvertedType => DeclaredType?.Type ?? InputType;
    }
}
