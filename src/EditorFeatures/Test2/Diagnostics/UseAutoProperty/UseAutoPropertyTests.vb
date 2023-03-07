' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CSharp.UseAutoProperty
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseAutoProperty

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.UseAutoProperty
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseAutoProperty)>
    Public Class UseAutoPropertyTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As (DiagnosticAnalyzer, CodeFixProvider)
            If language = LanguageNames.CSharp Then
                Return (New CSharpUseAutoPropertyAnalyzer(), New CSharpUseAutoPropertyCodeFixProvider())
            ElseIf language = LanguageNames.VisualBasic Then
                Return (New VisualBasicUseAutoPropertyAnalyzer(), New VisualBasicUseAutoPropertyCodeFixProvider())
            Else
                Throw New Exception()
            End If
        End Function

        <Fact>
        Public Async Function TestMultiFile_CSharp() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
partial class C
{
    int $$i;
}
                        </Document>
                        <Document FilePath='Test2.cs'>
partial class C
{
    int P { get { return i; } }
}
                        </Document>
                    </Project>
                </Workspace>

            Await TestAsync(input, fileNameToExpected:=
                 New Dictionary(Of String, String) From {
                    {"Test1.cs",
<text>
partial class C
{
}
</text>.Value.Trim()},
                    {"Test2.cs",
<text>
partial class C
{
    int P { get; }
}
</text>.Value.Trim()}
                })
        End Function

        <Fact>
        Public Async Function TestMultiFile_VisualBasic() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.vb'>
partial class C
    dim $$i as Integer
end class
                        </Document>
                        <Document FilePath='Test2.vb'>
partial class C
    property P as Integer
        get
            return i
        end get
    end property
end class
                        </Document>
                    </Project>
                </Workspace>

            Await TestAsync(input, fileNameToExpected:=
                 New Dictionary(Of String, String) From {
                    {"Test1.vb",
<text>
partial class C
end class
</text>.Value.Trim()},
                    {"Test2.vb",
<text>
partial class C
    ReadOnly property P as Integer
end class
</text>.Value.Trim()}
                })
        End Function

        <Fact>
        <WorkItem("https://github.com/dotnet/roslyn/issues/20855")>
        <WorkItem("https://github.com/dotnet/roslyn/issues/25408")>
        Public Async Function TestLinkedFile() As Task
            Dim input =
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="LinkedProj" Name="CSProj.1">
                        <Document FilePath='C.cs'>
partial class C
{
    $$int i;

    public int P { get { return i; } }

    public C()
    {
        this.i = 0;
    }
}
                        </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" AssemblyName="LinkedProj" Name="CSProj.2">
                        <Document IsLinkFile="true" LinkProjectName="CSProj.1" LinkFilePath="C.cs"/>
                    </Project>
                </Workspace>

            Dim expectedText = "
partial class C
{
    public int P { get; }

    public C()
    {
        this.P = 0;
    }
}".Trim()

            Await TestAsync(input, verifySolutions:=
                Async Function(oldSolution, newSolution)
                    Dim documents = newSolution.Projects.SelectMany(Function(p) p.Documents).
                                                         Where(Function(d) d.Name = "C.cs")
                    Assert.Equal(2, documents.Count())

                    For Each doc In documents
                        Dim text = (Await doc.GetTextAsync()).ToString().Trim()
                        Assert.Equal(expectedText, text)
                    Next
                End Function)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66320")>
        Public Async Function TestChangeReferencesInMultipleFiles_CSharp() As Task
            Dim input =
                <Workspace>
                    <Project Language='C#' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.cs'>
partial class C
{
    private int test = 0;

    public int $$TestProp { get => test; set => test = value; }
}
                        </Document>
                        <Document FilePath='Test2.cs'>
partial class C
{
    private void Bla()
    {
        Console.WriteLine(test);
    }
}
                        </Document>
                    </Project>
                </Workspace>

            Await TestAsync(input, fileNameToExpected:=
                 New Dictionary(Of String, String) From {
                    {"Test1.cs",
<text>
partial class C
{
    public int TestProp { get; set; } = 0;
}
</text>.Value.Trim()},
                    {"Test2.cs",
<text>
partial class C
{
    private void Bla()
    {
        Console.WriteLine(TestProp);
    }
}
</text>.Value.Trim()}
                })
        End Function

        <Fact>
        Public Async Function TestChangeReferencesInMultipleFiles_VisualBasic() As Task
            Dim input =
                <Workspace>
                    <Project Language='Visual Basic' AssemblyName='CSharpAssembly1' CommonReferences='true'>
                        <Document FilePath='Test1.vb'>
partial class C
    dim $$test as Integer

    property TestProp as Integer
        get
            return test
        end get
        set(value as Integer)
            test = value
        end set
    end property
end class
                        </Document>
                        <Document FilePath='Test2.vb'>
partial class C
    private sub Bla()
        Console.WriteLine(test)
    end sub
end class
                        </Document>
                    </Project>
                </Workspace>

            Await TestAsync(input, fileNameToExpected:=
                 New Dictionary(Of String, String) From {
                    {"Test1.vb",
<text>
partial class C
    property TestProp as Integer
end class
</text>.Value.Trim()},
                    {"Test2.vb",
<text>
partial class C
    private sub Bla()
        Console.WriteLine(TestProp)
    end sub
end class
</text>.Value.Trim()}
                })
        End Function
    End Class
End Namespace
