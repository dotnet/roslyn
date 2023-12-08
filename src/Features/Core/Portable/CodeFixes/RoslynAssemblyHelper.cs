// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    internal static class RoslynAssemblyHelper
    {
        public static bool HasRoslynPublicKey(object source)
            => source.GetType().GetTypeInfo().Assembly.GetName().GetPublicKey().SequenceEqual(
            typeof(RoslynAssemblyHelper).GetTypeInfo().Assembly.GetName().GetPublicKey());
    }
}
