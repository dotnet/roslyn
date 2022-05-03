// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.RoslynTools.Authentication
{
    internal class RoslynToolsSettings
    {
        public const string DefaultDevDivAzureDevOpsBaseUri = "https://devdiv.visualstudio.com/";
        public const string DefaultDncEngAzureDevOpsBaseUri = "https://dnceng.visualstudio.com/";

        public string GitHubToken { get; set; } = string.Empty;


        public string DevDivAzureDevOpsToken { get; set; } = string.Empty;
        public string DevDivAzureDevOpsBaseUri { get; set; } = DefaultDevDivAzureDevOpsBaseUri;

        public string DncEngAzureDevOpsToken { get; set; } = string.Empty;
        public string DncEngAzureDevOpsBaseUri { get; set; } = DefaultDncEngAzureDevOpsBaseUri;
    }
}
