// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Telemetry;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [ExportWorkspaceService(typeof(IErrorReportingService), ServiceLayer.Test), Shared]
    internal sealed class TestErrorReportingService : IErrorReportingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestErrorReportingService()
        {
        }

        public Action<string> OnError { get; set; } = message => Assert.False(true, message);

        public string HostDisplayName
            => "Test Host";

        public void ShowDetailedErrorInfo(Exception exception)
            => OnError(exception.Message);

        public void ShowGlobalErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception, params InfoBarUI[] items)
            => OnError(message);

        public void ShowFeatureNotAvailableErrorInfo(string message, TelemetryFeatureName featureName, Exception? exception)
            => OnError($"{message} {exception}");
    }
}
