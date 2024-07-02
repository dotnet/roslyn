// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Formatting
{
    internal readonly record struct OmniSharpSyntaxFormattingOptionsWrapper
    {
        internal readonly SyntaxFormattingOptions UnderlyingObject;

        internal OmniSharpSyntaxFormattingOptionsWrapper(SyntaxFormattingOptions cleanupOptions)
        {
            UnderlyingObject = cleanupOptions;
        }

        public static async ValueTask<OmniSharpSyntaxFormattingOptionsWrapper> FromDocumentAsync(Document document, OmniSharpLineFormattingOptions fallbackLineFormattingOptions, CancellationToken cancellationToken)
        {
            var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);

            var optionsWithFallback = StructuredAnalyzerConfigOptions.Create(configOptions,
                StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty
                    .Add(FormattingOptions2.TabSize.Definition.ConfigName, FormattingOptions2.TabSize.Definition.Serializer.Serialize(fallbackLineFormattingOptions.TabSize))
                    .Add(FormattingOptions2.IndentationSize.Definition.ConfigName, FormattingOptions2.IndentationSize.Definition.Serializer.Serialize(fallbackLineFormattingOptions.IndentationSize))
                    .Add(FormattingOptions2.UseTabs.Definition.ConfigName, FormattingOptions2.UseTabs.Definition.Serializer.Serialize(fallbackLineFormattingOptions.UseTabs))
                    .Add(FormattingOptions2.NewLine.Definition.ConfigName, FormattingOptions2.NewLine.Definition.Serializer.Serialize(fallbackLineFormattingOptions.NewLine)))));

            return new OmniSharpSyntaxFormattingOptionsWrapper(
                optionsWithFallback.GetSyntaxFormattingOptions(document.Project.Services));
        }
    }
}
