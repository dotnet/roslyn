// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare
{
    internal static class LanguageServicesUtils
    {
        private const string LanguageServerProviderServiceName = "languageServerProvider";

        public static string GetLanguageServerProviderServiceName(string[] contentTypes)
        {
            Requires.NotNullOrEmpty(contentTypes, nameof(contentTypes));
            return GetLanguageServerProviderServiceName(GetContentTypesName(contentTypes));
        }

        public static string GetLanguageServerProviderServiceName(string lspServiceName)
        {
            return LanguageServerProviderServiceName + "-" + lspServiceName;
        }

        public static string GetContentTypesName(string[] contentTypes) => string.Join("-", contentTypes.OrderBy(c => c).ToArray());

        public static bool IsContentTypeRemote(string contentType)
        {
            return contentType.EndsWith("-remote");
        }

        public static bool HasVisualStudioLspCapability(this ClientCapabilities clientCapabilities)
        {
            if (clientCapabilities is VSClientCapabilities vsClientCapabilities)
            {
                return vsClientCapabilities.SupportsVisualStudioExtensions;
            }

            return false;
        }
    }
}
