// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using System;
using System.Collections.Specialized;
using System.Configuration;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal static class VBCSCompiler 
    {
        public static int Main(string[] args)
        {
            NameValueCollection appSettings;
            try
            {
                appSettings = ConfigurationManager.AppSettings;
            }
            catch (Exception ex)
            {
                // It is possible for AppSettings to throw when the application or machine configuration 
                // is corrupted.  This should not prevent the server from starting, but instead just revert
                // to the default configuration.
                appSettings = new NameValueCollection();
                CompilerServerLogger.LogException(ex, "Error loading application settings");
            }

            var controller = new DesktopBuildServerController(appSettings);
            return controller.Run(args);
        }
    }
}
