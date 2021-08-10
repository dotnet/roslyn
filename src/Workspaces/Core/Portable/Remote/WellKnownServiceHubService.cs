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
        // obsolete: RemoteLanguageServer = 7,
        IntelliCode = 8,
        // obsolete: Razor = 9,

        // owned by Unit Testing team:
        // obsolete: UnitTestingAnalysisService = 10,
        // obsolete: LiveUnitTestingBuildService = 11,
        // obsolete: UnitTestingSourceLookupService = 12,
    }
}
