
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    internal sealed record RazorSourceGenerationOptions
    {
        public string RootNamespace { get; set; } = "ASP";

        public RazorConfiguration Configuration { get; set; } = RazorConfiguration.Default;

        /// <summary>
        /// Gets a flag that determines if generated Razor views and Pages includes the <c>RazorSourceChecksumAttribute</c>.
        /// </summary>
        public bool GenerateMetadataSourceChecksumAttributes { get; set; } = false;

        internal CSharpParseOptions CSharpParseOptions { get; set; } = new CSharpParseOptions(LanguageVersion.CSharp10);

        /// <summary>
        /// Gets a flag that determines if localized component names should be supported.
        /// </summary>
        public bool SupportLocalizedComponentNames { get; set; } = false;

        /// <summary>
        /// Gets the flag that should be set on code documents to replace unique ids for testing purposes
        /// </summary>
        internal string? TestSuppressUniqueIds { get; set; }

        internal bool UseRoslynTokenizer { get; set; } = true;

        public override int GetHashCode() => Configuration.GetHashCode();
    }
}
