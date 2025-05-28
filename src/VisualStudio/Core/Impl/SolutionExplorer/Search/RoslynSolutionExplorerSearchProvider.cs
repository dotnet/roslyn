// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

[AppliesToProject("CSharp | VB")]
[Export(typeof(ISearchProvider))]
[Name("DependenciesTreeSearchProvider")]
[VisualStudio.Utilities.Order(Before = "GraphSearchProvider")]
internal sealed class RoslynSolutionExplorerSearchProvider : ISearchProvider
{
    public void Search(IRelationshipSearchParameters parameters, Action<ISearchResult> resultAccumulator)
    {
        throw new NotImplementedException();
    }
}
