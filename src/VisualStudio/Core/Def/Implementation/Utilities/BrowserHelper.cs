// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class BrowserHelper
    {
        private const string BingSearchString = "http://www.bing.com/search?form=VSHELP&q={0}";

        public static bool TryGetUri(string link, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(link) || !Uri.IsWellFormedUriString(link, UriKind.Absolute))
            {
                return false;
            }

            var absoluteUri = new Uri(link, UriKind.Absolute);
            if (absoluteUri.Scheme != Uri.UriSchemeHttp && absoluteUri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            uri = absoluteUri;
            return true;
        }

        public static Uri CreateBingQueryUri(string errorCode, string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return new Uri(string.Format(BingSearchString, errorCode));
            }

            return new Uri(string.Format(BingSearchString, errorCode + " " + title));
        }

        public static void StartBrowser(Uri uri)
        {
            VsShellUtilities.OpenSystemBrowser(uri.AbsoluteUri);
        }
    }
}
