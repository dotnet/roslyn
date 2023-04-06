// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Intentionally not exported.  This instance can instead be used whenever a specialized instance is not provided
    /// in the mef composition.
    /// </summary>
    internal sealed class DefaultAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
    {
        public static readonly IAnalyzerAssemblyLoaderProvider Instance = new DefaultAnalyzerAssemblyLoaderProvider();

        private readonly DefaultAnalyzerAssemblyLoader _loader = new();
        private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader = new();

        private DefaultAnalyzerAssemblyLoaderProvider()
        {
        }

        public IAnalyzerAssemblyLoader GetLoader(in AnalyzerAssemblyLoaderOptions options)
            => options.ShadowCopy ? _shadowCopyLoader : _loader;
    }
}
