// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
    internal sealed class RemoteWorkspaceTelemetryService : AbstractWorkspaceTelemetryService
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteWorkspaceTelemetryService(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        protected override ILogger CreateLogger(TelemetrySession telemetrySession)
            => AggregateLogger.Create(
                TelemetryLogger.Create(telemetrySession, _globalOptions),
                Logger.GetLogger());
    }
}
