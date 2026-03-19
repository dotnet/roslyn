// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// MUST match guids.h

using System;

namespace Roslyn.VisualStudio.DiagnosticsWindow;

internal static class GuidList
{
    public const string guidVisualStudioDiagnosticsWindowPkgString = "49e24138-9ee3-49e0-8ede-6b39f49303bf";
    public const string guidVisualStudioDiagnosticsWindowCmdSetString = "f22c2499-790a-4b6c-b0fd-b6f0491e1c9c";
    public const string guidToolWindowPersistanceString = "b2da68d7-fd1c-491a-a9a0-24f597b9f56c";

    public static readonly Guid guidVisualStudioDiagnosticsWindowCmdSet = new(guidVisualStudioDiagnosticsWindowCmdSetString);
};
