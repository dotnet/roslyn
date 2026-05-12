// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem;

internal static class FallbackRazorConfiguration
{
    public static readonly RazorConfiguration MVC_1_0 = new(
        RazorLanguageVersion.Version_1_0,
        "MVC-1.0",
        [new("MVC-1.0")]);

    public static readonly RazorConfiguration MVC_1_1 = new(
        RazorLanguageVersion.Version_1_1,
        "MVC-1.1",
        [new("MVC-1.1")]);

    public static readonly RazorConfiguration MVC_2_0 = new(
         RazorLanguageVersion.Version_2_0,
         "MVC-2.0",
         [new("MVC-2.0")]);

    public static readonly RazorConfiguration MVC_2_1 = new(
         RazorLanguageVersion.Version_2_1,
         "MVC-2.1",
         [new("MVC-2.1")]);

    public static readonly RazorConfiguration MVC_3_0 = new(
         RazorLanguageVersion.Version_3_0,
         "MVC-3.0",
         [new("MVC-3.0")]);

    public static readonly RazorConfiguration MVC_5_0 = new(
         RazorLanguageVersion.Version_5_0,
         // Razor 5.0 uses MVC 3.0 Razor configuration.
         "MVC-3.0",
         [new("MVC-3.0")]);

    public static readonly RazorConfiguration Latest = new(
         RazorLanguageVersion.Latest,
         // Razor latest uses MVC 3.0 Razor configuration.
         "MVC-3.0",
         [new("MVC-3.0")]);

    public static RazorConfiguration SelectConfiguration(Version version)
        => version switch
        {
            { Major: 1, Minor: 0 } => MVC_1_0,
            { Major: 1, Minor: 1 } => MVC_1_1,
            { Major: 2, Minor: 0 } => MVC_2_0,
            { Major: 2, Minor: >= 1 } => MVC_2_1,
            { Major: 3, Minor: 0 } => MVC_3_0,
            { Major: 5, Minor: 0 } => MVC_5_0,
            _ => Latest
        };
}
