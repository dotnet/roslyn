// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Flags]
    internal enum DeclarationModifiers
    {
        None = 0,
        Abstract = 1 << 0,
        Sealed = 1 << 1,
        Static = 1 << 2,
        New = 1 << 3,
        Public = 1 << 4,
        Protected = 1 << 5,
        Internal = 1 << 6,
        ProtectedInternal = 1 << 7, // the two keywords together are treated as one modifier
        Private = 1 << 8,
        PrivateProtected = 1 << 9, // the two keywords together are treated as one modifier
        ReadOnly = 1 << 10,
        Const = 1 << 11,
        Volatile = 1 << 12,

        Extern = 1 << 13,
        Partial = 1 << 14,
        Unsafe = 1 << 15,
        Fixed = 1 << 16,
        Virtual = 1 << 17, // used for method binding
        Override = 1 << 18, // used for method binding

        Indexer = 1 << 19, // not a real modifier, but used to record that indexer syntax was used. 

        Async = 1 << 20,
        Ref = 1 << 21, // used only for structs

        All = (1 << 22) - 1, // all modifiers
        Unset = 1 << 22, // used when a modifiers value hasn't yet been computed

        AccessibilityMask = PrivateProtected | Private | Protected | Internal | ProtectedInternal | Public,
    }
}
