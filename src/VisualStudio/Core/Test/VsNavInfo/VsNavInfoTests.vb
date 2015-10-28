Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.VsNavInfo
    Public Class VsNavInfoTests

#Region "C# Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestNamespace()
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace $$N { }
        </Document>
    </Project>
</Workspace>

            Test(workspace,
                 canonicalNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N")
                 },
                 presentationNodes:={
                    Package("CSharpTestAssembly"),
                    [Namespace]("N")
                 })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestClass()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestMethod()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestMethod_Parameters()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestMetadata_Class1()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestMetadata_Class2()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestMetadata_Ctor1()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestMetadata_Ctor2()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub CSharp_TestMetadata_Method()
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

            Test(workspace,
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
        End Sub

#End Region

#Region "Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestNamespace()
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace $$N
            End Namespace
        </Document>
    </Project>
</Workspace>

            Test(workspace,
                 canonicalNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N")
                 },
                 presentationNodes:={
                    Package("VBTestAssembly"),
                    [Namespace]("N")
                 })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestClass()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMember_Sub()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMember_Function()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMember_Parameters()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMetadata_Class1()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMetadata_Class2()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMetadata_Ctor1()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMetadata_Ctor2()
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

            Test(workspace,
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
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
        Public Sub VisualBasic_TestMetadata_Method()
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

            Test(workspace,
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
        End Sub

#End Region

        Private Shared Sub IsOK(comAction As Func(Of Integer))
            Assert.Equal(VSConstants.S_OK, comAction())
        End Sub

        Private Delegate Sub NodeVerifier(vsNavInfoNode As IVsNavInfoNode)

        Private Shared Function Node(expectedListType As _LIB_LISTTYPE, expectedName As String) As NodeVerifier
            Return Sub(vsNavInfoNode)
                       Dim listType As UInteger
                       IsOK(Function() vsNavInfoNode.get_Type(listType))
                       Assert.Equal(CUInt(expectedListType), listType)

                       Dim actualName As String = Nothing
                       IsOK(Function() vsNavInfoNode.get_Name(actualName))
                       Assert.Equal(expectedName, actualName)
                   End Sub
        End Function

        Private Shared Function Package(expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_PACKAGE, expectedName)
        End Function

        Private Shared Function [Namespace](expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_NAMESPACES, expectedName)
        End Function

        Private Shared Function [Class](expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_CLASSES, expectedName)
        End Function

        Private Shared Function Member(expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_MEMBERS, expectedName)
        End Function

        Private Shared Function Hierarchy(expectedName As String) As NodeVerifier
            Return Node(_LIB_LISTTYPE.LLT_HIERARCHY, expectedName)
        End Function

        Private Shared Sub VerifyNodes(enumerator As IVsEnumNavInfoNodes, verifiers() As NodeVerifier)
            Dim index = 0
            Dim actualNode = New IVsNavInfoNode(0) {}
            Dim fetched As UInteger
            While enumerator.Next(1, actualNode, fetched) = VSConstants.S_OK
                Dim verifier = verifiers(index)
                index += 1

                verifier(actualNode(0))
            End While

            Assert.Equal(index, verifiers.Length)
        End Sub

        Private Shared Sub Test(
            workspaceDefinition As XElement,
            Optional useExpandedHierarchy As Boolean = False,
            Optional canonicalNodes As NodeVerifier() = Nothing,
            Optional presentationNodes As NodeVerifier() = Nothing
        )

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
                Dim hostDocument = workspace.DocumentWithCursor
                Assert.True(hostDocument IsNot Nothing, "Test defined without cursor position")

                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim semanticModel = document.GetSemanticModelAsync(CancellationToken.None).Result
                Dim position As Integer = hostDocument.CursorPosition.Value
                Dim symbol = SymbolFinder.FindSymbolAtPosition(semanticModel, position, workspace, CancellationToken.None)
                Assert.True(symbol IsNot Nothing, $"Could not find symbol as position, {position}")

                Dim libraryService = document.Project.LanguageServices.GetService(Of ILibraryService)

                Dim project = document.Project
                Dim compilation = project.GetCompilationAsync(CancellationToken.None).Result
                Dim navInfo = libraryService.NavInfo.CreateForSymbol(symbol, document.Project, compilation, useExpandedHierarchy)
                Assert.True(navInfo IsNot Nothing, $"Could not retrieve nav info for {symbol.ToDisplayString()}")

                If canonicalNodes IsNot Nothing Then
                    Dim enumerator As IVsEnumNavInfoNodes = Nothing
                    IsOK(Function() navInfo.EnumCanonicalNodes(enumerator))

                    VerifyNodes(enumerator, canonicalNodes)
                End If

                If presentationNodes IsNot Nothing Then
                    Dim enumerator As IVsEnumNavInfoNodes = Nothing
                    IsOK(Function() navInfo.EnumPresentationNodes(CUInt(_LIB_LISTFLAGS.LLF_NONE), enumerator))

                    VerifyNodes(enumerator, presentationNodes)
                End If
            End Using
        End Sub
    End Class
End Namespace