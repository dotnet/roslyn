// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ImplementType;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CodeActions
{
    internal readonly record struct OmniSharpCodeActionOptions(
        OmniSharpImplementTypeOptions ImplementTypeOptions,
        OmniSharpLineFormattingOptions LineFormattingOptions)
    {
        internal CodeActionOptions GetCodeActionOptions(LanguageServices languageServices)
        {
            var defaultOptions = CodeActionOptions.GetDefault(languageServices);
            return defaultOptions with
            {
                CleanupOptions = defaultOptions.CleanupOptions with
                {
                    FormattingOptions = defaultOptions.CleanupOptions.FormattingOptions with
                    {
                        LineFormatting = new()
                        {
                            IndentationSize = LineFormattingOptions.IndentationSize,
                            TabSize = LineFormattingOptions.TabSize,
                            UseTabs = LineFormattingOptions.UseTabs,
                            NewLine = LineFormattingOptions.NewLine,
                        }
                    }
                },
                ImplementTypeOptions = new()
                {
                    InsertionBehavior = (ImplementTypeInsertionBehavior)ImplementTypeOptions.InsertionBehavior,
                    PropertyGenerationBehavior = (ImplementTypePropertyGenerationBehavior)ImplementTypeOptions.PropertyGenerationBehavior
                }
            };
        }
    }
}
