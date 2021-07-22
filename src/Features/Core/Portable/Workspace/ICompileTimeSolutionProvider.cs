// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Provides a compile-time view of the current workspace solution.
    /// Workaround for Razor projects which generate both design-time and compile-time source files.
    /// TODO: remove https://github.com/dotnet/roslyn/issues/51678
    /// </summary>
    internal interface ICompileTimeSolutionProvider : IWorkspaceService
    {
        Solution GetCompileTimeSolution(Solution designTimeSolution);
    }
}
