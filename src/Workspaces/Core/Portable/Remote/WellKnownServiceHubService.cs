// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal enum WellKnownServiceHubService
    {
        None = 0,
        RemoteHost = 1,
        // obsolete: CodeAnalysis = 2,
        // obsolete: RemoteSymbolSearchUpdateService = 3,
        // obsolete: RemoteDesignerAttributeService = 4,
        // obsolete: RemoteProjectTelemetryService = 5,
        // obsolete: RemoteTodoCommentsService = 6,
        RemoteLanguageServer = 7,
        IntelliCode = 8,
        Razor = 9,

        // owned by Unit Testing team:
        UnitTestingAnalysisService = 10,
        LiveUnitTestingBuildService = 11,
        UnitTestingSourceLookupService = 12,
    }
}
