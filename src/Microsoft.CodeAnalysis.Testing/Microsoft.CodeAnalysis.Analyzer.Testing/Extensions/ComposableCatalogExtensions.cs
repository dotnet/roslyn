// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Composition.Reflection;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class ComposableCatalogExtensions
    {
        public static ComposableCatalog WithDocumentTextDifferencingService(this ComposableCatalog catalog)
        {
            var assemblyQualifiedServiceTypeName = "Microsoft.CodeAnalysis.IDocumentTextDifferencingService, " + typeof(Workspace).GetTypeInfo().Assembly.GetName().ToString();

            // Check to see if IDocumentTextDifferencingService is exported
            foreach (var part in catalog.Parts)
            {
                foreach (var pair in part.ExportDefinitions)
                {
                    var exportDefinition = pair.Value;
                    if (exportDefinition.ContractName != "Microsoft.CodeAnalysis.Host.IWorkspaceService")
                    {
                        continue;
                    }

                    if (!exportDefinition.Metadata.TryGetValue("ServiceType", out var value)
                        || !(value is string serviceType))
                    {
                        continue;
                    }

                    if (serviceType != assemblyQualifiedServiceTypeName)
                    {
                        continue;
                    }

                    // The service is exported by default
                    return catalog;
                }
            }

            // If IDocumentTextDifferencingService is not exported by default, export it manually
            var manualExportDefinition = new ExportDefinition(
                typeof(IWorkspaceService).FullName,
                metadata: new Dictionary<string, object?>
                {
                    { "ExportTypeIdentity", typeof(IWorkspaceService).FullName },
                    { nameof(ExportWorkspaceServiceAttribute.ServiceType), assemblyQualifiedServiceTypeName },
                    { nameof(ExportWorkspaceServiceAttribute.Layer), ServiceLayer.Default },
                    { typeof(CreationPolicy).FullName, CreationPolicy.Shared },
                    { "ContractType", typeof(IWorkspaceService) },
                    { "ContractName", null },
                });

            var serviceImplType = typeof(Workspace).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.DefaultDocumentTextDifferencingService");
            return catalog.AddPart(new ComposablePartDefinition(
                TypeRef.Get(serviceImplType, Resolver.DefaultInstance),
                new Dictionary<string, object?> { { "SharingBoundary", null } },
                new[] { manualExportDefinition },
                new Dictionary<MemberRef, IReadOnlyCollection<ExportDefinition>>(),
                Enumerable.Empty<ImportDefinitionBinding>(),
                sharingBoundary: string.Empty,
                default(MethodRef),
                MethodRef.Get(serviceImplType.GetConstructors(BindingFlags.Instance | BindingFlags.Public).First(), Resolver.DefaultInstance),
                new List<ImportDefinitionBinding>(),
                CreationPolicy.Shared,
                new[] { typeof(Workspace).GetTypeInfo().Assembly.GetName() },
                isSharingBoundaryInferred: false));
        }
    }
}
