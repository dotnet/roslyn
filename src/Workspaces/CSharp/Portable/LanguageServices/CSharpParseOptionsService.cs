// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(IParseOptionsService), LanguageNames.CSharp), Shared]
    internal class CSharpParseOptionsService : IParseOptionsService
    {
        public string GetLanguageVersion(ParseOptions options) =>
            ((CSharpParseOptions)options).SpecifiedLanguageVersion.ToDisplayString();

        public ParseOptions WithLanguageVersion(ParseOptions options, string version)
        {
            var csharpOptions = (CSharpParseOptions)options;
            Contract.ThrowIfFalse(LanguageVersionFacts.TryParse(version, out var newVersion));

            return csharpOptions.WithLanguageVersion(newVersion);
        }
    }
}
