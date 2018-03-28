// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;

namespace Roslyn.Test.Utilities
{
    // TODO (tomat): this should be added to BCL's TypeAttributes
    internal static class TypeAttributesMissing
    {
        internal const TypeAttributes Forwarder = (TypeAttributes)0x00200000;
        internal const TypeAttributes NestedMask = (TypeAttributes)0x00000006;
    }
}
