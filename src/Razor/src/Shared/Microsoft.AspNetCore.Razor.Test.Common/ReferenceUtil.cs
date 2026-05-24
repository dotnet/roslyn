// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using AspNetLatest = Basic.Reference.Assemblies.AspNet80;
using NetLatest = Basic.Reference.Assemblies.Net80;

namespace Microsoft.AspNetCore.Razor.Test.Common;

internal static class ReferenceUtil
{
    public static ImmutableArray<PortableExecutableReference> AspNetLatestAll { get; } = AspNetLatest.References.All;
    public static PortableExecutableReference AspNetLatestComponents { get; } = AspNetLatest.References.MicrosoftAspNetCoreComponents;
    public static PortableExecutableReference AspNetLatestRazor { get; } = AspNetLatest.References.MicrosoftAspNetCoreRazor;
    public static ImmutableArray<PortableExecutableReference> NetLatestAll { get; } = NetLatest.References.All;
    public static PortableExecutableReference NetLatestSystemRuntime { get; } = NetLatest.References.SystemRuntime;
}
