// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.ErrorReporting;

internal interface IErrorReportingService : IWorkspaceService
{
    /// <summary>
    /// Name of the host to be used in error messages (e.g. "Visual Studio").
    /// </summary>
    string HostDisplayName { get; }

    /// <summary>
    /// Show global error info.
    ///
    /// this kind error info should be something that affects whole roslyn such as
    /// background compilation is disabled due to memory issue and etc
    /// </summary>
    void ShowGlobalErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception, params InfoBarUI[] items);

    void ShowDetailedErrorInfo(Exception exception);

    void ShowFeatureNotAvailableErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception);
}
