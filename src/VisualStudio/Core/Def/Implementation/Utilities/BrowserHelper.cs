// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class BrowserHelper
    {
        /// <summary>
        /// Unique VS session id. 
        /// Internal for testing.
        /// TODO: Revisit - static non-deterministic data https://github.com/dotnet/roslyn/issues/39415
        /// </summary>
        internal static readonly string EscapedRequestId = Guid.NewGuid().ToString();

        private const string BingGetApiUrl = "https://bingdev.cloudapp.net/BingUrl.svc/Get";
        private const int BingQueryArgumentMaxLength = 10240;

        private static bool TryGetWellFormedHttpUri(string link, out Uri uri)
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

        public static Uri GetHelpLink(DiagnosticDescriptor descriptor, string language)
            => GetHelpLink(descriptor.Id, descriptor.GetBingHelpMessage(), language, descriptor.HelpLinkUri);

        public static Uri GetHelpLink(DiagnosticData data)
            => GetHelpLink(data.Id, data.ENUMessageForBingSearch, data.Language, data.HelpLink);

        private static Uri GetHelpLink(string diagnosticId, string title, string language, string rawHelpLink)
        {
            if (string.IsNullOrWhiteSpace(diagnosticId))
            {
                return null;
            }

            if (TryGetWellFormedHttpUri(rawHelpLink, out var link))
            {
                return link;
            }

            return new Uri(BingGetApiUrl +
                "?selectedText=" + EscapeDataString(title) +
                "&mainLanguage=" + EscapeDataString(language) +
                "&requestId=" + EscapedRequestId +
                "&errorCode=" + EscapeDataString(diagnosticId));
        }

        private static string EscapeDataString(string str)
        {
            if (str == null)
            {
                return string.Empty;
            }

            try
            {
                // Uri has limit on string size (32766 characters).
                return Uri.EscapeDataString(str.Substring(0, Math.Min(str.Length, BingQueryArgumentMaxLength)));
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }
        }

        public static string GetHelpLinkToolTip(DiagnosticData data)
            => GetHelpLinkToolTip(data.Id, GetHelpLink(data));

        public static string GetHelpLinkToolTip(string diagnosticId, Uri uri)
        {
            var strUri = uri.ToString();

            var resourceName = strUri.StartsWith(BingGetApiUrl, StringComparison.Ordinal) ?
                ServicesVSResources.Get_help_for_0_from_Bing : ServicesVSResources.Get_help_for_0;

            // We make sure not to use Uri.AbsoluteUri for the url displayed in the tooltip so that the url displayed in the tooltip stays human readable.
            return string.Format(resourceName, diagnosticId) + "\r\n" + strUri;
        }

        public static void StartBrowser(string uri)
            => VsShellUtilities.OpenSystemBrowser(uri);

        public static void StartBrowser(Uri uri)
            => VsShellUtilities.OpenSystemBrowser(uri.AbsoluteUri);
    }
}
