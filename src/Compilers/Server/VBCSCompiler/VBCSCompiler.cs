// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class VBCSCompiler
    {
        public static int Main(string[] args)
        {
            // Pre-parse arguments to extract the log file path and other values so the logger can be
            // initialised with the correct path before the controller is created.  Ignore parse failures
            // here as they will be properly reported when Run() is called.
            BuildServerController.ParseCommandLine(args, out var pipeName, out var shutdown, out var keepAlive, out var logFilePath);

            using var logger = new CompilerServerLogger($"VBCSCompiler {Process.GetCurrentProcess().Id}", logFilePath);

#if BOOTSTRAP
            ExitingTraceListener.Install(logger);
#endif

            try
            {
                var controller = new BuildServerController(logger);
                return controller.Run(pipeName, shutdown, keepAlive);
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
