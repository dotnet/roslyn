// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim;

internal sealed class CSharpEntryPointFinder(Compilation compilation)
    : AbstractEntryPointFinder(compilation)
{
    protected override bool MatchesMainMethodName(string name)
        => name == "Main";

    public static IEnumerable<INamedTypeSymbol> FindEntryPoints(Compilation compilation)
    {
        // This differs from the VB implementation
        // (Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.EntryPointFinder) because we don't
        // ever consider forms entry points.
        var visitor = new CSharpEntryPointFinder(compilation);
        visitor.Visit(compilation.SourceModule.GlobalNamespace);
        return visitor.EntryPoints;
    }
}
