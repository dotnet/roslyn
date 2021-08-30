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
    /// Serializes options marked with <see cref="ExperimentSwitchStorageLocation"/> to the experiment switch storage maintained by VS.
    /// </summary>
    internal sealed class ExperimentSwitchPersister : IOptionPersister
    {
        private readonly IVsExperimentationService? _experimentationService;

        public ExperimentSwitchPersister(IVsExperimentationService? experimentationService)
        {
            _experimentationService = experimentationService;
        }

        public bool TryFetch(OptionKey optionKey, [NotNullWhen(true)] out object? value)
        {
            if (_experimentationService == null)
            {
                value = null;
                return false;
            }

            var location = optionKey.Option.StorageLocations.OfType<ExperimentSwitchStorageLocation>().FirstOrDefault();
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
                value = _experimentationService.IsCachedFlightEnabled(location.Name);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                value = defaultValue;
            }

            return true;
        }

        public bool TryPersist(OptionKey optionKey, object? value)
            => false;
    }
}
