// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Host.Mef;

public static class MSBuildMefHostServices
{
    public static MefHostServices DefaultServices
    {
        get
        {
            // At this point, we don't have any MEF types in this assembly, so we can just defer to the default set.
            // This type is just maintained for public API compatibility (and future expansion if we were to have to add types back in.)
            return MefHostServices.DefaultHost;
        }
    }

    public static ImmutableArray<Assembly> DefaultAssemblies
    {
        get
        {
            // At this point, we don't have any MEF types in this assembly, so we can just defer to the default set.
            // This type is just maintained for public API compatibility (and future expansion if we were to have to add types back in.)
            return MefHostServices.DefaultAssemblies;
        }
    }
}
