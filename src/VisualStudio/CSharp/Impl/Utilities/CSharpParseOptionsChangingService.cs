// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            else if (newCSharpOptions.LanguageVersion == LanguageVersion.Preview)
            {
                // It's always fine to upgrade a project to 'preview'.  This allows users to try out new features to see
                // how well they work, while also explicitly putting them into a *known* unsupported state (that's what
                // preview is after all).  Importantly, this doesn't put them into an unrealized unsupported state (for
                // example, picking some combo of a real lang version that isn't supported with their chosen framework
                // version).
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
