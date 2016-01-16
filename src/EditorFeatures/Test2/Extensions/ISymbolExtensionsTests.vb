' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

    Public Class ISymbolExtensionsTests
        Inherits TestBase

        Private Async Function TestIsAccessibleWithinAsync(workspaceDefinition As XElement, expectedVisible As Boolean) As Tasks.Task
            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(workspaceDefinition)
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim tree = Await document.GetSyntaxTreeAsync()
                Dim commonSyntaxToken = Await tree.GetTouchingTokenAsync(cursorPosition, Nothing)

                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim symbol = semanticModel.GetSymbols(commonSyntaxToken, document.Project.Solution.Workspace, bindLiteralsToUnderlyingType:=False, cancellationToken:=Nothing).First()
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
