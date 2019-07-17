// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable 

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Roslyn.Utilities;
using VSLangProj80;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Utilities
{
    [ExportLanguageService(typeof(IParseOptionsChangingService), LanguageNames.CSharp), Shared]
    internal class CSharpParseOptionsChangingService : IParseOptionsChangingService
    {
        [ImportingConstructor]
        public CSharpParseOptionsChangingService()
        {
        }

        public bool CanApplyChange(ParseOptions oldOptions, ParseOptions newOptions, string? maxLangVersion)
        {
            var oldCSharpOptions = (CSharpParseOptions)oldOptions;
            var newCSharpOptions = (CSharpParseOptions)newOptions;

            LanguageVersion parsedMaxLanguageVersion;

            if (string.IsNullOrEmpty(maxLangVersion))
            {
                parsedMaxLanguageVersion = LanguageVersion.Latest;
            }
            else
            {
                Contract.ThrowIfFalse(LanguageVersionFacts.TryParse(maxLangVersion, out parsedMaxLanguageVersion));
            }

            // Currently, only changes to the LanguageVersion of parse options are supported.
            return oldCSharpOptions.WithLanguageVersion(newCSharpOptions.SpecifiedLanguageVersion) == newOptions &&
                newCSharpOptions.LanguageVersion <= parsedMaxLanguageVersion;
        }

        public void Apply(ParseOptions options, ProjectPropertyStorage storage)
        {
            var csharpOptions = (CSharpParseOptions)options;

            storage.SetProperty("LangVersion", nameof(CSharpProjectConfigurationProperties3.LanguageVersion),
                LanguageVersionFacts.ToDisplayString(csharpOptions.SpecifiedLanguageVersion));
        }
    }
}
