// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
