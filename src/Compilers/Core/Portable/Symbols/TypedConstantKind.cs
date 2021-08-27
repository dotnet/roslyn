// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the kind of a TypedConstant.
    /// </summary>
    public enum TypedConstantKind
    {
        Error = 0, // error should be the default so that default(TypedConstant) is internally consistent
        Primitive = 1,
        Enum = 2,
        Type = 3,
        Array = 4
    }
}
