// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
{
    [Flags]
    internal enum DebugSessionFlags
    {
        /// <summary>
        /// No flags.
        /// </summary>
        None = 0,

        /// <summary>
        /// Edit and Continue has been disabled by the client.
        /// </summary>
        EditAndContinueDisabled = 0x1,
    }
}
