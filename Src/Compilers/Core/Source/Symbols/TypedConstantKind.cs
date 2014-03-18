// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the kind of a TypedConstant.
    /// </summary>
    public enum TypedConstantKind
    {
        Error = 0, // error should be the default so that default(TypedConstant) is internally consistent
        Primitive,
        Enum,
        Type,
        Array
    }
}
