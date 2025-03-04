// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.CSharp;
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

        var resourceDll = resourceReference[(resourceReference.IndexOf("\\") + 1)..];
        return resourceDll switch
        {
            CSharpLanguageServiceDllName => CSharpVSResources.ResourceManager.GetString(resourcesIdentifier, culture),
            VisualBasicLanguageServiceDllName => VisualBasicPackage.ResourceManager.GetString(resourcesIdentifier, culture),
            LanguageServiceDllName => ServicesVSResources.ResourceManager.GetString(resourcesIdentifier, culture),
            _ => throw ExceptionUtilities.UnexpectedValue(resourceDll)
        };
    }
}
