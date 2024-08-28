// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.CodeAnalysis.Scripting.Hosting;

internal readonly record struct AssemblyAndLocation(Assembly Assembly, string Location, bool GlobalAssemblyCache)
{
    public bool IsDefault => Assembly == null;

    public override string ToString()
        => Assembly + " @ " + (GlobalAssemblyCache ? "<GAC>" : Location);
}
