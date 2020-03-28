// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable 

using System;
using System.Composition;
using System.Drawing.Text;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpParseOptionsChangingService()
        {
        }

        public bool CanApplyChange(ParseOptions oldOptions, ParseOptions newOptions, string? maxLangVersion)
        {
            var oldCSharpOptions = (CSharpParseOptions)oldOptions;
            var newCSharpOptions = (CSharpParseOptions)newOptions;

            // Currently, only changes to the LanguageVersion of parse options are supported.
            if (oldCSharpOptions.WithLanguageVersion(newCSharpOptions.SpecifiedLanguageVersion) != newOptions)
            {
                return false;
            }

            if (string.IsNullOrEmpty(maxLangVersion))
            {
                return true;
            }
            else
            {
                Contract.ThrowIfFalse(LanguageVersionFacts.TryParse(maxLangVersion, out var parsedMaxLanguageVersion));
                return newCSharpOptions.LanguageVersion <= parsedMaxLanguageVersion;
            }
        }

        public void Apply(ParseOptions options, ProjectPropertyStorage storage)
        {
            var csharpOptions = (CSharpParseOptions)options;

            storage.SetProperty("LangVersion", nameof(CSharpProjectConfigurationProperties3.LanguageVersion),
                LanguageVersionFacts.ToDisplayString(csharpOptions.SpecifiedLanguageVersion));
        }
    }
}
