// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Host;

// Obsolete - used only for MSBuild workspace extensibility,
// other workspaces use IAnalyzerAssemblyLoaderProvider
public interface IAnalyzerService : IWorkspaceService
{
    IAnalyzerAssemblyLoader GetLoader();
}
