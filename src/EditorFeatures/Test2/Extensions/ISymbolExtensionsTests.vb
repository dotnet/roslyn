' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
    <[UseExportProvider]>
    Public Class ISymbolExtensionsTests
        Inherits TestBase

        Private Shared Async Function TestIsAccessibleWithinAsync(workspaceDefinition As XElement, expectedVisible As Boolean) As Tasks.Task
            Using workspace = EditorTestWorkspace.Create(workspaceDefinition)
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, cursorPosition)
                Dim namedTypeSymbol = semanticModel.GetEnclosingNamedType(cursorPosition, CancellationToken.None)

                Dim actualVisible = symbol.IsAccessibleWithin(namedTypeSymbol)

                Assert.Equal(expectedVisible, actualVisible)
            End Using
        End Function

        <Fact>
        Public Async Function TestIsAccessibleWithin_ProtectedInternal() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
            public class Program { protected internal void M() { } }
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
            class C { void M() { Program.$$M(); } }
        </Document>
    </Project>
</Workspace>
            Await TestIsAccessibleWithinAsync(workspace, False)
        End Function

        <Fact>
        Public Async Function TestIsAccessibleWithin_ProtectedInternal_InternalsVisibleTo() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
            using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("CSharpAssembly2")]
public class Program { protected internal static int F; }
        </Document>
    </Project>
    <Project Language="C#" AssemblyName="CSharpAssembly2" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
            class C { void M() { var f = Program.$$F; } }
        </Document>
    </Project>
</Workspace>
            Await TestIsAccessibleWithinAsync(workspace, True)
        End Function

        <Fact>
        Public Async Function TestIsAccessibleWithin_ProtectedInternal_WrongInternalsVisibleTo() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" AssemblyName="CSharpAssembly" CommonReferences="true">
        <Document>
            using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("NonExisting")]
public class Program { protected internal static int F; }
        </Document>
    </Project>
    <Project Language="C#" CommonReferences="true">
        <ProjectReference>CSharpAssembly</ProjectReference>
        <Document>
            class C { void M() { var f = Program.$$F; } }
        </Document>
    </Project>
</Workspace>
            Await TestIsAccessibleWithinAsync(workspace, False)
        End Function

        <Fact>
        Public Async Function TestIsAccessibleWithin_PrivateInsideNestedType() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Outer
{
    private static int field;
    class Inner
    {
        private int consumer = $$field;
    }
}        </Document>
    </Project>
</Workspace>
            Await TestIsAccessibleWithinAsync(workspace, True)
        End Function

        <Fact>
        Public Async Function TestIsAccessibleWithin_ProtectedInsideNestedType() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
class Outer
{
    protected static int field;
    class Inner
    {
        private int consumer = $$field;
    }
}        </Document>
    </Project>
</Workspace>
            Await TestIsAccessibleWithinAsync(workspace, True)
        End Function

    End Class
End Namespace
