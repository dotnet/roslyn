// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    /// <summary>
    /// Serializes options marked with <see cref="FeatureFlagStorageLocation"/> to the feature flag storage maintained by VS.
    /// </summary>
    internal sealed class FeatureFlagPersister : IOptionPersister
    {
        private readonly IVsFeatureFlags? _featureFlags;

        public FeatureFlagPersister(IVsFeatureFlags? featureFlags)
        {
            _featureFlags = featureFlags;
        }

        public bool TryFetch(OptionKey optionKey, [NotNullWhen(true)] out object? value)
        {
            if (_featureFlags == null)
            {
                value = null;
                return false;
            }

            var location = optionKey.Option.StorageLocations.OfType<FeatureFlagStorageLocation>().FirstOrDefault();
            if (location == null)
            {
                value = null;
                return false;
            }

            if (optionKey.Option.DefaultValue is not bool defaultValue)
            {
                throw ExceptionUtilities.UnexpectedValue(optionKey.Option.DefaultValue);
            }

            try
            {
                value = _featureFlags.IsFeatureEnabled(location.Name, defaultValue);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                value = defaultValue;
            }

            return true;
        }

        public bool TryPersist(OptionKey optionKey, object? value)
        {
            if (_featureFlags == null)
            {
                return false;
            }

            var location = optionKey.Option.StorageLocations.OfType<FeatureFlagStorageLocation>().FirstOrDefault();
            if (location == null)
            {
                return false;
            }

            if (value is not bool flag)
            {
                throw ExceptionUtilities.UnexpectedValue(value);
            }

            try
            {
                ((IVsFeatureFlags2)_featureFlags).EnableFeatureFlag(location.Name, flag);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                return false;
            }

            return true;
        }
    }
}
