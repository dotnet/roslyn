// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Globalization;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class Extensions
    {
        private static readonly CultureInfo USCultureInfo = new CultureInfo("en-US");

        public static string GetBingHelpMessage(this Diagnostic diagnostic, Workspace workspace = null)
        {
            var option = GetCustomTypeInBingSearchOption(workspace);

            // We use the ENU version of the message for bing search.
            return option ? diagnostic.GetMessage(USCultureInfo) : diagnostic.Descriptor.GetBingHelpMessage();
        }

        public static string GetBingHelpMessage(this DiagnosticDescriptor descriptor)
        {
            // We use the ENU version of the message for bing search.
            return descriptor.MessageFormat.ToString(USCultureInfo);
        }

        private static bool GetCustomTypeInBingSearchOption(Workspace workspace)
        {
            var workspaceForOptions = workspace ?? PrimaryWorkspace.Workspace;
            if (workspaceForOptions == null)
            {
                return false;
            }

            return workspaceForOptions.Options.GetOption(InternalDiagnosticsOptions.PutCustomTypeInBingSearch);
        }
    }
}