// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class VisualBasicTestWorkspaceFixture : TestWorkspaceFixture
    {
        protected override TestWorkspace CreateWorkspace(ExportProvider exportProvider = null)
        {
            return TestWorkspace.CreateVisualBasic(
                new string[] { string.Empty },
                new VisualBasicParseOptions[] { new VisualBasicParseOptions(kind: SourceCodeKind.Regular) },
                new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                exportProvider: exportProvider);
        }
    }
}
