// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [ExportLanguageSpecificOptionSerializer(
        LanguageNames.CSharp,
        FormattingOptions.TabFeatureName,
        BraceCompletionOptions.FeatureName,
        CompletionOptions.FeatureName,
        SignatureHelpOptions.FeatureName,
        NavigationBarOptions.FeatureName), Shared]
    internal class CSharpLanguageSettingsSerializer : AbstractLanguageSettingsSerializer
    {
        [ImportingConstructor]
        public CSharpLanguageSettingsSerializer(SVsServiceProvider serviceProvider) :
            base(Guids.CSharpLanguageServiceId, LanguageNames.CSharp, serviceProvider)
        {
        }
    }
}
