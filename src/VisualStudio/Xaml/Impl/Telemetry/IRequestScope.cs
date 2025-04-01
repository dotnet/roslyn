// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry;

internal interface IRequestScope : IDisposable
{
    /// <summary>
    /// Record if any failure happens inside a RequestScope.
    /// Please note that this call doesn't report the exception to Telemetry or watson. This is only used to imform Xaml language service that 
    /// the RequestScope has failed, which is used to aggregate data for failure/success rates.
    /// </summary>
    void RecordFailure(Exception? exception = null);
}
