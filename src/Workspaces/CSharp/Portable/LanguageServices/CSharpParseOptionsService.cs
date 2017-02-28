// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(IParseOptionsService), LanguageNames.CSharp), Shared]
    internal class CSharpParseOptionsService : IParseOptionsService
    {
        public string GetLanguageVersion(ParseOptions options) =>  ((CSharpParseOptions)options).LanguageVersion.ToDisplayString();

        public ParseOptions WithLanguageVersion(ParseOptions options, string version)
        {
            var csharpOptions = (CSharpParseOptions)options;
            var newVersion = LanguageVersion.Default.WithLanguageVersion(version);
            return csharpOptions.WithLanguageVersion(newVersion);
        }
    }
}
