// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor.IntegrationTests;

public static class WellKnownProjectTemplates
{
    public const string BlazorProject = "Microsoft.WAP.CSharp.ASPNET.Blazor";

    public static class GroupIdentifiers
    {
        public const string Server = "Microsoft.Web.Blazor.Server";
        public const string Wasm = "Microsoft.Web.Blazor.Wasm";
    }

    public static class TemplateIdentifiers
    {
        public const string Server31 = "Microsoft.Web.Blazor.Server.CSharp.3.1";
        public const string Server50 = "Microsoft.Web.Blazor.Server.CSharp.5.0";
        public const string Server60 = "Microsoft.Web.Blazor.Server.CSharp.6.0";
    }
}
