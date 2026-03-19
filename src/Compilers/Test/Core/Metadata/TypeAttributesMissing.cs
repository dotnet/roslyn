// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
