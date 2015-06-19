// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    [ExportLanguageService(typeof(ICompletionService), InteractiveLanguageNames.InteractiveCommand), Shared]
    internal class InteractiveCommandCompletionService : AbstractCompletionService
    {
        private readonly ImmutableList<ICompletionProvider> _completionProviders;

        [ImportingConstructor]
        public InteractiveCommandCompletionService(
            [ImportMany] IEnumerable<Lazy<ICompletionProvider, OrderableLanguageMetadata>> completionProviders)
        {
            _completionProviders =
                ExtensionOrderer.Order(
                    completionProviders.Where(l => l.Metadata.Language == InteractiveLanguageNames.InteractiveCommand))
                                .Select(l => l.Value)
                                .ToImmutableList();
        }

        public override IEnumerable<ICompletionProvider> GetDefaultCompletionProviders()
        {
            return _completionProviders;
        }

        public override Task<TextSpan> GetDefaultTrackingSpanAsync(Document document, int position, CancellationToken cancellationToken)
        {
            return SpecializedTasks.Default<TextSpan>();
        }

        protected override bool TriggerOnBackspace(SourceText text, int position, CompletionTriggerInfo triggerInfo, OptionSet options)
        {
            return false;
        }

        protected override string GetLanguageName()
        {
            return InteractiveLanguageNames.InteractiveCommand;
        }
    }
}
