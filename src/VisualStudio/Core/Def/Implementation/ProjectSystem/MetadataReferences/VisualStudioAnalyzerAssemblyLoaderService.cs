// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceService(typeof(IAnalyzerService), ServiceLayer.Host), Shared]
    internal sealed class VsAnalyzerAssemblyLoaderService : IAnalyzerService
    {
        private readonly ShadowCopyAnalyzerAssemblyLoader _loader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VsAnalyzerAssemblyLoaderService()
        {
        }

        public IAnalyzerAssemblyLoader GetLoader()
            => _loader;
    }
}
