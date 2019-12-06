// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Completion
{
    internal class RoslynCompletionService : CompletionServiceWithProviders
    {
        private readonly CompletionService _originalService;
        private readonly string _language;

        public RoslynCompletionService(Workspace workspace, CompletionService originalService, string language)
            : base(workspace)
        {
            _originalService = originalService ?? throw new ArgumentNullException(nameof(originalService));
            _language = language ?? throw new ArgumentNullException(nameof(language));
            workspace.Options = workspace.Options.WithChangedOption(CompletionOptions.BlockForCompletionItems, language, false);
        }

        public override string Language => _language;

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string> roles = null, OptionSet options = null)
        {
            // Just ask the local service if we should trigger completion based on its rules since that determination is based on just looking at the current buffer.
            return _originalService.ShouldTriggerCompletion(text, caretPosition, trigger, roles, options);
        }
    }
}
