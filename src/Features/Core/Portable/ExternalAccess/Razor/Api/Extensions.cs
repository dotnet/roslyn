// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Api
{
    internal static class Extensions
    {
        private const string RazorCSharp = "RazorCSharp";

        public static bool IsRazorDocument(this Document document)
        {
            var documentPropertiesService = document.Services.GetService<DocumentPropertiesService>();
            if (documentPropertiesService != null && documentPropertiesService.DiagnosticsLspClientName == RazorCSharp)
            {
                return true;
            }

            return false;
        }
    }
}
