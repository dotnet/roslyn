// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;

namespace Metalama.Compiler;

internal static class MetalamaCompilerExceptionHandler
{
    public static void HandleException(Exception e)
    {
        try
        {
            var applicationInfo = new MetalamaCompilerApplicationInfo(false, false, ImmutableArray<ISourceTransformer>.Empty);
            var initializationOptions =
                new BackstageInitializationOptions(applicationInfo) { AddSupportServices = true };

            var serviceProviderBuilder = new ServiceProviderBuilder().AddBackstageServices(initializationOptions);

            serviceProviderBuilder.ServiceProvider.GetService<IExceptionReporter>()?.ReportException(e);
        }
        catch
        {
            // Don't crash when exception handling fails.
        }
    }
}
