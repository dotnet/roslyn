// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
extern alias Scripting;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Scripting::Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal readonly struct InteractiveHostPlatformInfo
    {
        internal sealed class Data
        {
            public string[] PlatformAssemblyPaths = null!;
            public bool HasGlobalAssemblyCache;

            public InteractiveHostPlatformInfo Deserialize()
                => new InteractiveHostPlatformInfo(
                    PlatformAssemblyPaths.ToImmutableArray(),
                    HasGlobalAssemblyCache);
        }

        public static readonly InteractiveHostPlatformInfo Current = new InteractiveHostPlatformInfo(
            RuntimeMetadataReferenceResolver.GetTrustedPlatformAssemblyPaths(),
            GacFileResolver.IsAvailable);

        public readonly ImmutableArray<string> PlatformAssemblyPaths;
        public readonly bool HasGlobalAssemblyCache;

        public InteractiveHostPlatformInfo(ImmutableArray<string> platformAssemblyPaths, bool hasGlobalAssemblyCache)
        {
            Debug.Assert(!platformAssemblyPaths.IsDefault);

            HasGlobalAssemblyCache = hasGlobalAssemblyCache;
            PlatformAssemblyPaths = platformAssemblyPaths;
        }

        public Data Serialize()
            => new Data()
            {
                HasGlobalAssemblyCache = HasGlobalAssemblyCache,
                PlatformAssemblyPaths = PlatformAssemblyPaths.ToArray(),
            };
    }
}
