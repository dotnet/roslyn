// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.CSharp;
using Microsoft.VisualStudio.LanguageServices.VisualBasic;
using CSharpPackage = Microsoft.VisualStudio.LanguageServices.CSharp.VSPackage;
using VisualBasicPackage = Microsoft.VisualStudio.LanguageServices.VisualBasic.VSPackage;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal static class Utilities
{
    private const string CSharpLanguageServiceDllName = "Microsoft.VisualStudio.LanguageServices.CSharp.dll";

    private const string VisualBasicLanguageServiceDllName = "Microsoft.VisualStudio.LanguageServices.VisualBasic.dll";

    private const string LanguageServiceDllName = "Microsoft.VisualStudio.LanguageServices.dll";

    public static string? EvalResource(string resourceReference)
    {
        // Start with 1 because to skip @ at the beginning, like @Show_completion_item_filters
        var resourcesIdentifier = resourceReference.Substring(1, resourceReference.IndexOf(";") - 1);
        var resources = resourceReference[(resourceReference.IndexOf(";") + 1)..];
        var culture = new CultureInfo("en");
        if (resourcesIdentifier.IndexOf(':') is var namespaceSeparator && namespaceSeparator >= 0)
        {
            var resourceNamespace = resourcesIdentifier[..namespaceSeparator];
            var resourceKey = resourcesIdentifier[(namespaceSeparator + 1)..];
            var (manager, module, owner, resourceType) = GetResourceTable(resourceNamespace);
            if (!StringComparer.Ordinal.Equals(resourceType.FullName, resourceNamespace) ||
                Array.IndexOf(resourceType.Assembly.GetManifestResourceNames(), resourceNamespace + ".resources") < 0)
            {
                throw new InvalidOperationException("Resource namespace does not match the compiled table: " + resourceReference);
            }

            if (Guid.TryParse(resources, out var ownerGuid))
            {
                if (owner is null || ownerGuid != owner)
                {
                    throw new InvalidOperationException("Resource table does not belong to the specified package: " + resourceReference);
                }
            }
            else if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetFileName(resources), module))
            {
                throw new InvalidOperationException("Resource table does not belong to the specified assembly: " + resourceReference);
            }

            return manager.GetString(resourceKey, culture);
        }

        if (Guid.TryParse(resources, out var packageGuid))
        {
            if (packageGuid.Equals(Guids.CSharpPackageId))
            {
                return CSharpPackage.ResourceManager.GetString(resourcesIdentifier, culture);
            }
            else if (packageGuid.Equals(Guids.VisualBasicPackageId))
            {
                return VisualBasicPackage.ResourceManager.GetString(resourcesIdentifier, culture);
            }
            else
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        var resourceDll = Path.GetFileName(resources);
        return resourceDll switch
        {
            CSharpLanguageServiceDllName => CSharpVSResources.ResourceManager.GetString(resourcesIdentifier, culture),
            VisualBasicLanguageServiceDllName => VisualBasicPackage.ResourceManager.GetString(resourcesIdentifier, culture),
            LanguageServiceDllName => ServicesVSResources.ResourceManager.GetString(resourcesIdentifier, culture),
            _ => throw ExceptionUtilities.UnexpectedValue(resourceDll)
        };
    }

    private static (ResourceManager manager, string module, Guid? owner, Type resourceType) GetResourceTable(string resourceNamespace)
        => resourceNamespace switch
        {
            "Microsoft.VisualStudio.LanguageServices.ServicesVSResources" => (ServicesVSResources.ResourceManager, LanguageServiceDllName, Guids.RoslynPackageId, typeof(ServicesVSResources)),
            "Microsoft.VisualStudio.LanguageServices.CSharp.CSharpVSResources" => (CSharpVSResources.ResourceManager, CSharpLanguageServiceDllName, Guids.CSharpPackageId, typeof(CSharpVSResources)),
            "Microsoft.VisualStudio.LanguageServices.VisualBasic.BasicVSResources" => (BasicVSResources.ResourceManager, VisualBasicLanguageServiceDllName, Guids.VisualBasicPackageId, typeof(BasicVSResources)),
            "Microsoft.CodeAnalysis.WorkspacesResources" => (WorkspacesResources.ResourceManager, "Microsoft.CodeAnalysis.Workspaces.dll", null, typeof(WorkspacesResources)),
            "Microsoft.CodeAnalysis.EditorFeaturesResources" => (EditorFeaturesResources.ResourceManager, "Microsoft.CodeAnalysis.EditorFeatures.dll", null, typeof(EditorFeaturesResources)),
            _ => throw ExceptionUtilities.UnexpectedValue(resourceNamespace),
        };
}
