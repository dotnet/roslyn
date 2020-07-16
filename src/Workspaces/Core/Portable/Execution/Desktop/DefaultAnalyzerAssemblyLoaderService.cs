// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [Shared]
    [ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider))]
    internal sealed class DefaultAnalyzerAssemblyLoaderService : IAnalyzerAssemblyLoaderProvider
    {
        private readonly DefaultAnalyzerAssemblyLoader _loader = new DefaultAnalyzerAssemblyLoader();
        private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader = new ShadowCopyAnalyzerAssemblyLoader();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultAnalyzerAssemblyLoaderService()
        {
        }

        public IAnalyzerAssemblyLoader GetLoader(in AnalyzerAssemblyLoaderOptions options)
            => options.ShadowCopy ? _shadowCopyLoader : _loader;
    }
}
