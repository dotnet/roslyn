// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

[UseExportProvider]
public abstract partial class AbstractCodeActionOrUserDiagnosticTest(ITestOutputHelper? logger = null)
    : AbstractCodeActionOrUserDiagnosticTest_NoEditor<
        EditorTestHostDocument,
        EditorTestHostProject,
        EditorTestHostSolution,
        EditorTestWorkspace>(logger)
{
    protected override TestComposition GetComposition()
        => EditorTestCompositions.EditorFeatures;

    private protected override EditorTestWorkspace CreateWorkspace(string workspaceMarkupOrCode, TestParameters parameters, TestComposition composition, IDocumentServiceProvider documentServiceProvider)
    {
        var workspace = EditorTestWorkspace.IsWorkspaceElement(workspaceMarkupOrCode)
           ? EditorTestWorkspace.Create(XElement.Parse(workspaceMarkupOrCode), openDocuments: false, composition: composition, documentServiceProvider: documentServiceProvider, workspaceKind: parameters.workspaceKind)
           : EditorTestWorkspace.Create(GetLanguage(), parameters.compilationOptions, parameters.parseOptions, files: [workspaceMarkupOrCode], composition: composition, documentServiceProvider: documentServiceProvider, workspaceKind: parameters.workspaceKind);
        return workspace;
    }
}
