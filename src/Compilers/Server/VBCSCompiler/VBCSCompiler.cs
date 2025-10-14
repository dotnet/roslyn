// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class VBCSCompiler
    {
        public static int Main(string[] args)
        {
            using var logger = new CompilerServerLogger($"VBCSCompiler {Process.GetCurrentProcess().Id}");

            NameValueCollection appSettings;
            try
            {
#if BOOTSTRAP
                ExitingTraceListener.Install(logger);
#endif

#if NET472
                appSettings = System.Configuration.ConfigurationManager.AppSettings;
#else
                // Do not use AppSettings on non-desktop platforms
                appSettings = new NameValueCollection();
#endif
            }
            catch (Exception ex)
            {
                // It is possible for AppSettings to throw when the application or machine configuration 
                // is corrupted.  This should not prevent the server from starting, but instead just revert
                // to the default configuration.
                appSettings = new NameValueCollection();
                logger.LogException(ex, "Error loading application settings");
            }

            try
            {
                var controller = new BuildServerController(appSettings, logger);
                return controller.Run(args);
            }
            catch (Exception e)
            {
                // Assume the exception was the result of a missing compiler assembly.
                logger.LogException(e, "Cannot start server");
            }

            return CommonCompiler.Failed;
        }
    }
}
