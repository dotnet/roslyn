// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    internal static class RoslynAssemblyHelper
    {
        public static bool HasRoslynPublicKey(object source) =>
            source.GetType().GetTypeInfo().Assembly.GetName().GetPublicKey().SequenceEqual(
            typeof(RoslynAssemblyHelper).GetTypeInfo().Assembly.GetName().GetPublicKey());
    }
}
