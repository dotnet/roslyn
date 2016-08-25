// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// this is desktop implementation of IReferenceSerializationService
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IReferenceSerializationService), layer: ServiceLayer.Desktop), Shared]
    internal class ReferenceSerializationServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly SerializationAnalyzerAssemblyLoader s_loader = new SerializationAnalyzerAssemblyLoader();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices.GetService<ITemporaryStorageService>());
        }

        private sealed class Service : AbstractReferenceSerializationService
        {
            public Service(ITemporaryStorageService service) : base(service)
            {
            }

            protected override string GetAnalyzerAssemblyPath(AnalyzerFileReference reference)
            {
                // TODO: find out a way to get analyzer assembly location
                //       without actually loading analyzer in memory
                var assembly = reference.GetAssembly();
                return assembly?.Location;
            }

            protected override AnalyzerReference GetAnalyzerReference(string displayPath, string assemblyPath)
            {
                // desktop implementation doesn't do shadow copying and doesn't guarantee snapshot
                s_loader.AddPath(displayPath, assemblyPath);

                return new AnalyzerFileReference(assemblyPath, s_loader);
            }
        }
    }
}
