// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Maestro.Common;
using Maestro.Common.AzureDevOpsTokens;

namespace Microsoft.RoslynTools.Authentication
{
    internal class RoslynToolsSettings
    {
        public string GitHubToken { get; set; } = string.Empty;
        public string DevDivAzureDevOpsToken { get; set; } = string.Empty;
        public string DncEngAzureDevOpsToken { get; set; } = string.Empty;
        public bool IsCI { get; set; }

        public IRemoteTokenProvider GetGitHubTokenProvider() => new ResolvedTokenProvider(GitHubToken);
        public IAzureDevOpsTokenProvider GetDevDivAzDOTokenProvider() => GetAzdoTokenProvider(DevDivAzureDevOpsToken);
        public IAzureDevOpsTokenProvider GetDncEngAzDOTokenProvider() => GetAzdoTokenProvider(DncEngAzureDevOpsToken);

        private IAzureDevOpsTokenProvider GetAzdoTokenProvider(string token)
        {
            var azdoOptions = new AzureDevOpsTokenProviderOptions
            {
                ["default"] = new AzureDevOpsCredentialResolverOptions
                {
                    Token = token,
                    DisableInteractiveAuth = IsCI,
                }
            };
            return AzureDevOpsTokenProvider.FromStaticOptions(azdoOptions);
        }
    }
}
