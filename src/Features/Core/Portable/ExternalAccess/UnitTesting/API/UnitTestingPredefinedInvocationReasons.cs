// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingPredefinedInvocationReasons
    {
        public const string SemanticChanged = nameof(SemanticChanged);
        public const string Reanalyze = nameof(Reanalyze);
        public const string ProjectConfigurationChanged = nameof(ProjectConfigurationChanged);

        public const string HighPriority = nameof(HighPriority);
    }
}
