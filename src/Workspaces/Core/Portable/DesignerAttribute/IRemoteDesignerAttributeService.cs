// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DesignerAttribute
{
    /// <summary>
    /// Interface to allow host (VS) to inform the OOP service to start incrementally analyzing and
    /// reporting results back to the host.
    /// </summary>
    internal interface IRemoteDesignerAttributeService
    {
        Task StartScanningForDesignerAttributesAsync(CancellationToken cancellation);
    }
}
