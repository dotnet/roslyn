// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Execution.Serialization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Execution
{
    /// <summary>
    /// this is default implementation of IReferenceSerializationService
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IReferenceSerializationService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioReferenceSerializationServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly SerializationAnalyzerAssemblyLoader s_loader = new SerializationAnalyzerAssemblyLoader();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(
                workspaceServices.GetService<ITemporaryStorageService>(),
                workspaceServices.GetService<IAnalyzerService>());
        }

        private class Service : AbstractReferenceSerializationService
        {
            private readonly ShadowCopyAnalyzerAssemblyLoader _hostLoader;

            public Service(ITemporaryStorageService storageService, IAnalyzerService analyzerService) : base(storageService)
            {
                _hostLoader = (ShadowCopyAnalyzerAssemblyLoader)analyzerService.GetLoader();
            }

            protected override string GetAnalyzerAssemblyPath(string analyzerPath)
            {
                // TODO: find out a way to get shadow copied version of analyzer assembly location
                //       without actually loading analyzer in memory
                var assembly = _hostLoader.LoadFromPath(analyzerPath);

                return assembly.Location;
            }

            protected override AnalyzerReference GetAnalyzerReference(string displayPath, string assemblyPath)
            {
                // record path to actual assembly location pair info
                s_loader.AddPath(displayPath, assemblyPath);

                return new AnalyzerFileReference(displayPath, s_loader);
            }
        }
    }
}
