' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class SyntaxNodeKeyTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClass()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C$$
        {
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "C", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialClass1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        partial class C$$
        {
        }

        partial class C
        {
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "C", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPartialClass2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        partial class C
        {
        }

        partial class C$$
        {
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "C", 2)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestClassInNamespace()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N1
        {
            class C$$
            {
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "N1.C", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMethod()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N1
        {
            class C
            {
                void $$M()
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "N1.C.M()", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestMethodWithParameters()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N1
        {
            class C
            {
                void $$M(ref int, string)
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "N1.C.M(ref int,string)", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericMethodWithParametersInGenericClass()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace N1
        {
            class C&lt;T, U&gt;
            {
                void $$M&lt;V, W, X&gt;(ref int, string)
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "N1.C`2.M`3(ref int,string)", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestEscapedNames()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        namespace @int
        {
            namespace @class
            {
                class $$@void
                {
                }
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "int.class.void", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConversionOperator1()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public static explicit $$operator string(C c)
            {
                return null;
            }

            public static implicit operator int(C c)
            {
                return 0;
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "C.#op_Explicit_string(C)", 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConversionOperator2()
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document>
        class C
        {
            public static explicit operator string(C c)
            {
                return null;
            }

            public static implicit $$operator int(C c)
            {
                return 0;
            }
        }
        </Document>
    </Project>
</Workspace>

            Test(input, "C.#op_Implicit_int(C)", 1)
        End Sub

        Private Sub Test(definition As XElement, expectedName As String, expectedOrdinal As Integer)
            Using workspace = TestWorkspaceFactory.CreateWorkspace(definition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
                Dim project = workspace.CurrentSolution.Projects.First()
                Dim codeModelService = project.LanguageServices.GetService(Of ICodeModelService)()
                Assert.NotNull(codeModelService)

                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim tree = document.GetSyntaxTreeAsync().Result
                Dim node = tree.GetRoot().FindToken(cursorPosition).Parent
                Dim nodeKey = codeModelService.GetNodeKey(node)

                Assert.Equal(expectedName, nodeKey.Name)
                Assert.Equal(expectedOrdinal, nodeKey.Ordinal)
            End Using
        End Sub

    End Class
End Namespace
