' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.Threading
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

        Private Sub TestIsAccessibleWithin(workspaceDefinition As XElement, expectedVisible As Boolean)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceDefinition)
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim commonSyntaxToken = document.GetSyntaxTreeAsync().Result.GetTouchingToken(cursorPosition, Nothing)

                Dim semanticModel = document.GetSemanticModelAsync().Result
                Dim symbol = semanticModel.GetSymbols(commonSyntaxToken, document.Project.Solution.Workspace, bindLiteralsToUnderlyingType:=False, cancellationToken:=Nothing).First()
                Dim namedTypeSymbol = semanticModel.GetEnclosingNamedType(cursorPosition, CancellationToken.None)

                Dim actualVisible = symbol.IsAccessibleWithin(namedTypeSymbol)

                Assert.Equal(expectedVisible, actualVisible)
            End Using
        End Sub

        <WpfFact>
        Public Sub TestIsAccessibleWithin_ProtectedInternal()
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
            TestIsAccessibleWithin(workspace, False)
        End Sub

        <WpfFact>
        Public Sub TestIsAccessibleWithin_ProtectedInternal_InternalsVisibleTo()
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
            TestIsAccessibleWithin(workspace, True)
        End Sub

        <WpfFact>
        Public Sub TestIsAccessibleWithin_ProtectedInternal_WrongInternalsVisibleTo()
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
            TestIsAccessibleWithin(workspace, False)
        End Sub

        <WpfFact>
        Public Sub TestIsAccessibleWithin_PrivateInsideNestedType()
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
            TestIsAccessibleWithin(workspace, True)
        End Sub

        <WpfFact>
        Public Sub TestIsAccessibleWithin_ProtectedInsideNestedType()
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
            TestIsAccessibleWithin(workspace, True)
        End Sub

    End Class
End Namespace
