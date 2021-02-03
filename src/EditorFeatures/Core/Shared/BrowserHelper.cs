// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.Shared
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

        private static bool TryGetWellFormedHttpUri(string? link, [NotNullWhen(true)] out Uri? uri)
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

        public static Uri? GetHelpLink(DiagnosticDescriptor descriptor, string language)
            => GetHelpLink(descriptor.Id, descriptor.GetBingHelpMessage(), language, descriptor.HelpLinkUri);

        public static Uri? GetHelpLink(DiagnosticData data)
            => GetHelpLink(data.Id, data.ENUMessageForBingSearch, data.Language, data.HelpLink);

        private static Uri? GetHelpLink(string diagnosticId, string? title, string? language, string? rawHelpLink)
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

        private static string EscapeDataString(string? str)
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

        public static string? GetHelpLinkToolTip(DiagnosticData data)
        {
            var helpLink = GetHelpLink(data);

            if (helpLink == null)
            {
                return null;
            }

            return GetHelpLinkToolTip(data.Id, helpLink);
        }

        public static string GetHelpLinkToolTip(string diagnosticId, Uri uri)
        {
            var strUri = uri.ToString();

            var resourceName = strUri.StartsWith(BingGetApiUrl, StringComparison.Ordinal) ?
                EditorFeaturesResources.Get_help_for_0_from_Bing : EditorFeaturesResources.Get_help_for_0;

            // We make sure not to use Uri.AbsoluteUri for the url displayed in the tooltip so that the url displayed in the tooltip stays human readable.
            return string.Format(resourceName, diagnosticId) + "\r\n" + strUri;
        }
    }
}
