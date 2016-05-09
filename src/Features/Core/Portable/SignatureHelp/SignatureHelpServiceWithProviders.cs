// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

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
    }
}
