﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attempt to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    [Export(typeof(ITaggerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.CSharpLspContentTypeName)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CSharpModeAwareSyntacticClassificationTaggerProvider : ModeAwareSyntacticClassificationTaggerProvider
    {
        [ImportingConstructor]
        public CSharpModeAwareSyntacticClassificationTaggerProvider(
            Lazy<TextMateClassificationTaggerProvider> textMateProvider,
            Lazy<SyntacticClassificationTaggerProvider> serverProvider,
            CSharpLspClientServiceFactory lspClientServiceFactory)
            : base(textMateProvider, serverProvider, lspClientServiceFactory)
        {
        }
    }

    [Export(typeof(ITaggerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [TagType(typeof(IClassificationTag))]
    [ContentType(ContentTypeNames.VBLspContentTypeName)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class VBModeAwareSyntacticClassificationTaggerProvider : ModeAwareSyntacticClassificationTaggerProvider
    {
        [ImportingConstructor]
        public VBModeAwareSyntacticClassificationTaggerProvider(
            Lazy<TextMateClassificationTaggerProvider> textMateProvider,
            Lazy<SyntacticClassificationTaggerProvider> serverProvider,
            VisualBasicLspClientServiceFactory lspClientServiceFactory)
            : base(textMateProvider, serverProvider, lspClientServiceFactory)
        {
        }
    }

    internal class ModeAwareSyntacticClassificationTaggerProvider : ITaggerProvider
    {
        private readonly Lazy<TextMateClassificationTaggerProvider> _textMateProvider;
        private readonly Lazy<SyntacticClassificationTaggerProvider> _serverProvider;
        private readonly AbstractLspClientServiceFactory _lspClientServiceFactory;

        public ModeAwareSyntacticClassificationTaggerProvider(
            Lazy<TextMateClassificationTaggerProvider> textMateProvider,
            Lazy<SyntacticClassificationTaggerProvider> serverProvider,
            AbstractLspClientServiceFactory lspClientServiceFactory)
        {
            _textMateProvider = textMateProvider;
            _serverProvider = serverProvider;
            _lspClientServiceFactory = lspClientServiceFactory;
        }

        ITagger<TTag> ITaggerProvider.CreateTagger<TTag>(ITextBuffer buffer)
        {
            Debug.Assert(typeof(TTag).IsAssignableFrom(typeof(IClassificationTag)));
            return CreateTagger(buffer) as ITagger<TTag>;
        }

        private ITagger<IClassificationTag> CreateTagger(ITextBuffer buffer)
        {
            return new ModeAwareTagger(
                () => this._textMateProvider.Value.CreateTagger<IClassificationTag>(buffer),
                () => this._serverProvider.Value.CreateTagger<IClassificationTag>(buffer),
                SyntacticClassificationModeSelector.GetModeSelector(_lspClientServiceFactory, buffer));
        }
    }

}
