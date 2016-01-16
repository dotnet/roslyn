' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.Utilities.VsNavInfo
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.VsNavInfo
    Public Class VsNavInfoTests

#Region "C# Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestNamespace() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace $$N { }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N")
                 },
                 presentationNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class $$C { }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C")
                 },
                 presentationNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    void $$M() { }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M()")
                 },
                 presentationNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M()")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMethod_Parameters() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    int $$M(int x, int y)
                    {
                        return x + y;
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M(int, int)")
                 },
                 presentationNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M(int, int)")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMetadata_Class1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            using System;
            class C
            {
                String$$ s;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMetadata_Class2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            using System.Text;
            class C
            {
                StringBuilder$$ sb;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Namespace]("Text"),
                    [Class]("StringBuilder")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System.Text"),
                    [Class]("StringBuilder")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMetadata_Ctor1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            using System.Text;
            class C
            {
                StringBuilder sb = new StringBuilder$$();
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Namespace]("Text"),
                    [Class]("StringBuilder"),
                    Member("StringBuilder()")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System.Text"),
                    [Class]("StringBuilder"),
                    Member("StringBuilder()")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMetadata_Ctor2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            using System;
            class C
            {
                String s = new String$$(' ', 42);
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("String(char, int)")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("String(char, int)")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMetadata_Method() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            using System;
            class C
            {
                String s = new String(' ', 42).Replace$$(' ', '\r');
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("Replace(char, char)")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("Replace(char, char)")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMetadata_GenericType() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            using System.Collections.Generic;
            class C
            {
                $$List&lt;int&gt; s;
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Namespace]("Collections"),
                    [Namespace]("Generic"),
                    [Class]("List<T>")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System.Collections.Generic"),
                    [Class]("List<T>")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestMetadata_GenericMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            using System;
            class C
            {
                void M()
                {
                    var a = new int[] { 1, 2, 3, 4, 5 };
                    var r = Array.AsReadOnly$$(a);
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("Array"),
                    Member("AsReadOnly<T>(T[])")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("Array"),
                    Member("AsReadOnly<T>(T[])")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestNull_Parameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            class C
            {
                void M(int i$$) { }
            }
        </Document>
    </Project>
</Workspace>

            Await TestIsNullAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestNull_Local() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            class C
            {
                void M()
                {
                    int i$$;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestIsNullAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestCSharp_TestNull_Label() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            class C
            {
                void M()
                {
                    label$$:
                        int i;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestIsNullAsync(workspace)
        End Function

#End Region

#Region "Visual Basic Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestNamespace() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace $$N
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N")
                 },
                 presentationNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestClass() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class $$C
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C")
                 },
                 presentationNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMember_Sub() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    Sub $$M()
                    End Sub
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M()")
                 },
                 presentationNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M()")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMember_Function() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    Function $$M() As Integer
                    End Function
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M() As Integer")
                 },
                 presentationNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M() As Integer")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMember_Parameters() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    Function $$M(x As Integer, y As Integer) As Integer
                    End Function
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M(Integer, Integer) As Integer")
                 },
                 presentationNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N"),
                    [Class]("C"),
                    Member("M(Integer, Integer) As Integer")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMetadata_Class1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Imports System
            Class C
                Dim s As String$$
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMetadata_Class2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Imports System.Text
            Class C
                Dim s As StringBuilder$$
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Namespace]("Text"),
                    [Class]("StringBuilder")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System.Text"),
                    [Class]("StringBuilder")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMetadata_Ctor1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Imports System.Text
            Class C
                Dim s As New StringBuilder$$()
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Namespace]("Text"),
                    [Class]("StringBuilder"),
                    Member("New()")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System.Text"),
                    [Class]("StringBuilder"),
                    Member("New()")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMetadata_Ctor2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Imports System
            Class C
                Dim s As String = New String$$(" "c, 42)
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("New(Char, Integer)")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("New(Char, Integer)")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMetadata_Method() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Imports System
            Class C
                Dim s As String = New String(" "c, 42).Replace$$(" "c, "."c)
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("Replace(Char, Char) As String")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("String"),
                    Member("Replace(Char, Char) As String")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMetadata_GenericType() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Imports System.Collections.Generic
            Class C
                Dim s As List$$(Of Integer)
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Namespace]("Collections"),
                    [Namespace]("Generic"),
                    [Class]("List(Of T)")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System.Collections.Generic"),
                    [Class]("List(Of T)")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestMetadata_GenericMethod() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VisualBasicTestAssembly">
        <Document>
            Imports System
            Class C
                Sub M()
                    Dim a = New Integer() { 1, 2, 3, 4, 5 }
                    Dim r = Array.AsReadOnly$$(a)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                 canonicalNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("Array"),
                    Member("AsReadOnly(Of T)(T()) As System.Collections.ObjectModel.ReadOnlyCollection(Of T)")
                 },
                 presentationNodes:={
                    Package("Z:\FxReferenceAssembliesUri"),
                    [Namespace]("System"),
                    [Class]("Array"),
                    Member("AsReadOnly(Of T)(T()) As System.Collections.ObjectModel.ReadOnlyCollection(Of T)")
                 })
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestNull_Parameter() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Class C
                Sub M(i$$ As Integer)
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestIsNullAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestNull_Local() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Class C
                Sub M()
                    Dim i$$ As Integer
                End Sub
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestIsNullAsync(workspace)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Async Function TestVisualBasic_TestNull_Label() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Class C
                void M()
                {
                Sub M()
                    label$$:
                    Dim i As Integer
                End Sub
                }
            End Class
        </Document>
    </Project>
</Workspace>

            Await TestIsNullAsync(workspace)
        End Function

#End Region

        Private Shared Async Function TestAsync(
            workspaceDefinition As XElement,
            Optional useExpandedHierarchy As Boolean = False,
            Optional canonicalNodes As NodeVerifier() = Nothing,
            Optional presentationNodes As NodeVerifier() = Nothing
        ) As Task

            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(workspaceDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
                Dim hostDocument = workspace.DocumentWithCursor
                Assert.True(hostDocument IsNot Nothing, "Test defined without cursor position")

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim position As Integer = hostDocument.CursorPosition.Value
                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, workspace).ConfigureAwait(False)
                Assert.True(symbol IsNot Nothing, $"Could not find symbol as position, {position}")

                Dim libraryService = document.Project.LanguageServices.GetService(Of ILibraryService)

                Dim project = document.Project
                Dim compilation = Await project.GetCompilationAsync()
                Dim navInfo = libraryService.NavInfoFactory.CreateForSymbol(symbol, document.Project, compilation, useExpandedHierarchy)
                Assert.True(navInfo IsNot Nothing, $"Could not retrieve nav info for {symbol.ToDisplayString()}")

                If canonicalNodes IsNot Nothing Then
                    Dim enumerator As IVsEnumNavInfoNodes = Nothing
                    IsOK(navInfo.EnumCanonicalNodes(enumerator))

                    VerifyNodes(enumerator, canonicalNodes)
                End If

                If presentationNodes IsNot Nothing Then
                    Dim enumerator As IVsEnumNavInfoNodes = Nothing
                    IsOK(navInfo.EnumPresentationNodes(CUInt(_LIB_LISTFLAGS.LLF_NONE), enumerator))

                    VerifyNodes(enumerator, presentationNodes)
                End If
            End Using
        End Function

        Private Shared Async Function TestIsNullAsync(
            workspaceDefinition As XElement,
            Optional useExpandedHierarchy As Boolean = False
        ) As Task

            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(workspaceDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
                Dim hostDocument = workspace.DocumentWithCursor
                Assert.True(hostDocument IsNot Nothing, "Test defined without cursor position")

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim position As Integer = hostDocument.CursorPosition.Value
                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, workspace).ConfigureAwait(False)
                Assert.True(symbol IsNot Nothing, $"Could not find symbol as position, {position}")

                Dim libraryService = document.Project.LanguageServices.GetService(Of ILibraryService)

                Dim project = document.Project
                Dim compilation = Await project.GetCompilationAsync()
                Dim navInfo = libraryService.NavInfoFactory.CreateForSymbol(symbol, document.Project, compilation, useExpandedHierarchy)
                Assert.Null(navInfo)
            End Using
        End Function

    End Class
End Namespace