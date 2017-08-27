// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    public enum InstanceReferenceKind
    {
        None = 0x0,
        /// <summary>Indicates an implicit this or Me expression.</summary>
        Implicit = 0x1,
        /// <summary>Indicates an explicit this or Me expression.</summary>
        Explicit = 0x2,
        /// <summary>Indicates an explicit base or MyBase expression.</summary>
        BaseClass = 0x3,
        /// <summary>Indicates an explicit MyClass expression.</summary>
        ThisClass = 0x4
    }
}

