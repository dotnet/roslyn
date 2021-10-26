// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal readonly record struct DocumentationCommentOptions(
        bool AutoXmlDocCommentGeneration,
        int TabSize,
        bool UseTabs,
        string NewLine)
    {
        [ExportOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                AutoXmlDocCommentGeneration);

            public static readonly PerLanguageOption2<bool> AutoXmlDocCommentGeneration = new(nameof(DocumentationCommentOptions), nameof(AutoXmlDocCommentGeneration), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.AutoComment" : "TextEditor.%LANGUAGE%.Specific.Automatic XML Doc Comment Generation"));
        }

        public static DocumentationCommentOptions From(DocumentOptionSet options)
          => new(
              AutoXmlDocCommentGeneration: options.GetOption(Metadata.AutoXmlDocCommentGeneration),
              TabSize: options.GetOption(FormattingOptions.TabSize),
              UseTabs: options.GetOption(FormattingOptions.UseTabs),
              NewLine: options.GetOption(FormattingOptions.NewLine));
    }
}
