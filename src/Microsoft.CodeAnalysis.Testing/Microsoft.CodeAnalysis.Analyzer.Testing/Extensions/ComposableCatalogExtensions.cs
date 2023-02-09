// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Composition.Reflection;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class ComposableCatalogExtensions
    {
        private static readonly string AssemblyQualifiedServiceTypeName = "Microsoft.CodeAnalysis.IDocumentTextDifferencingService, " + typeof(Workspace).GetTypeInfo().Assembly.GetName().ToString();

        public static ComposableCatalog WithDocumentTextDifferencingService(this ComposableCatalog catalog)
        {
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

                    if (serviceType != AssemblyQualifiedServiceTypeName)
                    {
                        continue;
                    }

                    // The service is exported by default
                    return catalog;
                }
            }

            // If IDocumentTextDifferencingService is not exported by default, export it manually
            return AddDefaultDocumentTextDifferencingServiceToCatalog(catalog);
        }

        // This method references APIs that changed in more recent versions of Microsoft.VisualStudio.Composition. Since
        // the methods are only used for testing old Roslyn versions (which won't need to use the newer version of this
        // dependency, we extracted the API usage to a method that won't be visible to the JIT on test paths involving
        // new versions of Roslyn.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ComposableCatalog AddDefaultDocumentTextDifferencingServiceToCatalog(ComposableCatalog catalog)
        {
            var manualExportDefinition = new ExportDefinition(
                typeof(IWorkspaceService).FullName,
                metadata: new Dictionary<string, object?>
                {
                    { "ExportTypeIdentity", typeof(IWorkspaceService).FullName },
                    { nameof(ExportWorkspaceServiceAttribute.ServiceType), AssemblyQualifiedServiceTypeName },
                    { nameof(ExportWorkspaceServiceAttribute.Layer), ServiceLayer.Default },
                    { typeof(CreationPolicy).FullName!, CreationPolicy.Shared },
                    { "ContractType", typeof(IWorkspaceService) },
                    { "ContractName", null },
                });

            var serviceImplType = typeof(Workspace).GetTypeInfo().Assembly.GetType("Microsoft.CodeAnalysis.DefaultDocumentTextDifferencingService");
            RoslynDebug.AssertNotNull(serviceImplType);

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
