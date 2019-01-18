// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Indicates whether a bound local is also a declaration, and if so was it a declaration with an explicit or an inferred type.
    /// Ex:
    /// - In `M(x)`, `x` has `LocalDeclarationKind.None`
    /// - In `M(out int x)`, `x` has `LocalDeclarationKind.WithExplicitType`
    /// - In `M(out var x)`, `x` has `LocalDeclarationKind.WithInferredType`
    /// </summary>
    internal enum BoundLocalDeclarationKind
    {
        None = 0,
        WithExplicitType,
        WithInferredType
    }
}
