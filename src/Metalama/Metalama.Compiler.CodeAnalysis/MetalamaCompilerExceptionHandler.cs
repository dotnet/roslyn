﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;

namespace Metalama.Compiler;

public static class MetalamaCompilerExceptionHandler
{
    public static void HandleException(Exception e)
    {
        try
        {
            var serviceProviderBuilder = new ServiceProviderBuilder().AddMinimalBackstageServices(
                applicationInfo: new MetalamaCompilerApplicationInfo(false, false),
                addSupportServices: true);

            serviceProviderBuilder.ServiceProvider.GetService<IExceptionReporter>()?.ReportException(e);
        }
        catch
        {
            // Don't crash when exception handling fails.
        }
    }
}
