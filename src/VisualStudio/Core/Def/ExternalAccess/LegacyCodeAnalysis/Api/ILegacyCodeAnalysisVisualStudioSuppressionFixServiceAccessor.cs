// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;

internal interface ILegacyCodeAnalysisVisualStudioSuppressionFixServiceAccessor
{
    bool AddSuppressions(IVsHierarchy projectHierarchyOpt);
    bool AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy projectHierarchyOpt);
    bool RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy projectHierarchyOpt);
}
