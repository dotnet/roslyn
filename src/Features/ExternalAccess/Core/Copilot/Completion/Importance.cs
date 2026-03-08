// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot.Completion;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;
#endif

internal static class Importance
{
    public const int Lowest = 0;
    public const int Highest = 100;

    public const int Default = Lowest;
}
