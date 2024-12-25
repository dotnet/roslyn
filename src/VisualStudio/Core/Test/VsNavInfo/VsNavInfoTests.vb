' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.Utilities.VsNavInfo
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.VsNavInfo
    <UseExportProvider, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
    Public Class VsNavInfoTests

#Region "C# Tests"

        <WpfFact>
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
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N")
                 },
                 presentationNodes:={
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C")
                 },
                 presentationNodes:={
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M()")
                 },
                 presentationNodes:={
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M()")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M(int, int)")
                 },
                 presentationNodes:={
                    PackageNode("CSharpTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M(int, int)")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    NamespaceNode("Text"),
                    ClassNode("StringBuilder")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System.Text"),
                    ClassNode("StringBuilder")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    NamespaceNode("Text"),
                    ClassNode("StringBuilder"),
                    MemberNode("StringBuilder()")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System.Text"),
                    ClassNode("StringBuilder"),
                    MemberNode("StringBuilder()")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("String(char, int)")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("String(char, int)")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("Replace(char, char)")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("Replace(char, char)")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    NamespaceNode("Collections"),
                    NamespaceNode("Generic"),
                    ClassNode("List<T>")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System.Collections.Generic"),
                    ClassNode("List<T>")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("Array"),
                    MemberNode("AsReadOnly<T>(T[])")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("Array"),
                    MemberNode("AsReadOnly<T>(T[])")
                 })
        End Function

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N")
                 },
                 presentationNodes:={
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C")
                 },
                 presentationNodes:={
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M()")
                 },
                 presentationNodes:={
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M()")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M() As Integer")
                 },
                 presentationNodes:={
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M() As Integer")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M(Integer, Integer) As Integer")
                 },
                 presentationNodes:={
                    PackageNode("VBTestAssembly"),
                    NamespaceNode("N"),
                    ClassNode("C"),
                    MemberNode("M(Integer, Integer) As Integer")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    NamespaceNode("Text"),
                    ClassNode("StringBuilder")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System.Text"),
                    ClassNode("StringBuilder")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    NamespaceNode("Text"),
                    ClassNode("StringBuilder"),
                    MemberNode("New()")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System.Text"),
                    ClassNode("StringBuilder"),
                    MemberNode("New()")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("New(Char, Integer)")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("New(Char, Integer)")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("Replace(Char, Char) As String")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("String"),
                    MemberNode("Replace(Char, Char) As String")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    NamespaceNode("Collections"),
                    NamespaceNode("Generic"),
                    ClassNode("List(Of T)")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System.Collections.Generic"),
                    ClassNode("List(Of T)")
                 })
        End Function

        <WpfFact>
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
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("Array"),
                    MemberNode("AsReadOnly(Of T)(T()) As System.Collections.ObjectModel.ReadOnlyCollection(Of T)")
                 },
                 presentationNodes:={
                    PackageNode("Z:\FxReferenceAssembliesUri"),
                    NamespaceNode("System"),
                    ClassNode("Array"),
                    MemberNode("AsReadOnly(Of T)(T()) As System.Collections.ObjectModel.ReadOnlyCollection(Of T)")
                 })
        End Function

        <WpfFact>
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

        <WpfFact>
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

        <WpfFact>
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

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim hostDocument = workspace.DocumentWithCursor
                Assert.True(hostDocument IsNot Nothing, "Test defined without cursor position")

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim position As Integer = hostDocument.CursorPosition.Value
                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, workspace).ConfigureAwait(False)
                Assert.True(symbol IsNot Nothing, $"Could not find symbol as position, {position}")

                Dim libraryService = document.GetLanguageService(Of ILibraryService)

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

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=VisualStudioTestCompositions.LanguageServices)
                Dim hostDocument = workspace.DocumentWithCursor
                Assert.True(hostDocument IsNot Nothing, "Test defined without cursor position")

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim semanticModel = Await document.GetSemanticModelAsync()
                Dim position As Integer = hostDocument.CursorPosition.Value
                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(semanticModel, position, workspace).ConfigureAwait(False)
                Assert.True(symbol IsNot Nothing, $"Could not find symbol as position, {position}")

                Dim libraryService = document.GetLanguageService(Of ILibraryService)

                Dim project = document.Project
                Dim compilation = Await project.GetCompilationAsync()
                Dim navInfo = libraryService.NavInfoFactory.CreateForSymbol(symbol, document.Project, compilation, useExpandedHierarchy)
                Assert.Null(navInfo)
            End Using
        End Function

    End Class
End Namespace
