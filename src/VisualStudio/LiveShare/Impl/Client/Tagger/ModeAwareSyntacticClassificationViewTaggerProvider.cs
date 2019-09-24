// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Tagger
{
    /// <summary>
    /// this is almost straight copy from typescript for syntatic LSP experiement.
    /// we won't attemp to change code to follow Roslyn style until we have result of the experiement
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [TagType(typeof(IClassificationTag))]
    [ContentType(StringConstants.CSharpLspContentTypeName)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class CSharpModeAwareSyntacticClassificationViewTaggerProvider : ModeAwareSyntacticClassificationViewTaggerProvider
    {
        [ImportingConstructor]
        public CSharpModeAwareSyntacticClassificationViewTaggerProvider(
                Lazy<TextMateClassificationTaggerProvider> textMateProvider,
                Lazy<SyntacticClassificationViewTaggerProvider> serverProvider,
                CSharpLspClientServiceFactory lspClientServiceFactory)
                : base(textMateProvider, serverProvider, lspClientServiceFactory)
        {
        }
    }

    [Export(typeof(IViewTaggerProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [TagType(typeof(IClassificationTag))]
    [ContentType(StringConstants.VBLspContentTypeName)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class VBModeAwareSyntacticClassificationViewTaggerProvider : ModeAwareSyntacticClassificationViewTaggerProvider
    {
        [ImportingConstructor]
        public VBModeAwareSyntacticClassificationViewTaggerProvider(
            Lazy<TextMateClassificationTaggerProvider> textMateProvider,
            Lazy<SyntacticClassificationViewTaggerProvider> serverProvider,
            VisualBasicLspClientServiceFactory lspClientServiceFactory)
            : base(textMateProvider, serverProvider, lspClientServiceFactory)
        {
        }
    }

    internal class ModeAwareSyntacticClassificationViewTaggerProvider : IViewTaggerProvider
    {
        private readonly Lazy<TextMateClassificationTaggerProvider> _textMateProvider;
        private readonly Lazy<SyntacticClassificationViewTaggerProvider> _serverProvider;
        private readonly AbstractLspClientServiceFactory _lspClientServiceFactory;

        [ImportingConstructor]
        public ModeAwareSyntacticClassificationViewTaggerProvider(
            Lazy<TextMateClassificationTaggerProvider> textMateProvider,
            Lazy<SyntacticClassificationViewTaggerProvider> serverProvider,
            AbstractLspClientServiceFactory lspClientServiceFactory)
        {
            _textMateProvider = textMateProvider;
            _serverProvider = serverProvider;
            _lspClientServiceFactory = lspClientServiceFactory;
        }

        ITagger<TTag> IViewTaggerProvider.CreateTagger<TTag>(ITextView textView, ITextBuffer buffer)
        {
            Debug.Assert(typeof(TTag).IsAssignableFrom(typeof(IClassificationTag)));
            return CreateTagger(textView, buffer) as ITagger<TTag>;
        }

        private ITagger<IClassificationTag> CreateTagger(ITextView textView, ITextBuffer buffer)
        {
            return new ModeAwareTagger(
                () => this._textMateProvider.Value.CreateTagger<IClassificationTag>(textView, buffer),
                () => this._serverProvider.Value.CreateTagger<IClassificationTag>(textView, buffer),
                SyntacticClassificationModeSelector.GetModeSelector(_lspClientServiceFactory, buffer));
        }
    }
}
