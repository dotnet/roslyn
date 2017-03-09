// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    /// <summary>
    /// A subtype of <see cref="SignatureHelpService"/> that aggregates signatures from one or more <see cref="SignatureHelpProvider"/>s.
    /// </summary>
    internal abstract class SignatureHelpServiceWithProviders : SignatureHelpService
    {
        private readonly Workspace _workspace;
        private readonly string _language;

        public SignatureHelpServiceWithProviders(Workspace workspace, string language)
        {
            _workspace = workspace;
            _language = language;
        }

        protected virtual ImmutableArray<SignatureHelpProvider> GetBuiltInProviders()
        {
            return ImmutableArray<SignatureHelpProvider>.Empty;
        }

        private ImmutableArray<SignatureHelpProvider> _importedProviders;

        private IEnumerable<SignatureHelpProvider> GetImportedProviders()
        {
            if (_importedProviders.IsDefault)
            {
                var mefExporter = (IMefHostExportProvider)_workspace.Services.HostServices;

                var providers = ExtensionOrderer.Order(
                        mefExporter.GetExports<SignatureHelpProvider, SignatureHelpProviderMetadata>()
                        .Where(lz => lz.Metadata.Language == _language)
                        ).Select(lz => lz.Value).ToImmutableArray();

                ImmutableInterlocked.InterlockedCompareExchange(ref _importedProviders, providers, default(ImmutableArray<SignatureHelpProvider>));
            }

            return _importedProviders;
        }

        private ImmutableArray<SignatureHelpProvider> _testProviders = ImmutableArray<SignatureHelpProvider>.Empty;
        private bool _testAugmentBuiltInProviders;

        internal void SetTestProviders(IEnumerable<SignatureHelpProvider> testProviders, bool augmentBuiltInProviders = false)
        {
            _testProviders = testProviders != null
                ? testProviders.ToImmutableArray()
                : ImmutableArray<SignatureHelpProvider>.Empty;

            _testAugmentBuiltInProviders = augmentBuiltInProviders;
        }

        private ImmutableArray<SignatureHelpProvider> _providers;

        private ImmutableArray<SignatureHelpProvider> GetProviders()
        {
            if (_providers.IsDefault)
            {
                if (_testProviders.Length > 0)
                {
                    _providers = _testAugmentBuiltInProviders
                        ? GetBuiltInProviders().Concat(this.GetImportedProviders()).Concat(_testProviders).ToImmutableArray()
                        : _testProviders;
                }
                else
                {
                    _providers = GetBuiltInProviders().Concat(this.GetImportedProviders()).ToImmutableArray();
                }
            }

            return _providers;
        }

        public override bool ShouldTriggerSignatureHelp(
            SourceText text,
            int caretPosition,
            SignatureHelpTrigger trigger,
            OptionSet options = null)
        {
            if (trigger.Kind == SignatureHelpTriggerKind.Insertion)
            {
                foreach (var provider in GetProviders())
                {
                    if (provider.IsTriggerCharacter(trigger.Character))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void FilterTextuallyTriggeredProviders(
            ImmutableArray<SignatureHelpProvider> providers,
            char ch,
            out ImmutableArray<SignatureHelpProvider> triggeredProviders,
            out ImmutableArray<SignatureHelpProvider> untriggeredProviders)
        {
            var triggeredBuilder = ImmutableArray.CreateBuilder<SignatureHelpProvider>();
            var untriggeredBuilder = ImmutableArray.CreateBuilder<SignatureHelpProvider>();

            foreach (var provider in providers)
            {
                if (provider.IsTriggerCharacter(ch))
                {
                    triggeredBuilder.Add(provider);
                }
                else
                {
                    untriggeredBuilder.Add(provider);
                }
            }

            triggeredProviders = triggeredBuilder.ToImmutable();
            untriggeredProviders = untriggeredBuilder.ToImmutable();
        }

        public override async Task<SignatureList> GetSignaturesAsync(
            Document document,
            int caretPosition,
            SignatureHelpTrigger trigger = default(SignatureHelpTrigger),
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var allProviders = GetProviders();

            switch (trigger.Kind)
            {
                case SignatureHelpTriggerKind.Insertion:
                    // TODO(DustinCa): Clean up comment

                    // Separate the sig help providers into two buckets; one bucket for those that were triggered
                    // by the typed character, and those that weren't.  To keep our queries to a minimum, we first
                    // check with the textually triggered providers.  If none of those produced any sig help items
                    // then we query the other providers to see if they can produce anything viable.  This takes
                    // care of cases where the filtered set of providers didn't provide anything but one of the
                    // other providers could still be valid, but doesn't explicitly treat the typed character as
                    // a trigger character.

                    ImmutableArray<SignatureHelpProvider> triggeredProviders, untriggeredProviders;
                    FilterTextuallyTriggeredProviders(allProviders, trigger.Character, out triggeredProviders, out untriggeredProviders);

                    return await GetSignaturesAsync(triggeredProviders, document, caretPosition, trigger, options, cancellationToken).ConfigureAwait(false);

                case SignatureHelpTriggerKind.Other:
                case SignatureHelpTriggerKind.Update:
                    return await GetSignaturesAsync(allProviders, document, caretPosition, trigger, options, cancellationToken).ConfigureAwait(false);

                default:
                    return SignatureList.Empty;
            }
        }

        private async Task<SignatureList> GetSignaturesAsync(
            ImmutableArray<SignatureHelpProvider> providers,
            Document document,
            int caretPosition,
            SignatureHelpTrigger trigger = default(SignatureHelpTrigger),
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                options = options ?? document.Project.Solution.Workspace.Options;

                SignatureList bestSignatureList = SignatureList.Empty;

                // TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
                // to the extension crashing.
                foreach (var provider in providers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var context = new SignatureContext(provider, document, caretPosition, trigger, options, cancellationToken);

                    await provider.ProvideSignaturesAsync(context).ConfigureAwait(false);
                    if (context.Items.Count >= 0 && context.ApplicableSpan.IntersectsWith(caretPosition))
                    {
                        var currentSignatureList = context.ToSignatureList();

                        // If another provider provides sig help items, then only take them if they
                        // start after the last batch of items.  i.e. we want the set of items that
                        // conceptually are closer to where the caret position is.  This way if you have:
                        //
                        //  Foo(new Bar($$
                        //
                        // Then invoking sig help will only show the items for "new Bar(" and not also
                        // the items for "Foo(..."
                        if (IsBetter(bestSignatureList, context.ApplicableSpan))
                        {
                            bestSignatureList = currentSignatureList;
                        }
                    }
                }

                return bestSignatureList;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private bool IsBetter(SignatureList bestItems, TextSpan? currentTextSpan)
        {
            // If we have no best text span, then this span is definitely better.
            if (bestItems == null)
            {
                return true;
            }

            // Otherwise we want the one that is conceptually the innermost signature.  So it's
            // only better if the distance from it to the caret position is less than the best
            // one so far.
            return currentTextSpan.Value.Start > bestItems.ApplicableSpan.Start;
        }

        private SignatureHelpProvider GetProvider(SignatureHelpItem item)
        {
            string providerName;
            if (item.Properties.TryGetValue("Provider", out providerName))
            {
                return this.GetProviders().FirstOrDefault(p => p.Name == providerName);
            }

            return null;
        }

        private SignatureHelpProvider GetProvider(SignatureHelpParameter parameter)
        {
            string providerName;
            if (parameter.Properties.TryGetValue("Provider", out providerName))
            {
                return this.GetProviders().FirstOrDefault(p => p.Name == providerName);
            }

            return null;
        }

        public override Task<ImmutableArray<TaggedText>> GetItemDocumentationAsync(Document document, SignatureHelpItem item, CancellationToken cancellationToken)
        {
            var provider = GetProvider(item);
            if (provider != null)
            {
                return provider.GetItemDocumentationAsync(document, item, cancellationToken);
            }
            else
            {
                return EmptyTextTask;
            }
        }

        public override Task<ImmutableArray<TaggedText>> GetParameterDocumentationAsync(Document document, SignatureHelpParameter parameter, CancellationToken cancellationToken)
        {
            var provider = GetProvider(parameter);
            if (provider != null)
            {
                return provider.GetParameterDocumentationAsync(document, parameter, cancellationToken);
            }
            else
            {
                return EmptyTextTask;
            }
        }
    }
}
