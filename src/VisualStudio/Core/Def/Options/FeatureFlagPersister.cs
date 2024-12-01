// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Options;

internal sealed class FeatureFlagPersister
{
    private readonly IVsFeatureFlags? _featureFlags;

    public FeatureFlagPersister(IVsFeatureFlags? featureFlags)
    {
        _featureFlags = featureFlags;
    }

    public bool TryFetch(OptionKey2 optionKey, string flagName, [NotNullWhen(true)] out object? value)
    {
        if (_featureFlags == null)
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
            value = _featureFlags.IsFeatureEnabled(flagName, defaultValue);
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
            value = defaultValue;
        }

        return true;
    }

    public void Persist(string flagName, object? value)
    {
        if (value is not bool flag)
        {
            throw ExceptionUtilities.UnexpectedValue(value);
        }

        try
        {
            ((IVsFeatureFlags2?)_featureFlags)?.EnableFeatureFlag(flagName, flag);
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
        }
    }
}
