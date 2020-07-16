// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Specialized;
using System.IO;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class VBCSCompiler
    {
        public static int Main(string[] args)
        {
            NameValueCollection appSettings;
            try
            {
#if BOOTSTRAP
                ExitingTraceListener.Install();
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
                CompilerServerLogger.LogException(ex, "Error loading application settings");
            }

            try
            {
                var controller = new BuildServerController(appSettings);
                return controller.Run(args);
            }
            catch (FileNotFoundException e)
            {
                // Assume the exception was the result of a missing compiler assembly.
                LogException(e);
            }
            catch (TypeInitializationException e) when (e.InnerException is FileNotFoundException)
            {
                // Assume the exception was the result of a missing compiler assembly.
                LogException((FileNotFoundException)e.InnerException);
            }
            return CommonCompiler.Failed;
        }

        private static void LogException(FileNotFoundException e)
        {
            CompilerServerLogger.LogException(e, "File not found");
        }
    }
}
