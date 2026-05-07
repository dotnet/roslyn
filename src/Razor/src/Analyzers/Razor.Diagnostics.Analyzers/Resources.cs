// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Razor.Diagnostics.Analyzers;

internal partial class Resources
{
    private static readonly Type s_resourcesType = typeof(Resources);

    public static LocalizableResourceString CreateLocalizableResourceString(string nameOfLocalizableResource)
        => new(nameOfLocalizableResource, ResourceManager, s_resourcesType);

    public static LocalizableResourceString CreateLocalizableResourceString(string nameOfLocalizableResource, params string[] formatArguments)
        => new(nameOfLocalizableResource, ResourceManager, s_resourcesType, formatArguments);
}
