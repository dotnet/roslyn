// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

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
            LanguageVersion.Default.TryParseDisplayString(version, out var newVersion);

            return csharpOptions.WithLanguageVersion(newVersion);
        }

        public bool NewerThan(string newVersion, string oldVersion)
        {
            LanguageVersion.Default.TryParseDisplayString(newVersion, out var newLangVersion);
            LanguageVersion.Default.TryParseDisplayString(oldVersion, out var oldLangVersion);

            newLangVersion = newLangVersion.MapSpecifiedToEffectiveVersion();
            oldLangVersion = oldLangVersion.MapSpecifiedToEffectiveVersion();

            return newLangVersion > oldLangVersion;
        }
    }
}
