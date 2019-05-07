//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client
{
    internal class RoslynCompletionService : CompletionServiceWithProviders
    {
        private readonly CompletionService originalService;
        private readonly string language;

        public RoslynCompletionService(Workspace workspace, CompletionService originalService, string language)
            : base(workspace)
        {
            this.originalService = originalService ?? throw new ArgumentNullException(nameof(originalService));
            this.language = language ?? throw new ArgumentNullException(nameof(language));
            workspace.Options = workspace.Options.WithChangedOption(CompletionOptions.BlockForCompletionItems, language, false);
        }

        public override string Language => this.language;

        public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, ImmutableHashSet<string> roles = null, OptionSet options = null)
        {
            // Just ask the local service if we should trigger completion based on its rules since that determination is based on just looking at the current buffer.
            return this.originalService.ShouldTriggerCompletion(text, caretPosition, trigger, roles, options);
        }
    }
}
