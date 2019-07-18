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
        private static readonly string s_requestId = Guid.NewGuid().ToString();

        private const string BingSearchString = "https://bingdev.cloudapp.net/BingUrl.svc/Get?selectedText={0}&mainLanguage={1}&projectType={2}&requestId={3}&clientId={4}&errorCode={5}";

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

        public static Uri CreateBingQueryUri(Workspace workspace, DiagnosticData diagnostic)
        {
            var errorCode = diagnostic.Id;
            var title = diagnostic.ENUMessageForBingSearch;
            workspace.GetLanguageAndProjectType(diagnostic.ProjectId, out var language, out var projectType);

            return CreateBingQueryUri(errorCode, title, language, projectType);
        }

        public static Uri CreateBingQueryUri(string errorCode, string title, string language, string projectType)
        {
            errorCode ??= string.Empty;
            title ??= string.Empty;
            language ??= string.Empty;
            projectType ??= string.Empty;

            var url = string.Format(BingSearchString, Uri.EscapeDataString(title), Uri.EscapeDataString(language), Uri.EscapeDataString(projectType), Uri.EscapeDataString(s_requestId), Uri.EscapeDataString(string.Empty), Uri.EscapeDataString(errorCode));
            return new Uri(url);
        }

        public static void StartBrowser(Uri uri)
        {
            VsShellUtilities.OpenSystemBrowser(uri.AbsoluteUri);
        }
    }
}
