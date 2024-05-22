// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias Scripting;

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;
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

        private static readonly string s_hostDirectory = PathUtilities.GetDirectoryName(typeof(InteractiveHostPlatformInfo).Assembly.Location)!;

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

        public static InteractiveHostPlatformInfo GetCurrentPlatformInfo()
            => new InteractiveHostPlatformInfo(
                RuntimeMetadataReferenceResolver.GetTrustedPlatformAssemblyPaths().Where(IsNotHostAssembly).ToImmutableArray(),
                GacFileResolver.IsAvailable);

        private static bool IsNotHostAssembly(string path)
            => !StringComparer.OrdinalIgnoreCase.Equals(PathUtilities.GetDirectoryName(path), s_hostDirectory);
    }
}
