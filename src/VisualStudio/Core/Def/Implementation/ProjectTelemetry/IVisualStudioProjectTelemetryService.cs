// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectTelemetry
{
    /// <summary>
    /// In process service responsible for listening to OOP telemetry notifications.
    /// </summary>
    internal interface IVisualStudioProjectTelemetryService
    {
        /// <summary>
        /// Called by a host to let this service know that it should start background
        /// analysis of the workspace to determine project telemetry.
        /// </summary>
        void Start(CancellationToken cancellationToken);
    }
}
