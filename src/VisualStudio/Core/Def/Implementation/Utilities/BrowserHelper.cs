// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal static class BrowserHelper
    {
        /// <summary>
        /// unique VS session id
        /// </summary>
        private static readonly string s_escapedRequestId = Uri.EscapeDataString(Guid.NewGuid().ToString());

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

        public static Uri CreateBingQueryUri(DiagnosticData diagnostic)
            => CreateBingQueryUri(diagnostic.Id, diagnostic.ENUMessageForBingSearch, diagnostic.Language);

        public static Uri CreateBingQueryUri(DiagnosticDescriptor descriptor, string language)
            => CreateBingQueryUri(descriptor.Id, descriptor.GetBingHelpMessage(), language);

        private static Uri CreateBingQueryUri(string errorCode, string title, string language)
            => new Uri("https://bingdev.cloudapp.net/BingUrl.svc/Get" +
                "?selectedText=" + Uri.EscapeDataString(title ?? string.Empty) +
                "&mainLanguage=" + Uri.EscapeDataString(language ?? string.Empty) +
                "&requestId=" + s_escapedRequestId +
                "&errorCode=" + Uri.EscapeDataString(errorCode ?? string.Empty));

        public static void StartBrowser(Uri uri)
        {
            VsShellUtilities.OpenSystemBrowser(uri.AbsoluteUri);
        }
    }
}
