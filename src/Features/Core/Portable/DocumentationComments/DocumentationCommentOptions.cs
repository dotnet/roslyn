// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.DocumentationComments
{
    internal static class DocumentationCommentOptions
    {
        public static readonly PerLanguageOption2<bool> AutoXmlDocCommentGeneration = new(nameof(DocumentationCommentOptions), nameof(AutoXmlDocCommentGeneration), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation(language => language == LanguageNames.VisualBasic ? "TextEditor.%LANGUAGE%.Specific.AutoComment" : "TextEditor.%LANGUAGE%.Specific.Automatic XML Doc Comment Generation"));
    }

    [ExportOptionProvider, Shared]
    internal sealed class DocumentationCommentOptionsProvider : IOptionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentationCommentOptionsProvider()
        {
        }

        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(DocumentationCommentOptions.AutoXmlDocCommentGeneration);
    }
}
