// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
