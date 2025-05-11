// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.ErrorReporting;

[ExportWorkspaceService(typeof(IErrorReportingService), ServiceLayer.Editor), Shared]
internal sealed class EditorErrorReportingService : IErrorReportingService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditorErrorReportingService()
    {
    }

    public string HostDisplayName => "host";

    public void ShowDetailedErrorInfo(Exception exception)
        => Logger.Log(FunctionId.Extension_Exception, exception.StackTrace);

    public void ShowGlobalErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception, params InfoBarUI[] items)
        => Logger.Log(FunctionId.Extension_Exception, message);

    public void ShowFeatureNotAvailableErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception)
    {
        // telemetry has already been reported
    }
}
