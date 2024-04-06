// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Implements a no-op wrapper to search the global assembly cache when running under a .net core runtime.
/// At some point we may wish to get information about assemblies in the GAC under a .net core runtime - for example
/// if we're loading a .net framework project in vscode and want to find the actual assembly to decompile.
/// 
/// However it isn't extremely straightforward to implement and we don't need it at this time so leaving it as a no-op.
/// More info on how this might be possible under a .net core runtime can be found https://github.com/dotnet/core/issues/3048#issuecomment-725781811
/// </summary>
internal sealed class DotNetCoreGlobalAssemblyCache : GlobalAssemblyCache
{
    public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, ImmutableArray<ProcessorArchitecture> architectureFilter = default)
    {
        return ImmutableArray<AssemblyIdentity>.Empty;
    }

    public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string? partialName = null, ImmutableArray<ProcessorArchitecture> architectureFilter = default)
    {
        return ImmutableArray<AssemblyIdentity>.Empty;
    }

    public override IEnumerable<string> GetAssemblySimpleNames(ImmutableArray<ProcessorArchitecture> architectureFilter = default)
    {
        return ImmutableArray<string>.Empty;
    }

    public override AssemblyIdentity? ResolvePartialName(string displayName, out string? location, ImmutableArray<ProcessorArchitecture> architectureFilter = default, CultureInfo? preferredCulture = null)
    {
        location = null;
        return null;
    }
}

