// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    /// <summary>
    /// A subtype of <see cref="SignatureHelpService"/> that aggregates signatures from one or more <see cref="ISignatureHelpProvider"/>s.
    /// </summary>
    internal abstract class SignatureHelpServiceWithProviders : SignatureHelpService
    {
        private ImmutableArray<ISignatureHelpProvider> _testProviders = ImmutableArray<ISignatureHelpProvider>.Empty;
        private bool _testAugmentBuiltInProviders;

        internal void SetTestProviders(IEnumerable<ISignatureHelpProvider> testProviders, bool augmentBuiltInProviders = false)
        {
            _testProviders = testProviders != null
                ? testProviders.ToImmutableArray()
                : ImmutableArray<ISignatureHelpProvider>.Empty;

            _testAugmentBuiltInProviders = augmentBuiltInProviders;
        }

        protected virtual ImmutableArray<ISignatureHelpProvider> GetBuiltInProviders()
        {
            return ImmutableArray<ISignatureHelpProvider>.Empty;
        }

        public ImmutableArray<ISignatureHelpProvider> GetProviders()
        {
            if (_testProviders.Length > 0)
            {
                return _testAugmentBuiltInProviders
                    ? GetBuiltInProviders().Concat(_testProviders)
                    : _testProviders;
            }
            else
            {
                return GetBuiltInProviders();
            }
        }

        public override async Task<SignatureHelpItems> GetSignaturesAsync(
            ImmutableArray<ISignatureHelpProvider> providers,
            Document document,
            int caretPosition,
            SignatureHelpTrigger trigger = default(SignatureHelpTrigger),
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                SignatureHelpItems bestItems = null;

                // TODO(cyrusn): We're calling into extensions, we need to make ourselves resilient
                // to the extension crashing.
                foreach (var provider in providers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var currentItems = await provider.GetItemsAsync(document, caretPosition, trigger, cancellationToken).ConfigureAwait(false);
                    if (currentItems != null && currentItems.ApplicableSpan.IntersectsWith(caretPosition))
                    {
                        // If another provider provides sig help items, then only take them if they
                        // start after the last batch of items.  i.e. we want the set of items that
                        // conceptually are closer to where the caret position is.  This way if you have:
                        //
                        //  Foo(new Bar($$
                        //
                        // Then invoking sig help will only show the items for "new Bar(" and not also
                        // the items for "Foo(..."
                        if (IsBetter(bestItems, currentItems.ApplicableSpan))
                        {
                            bestItems = currentItems;
                        }
                    }
                }

                return bestItems;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private bool IsBetter(SignatureHelpItems bestItems, TextSpan? currentTextSpan)
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
    }
}
