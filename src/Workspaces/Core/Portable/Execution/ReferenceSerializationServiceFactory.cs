// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// this is default implementation of IReferenceSerializationService
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IReferenceSerializationService), layer: ServiceLayer.Default), Shared]
    internal class ReferenceSerializationServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly IAnalyzerAssemblyLoader s_loader = new NullLoader();

        [ImportingConstructor]
        public ReferenceSerializationServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(
                workspaceServices.GetService<ITemporaryStorageService>() as ITemporaryStorageService2,
                workspaceServices.GetService<IDocumentationProviderService>());
        }

        private sealed class Service : AbstractReferenceSerializationService
        {
            public Service(ITemporaryStorageService2 service, IDocumentationProviderService documentationService)
                : base(service, documentationService)
            {
            }

            protected override string GetAnalyzerAssemblyPath(AnalyzerFileReference reference)
            {
                // default implementation doesn't do shadow copying and doesn't guarantee snapshot
                return reference.FullPath;
            }

            protected override AnalyzerReference GetAnalyzerReference(string displayPath, string assemblyPath)
            {
                // default implementation doesn't do shadow copying and doesn't guarantee snapshot
                return new AnalyzerFileReference(assemblyPath, s_loader);
            }
        }

        public sealed class NullLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                // TODO: can we make workspace to support analyzer?
                //       workspace is in a layer where we can't load analyzer assembly from file. 
                //       can we use CoreClr analyzer loader here?
                return null;
            }
        }
    }
}
