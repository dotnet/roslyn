// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    [DataContract]
    internal readonly struct ManagedModuleUpdates
    {
        [DataMember(Order = 0)]
        public readonly ManagedModuleUpdateStatus Status;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<ManagedModuleUpdate> Updates;

        public ManagedModuleUpdates(ManagedModuleUpdateStatus status, ImmutableArray<ManagedModuleUpdate> updates)
        {
            Status = status;
            Updates = updates;
        }
    }
}

