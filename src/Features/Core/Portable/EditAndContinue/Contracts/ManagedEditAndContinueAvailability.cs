// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    internal readonly struct ManagedEditAndContinueAvailability
    {
        /// <returns>
        /// <see cref="ManagedEditAndContinueAvailabilityStatus.ModuleNotLoaded"/> if no instance of the module is loaded (does not indicate error).
        /// <see cref="ManagedEditAndContinueAvailabilityStatus.Available"/> if all loaded instances allow EnC.
        /// Returns error code and a corresponding localized error message otherwise.
        /// </returns>
        [DataMember(Order = 0)]
        public readonly ManagedEditAndContinueAvailabilityStatus Status;

        [DataMember(Order = 1)]
        public readonly string? LocalizedMessage;

        public ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus status, string? localizedMessage = null)
        {
            Status = status;
            LocalizedMessage = localizedMessage;
        }
    }
}
