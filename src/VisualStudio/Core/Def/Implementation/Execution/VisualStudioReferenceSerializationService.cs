// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Execution.Serialization;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Execution
{
    /// <summary>
    /// this is default implementation of IReferenceSerializationService
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IReferenceSerializationService), layer: ServiceLayer.Host), Shared]
    internal class VisualStudioReferenceSerializationServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly IAnalyzerAssemblyLoader s_loader = new AssemblyLoader();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service(workspaceServices.GetService<ITemporaryStorageService>());
        }

        private class Service : AbstractReferenceSerializationService
        {
            public Service(ITemporaryStorageService service) : base(service)
            {
            }

            public override void WriteTo(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
            {
                writer.WriteString(reference.FullPath);

                var file = reference as AnalyzerFileReference;
                if (file != null)
                {
                    writer.WriteString(nameof(AnalyzerFileReference));
                    writer.WriteInt32((int)SerializationKinds.FilePath);
                    return;
                }

                var image = reference as AnalyzerImageReference;
                if (image != null)
                {
                    // TODO: think a way to support this or a way to deal with this kind of situation.
                    throw new NotSupportedException(nameof(AnalyzerImageReference));
                }

                var unresolved = reference as UnresolvedAnalyzerReference;
                if (unresolved != null)
                {
                    writer.WriteString(nameof(UnresolvedAnalyzerReference));
                    return;
                }

                throw ExceptionUtilities.UnexpectedValue(reference.GetType());
            }

            public override AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
            {
                var fullPath = reader.ReadString();

                var type = reader.ReadString();
                if (type == nameof(AnalyzerFileReference))
                {
                    var kind = (SerializationKinds)reader.ReadInt32();
                    Contract.ThrowIfFalse(kind == SerializationKinds.FilePath);

                    return new AnalyzerFileReference(fullPath, s_loader);
                }

                if (type == nameof(UnresolvedAnalyzerReference))
                {
                    return new UnresolvedAnalyzerReference(fullPath);
                }

                throw ExceptionUtilities.UnexpectedValue(type);
            }
        }

        private class AssemblyLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}
