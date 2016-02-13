' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Commands
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.Utilities.VsNavInfo
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ClassView
    Public Class SyncClassViewTests

#Region "C# Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestClass1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    $$
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestClass2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C$$
                {
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestClass3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class $$C
                {
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestClass4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                $$class C
                {
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestClass5() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {  $$
                    void M()
                    {
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestClassInNestedNamespaces1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace X.Y
            {
                class C
                {  $$
                    void M()
                    {
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("X.Y"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestClassInNestedNamespaces2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace X
            {
                namespace Y
                {
                    class C
                    {  $$
                        void M()
                        {
                        }
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("X.Y"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestMethod1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    void M()
                    {$$
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestMethod2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    void M()
                    $${
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestMethod3() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
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

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestMethod4() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    $$void M()
                    {
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestMethod5() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
            $$        void M()
                    {
                    }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestField1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
            $$        int i;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("i"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestField2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    int $$i;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("i"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestProperty1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
            $$        int P { get; }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("P"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestProperty2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    int $$P { get; }
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("P"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestEvent1() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
            $$        event System.EventHandler E;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("E"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function CSharp_TestEvent2() As Task
            Dim workspace =
<Workspace>
    <Project Language="C#" CommonReferences="true" AssemblyName="CSharpTestAssembly">
        <Document>
            namespace N
            {
                class C
                {
                    event System.EventHandler $$E;
                }
            }
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("CSharpTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("E"))
        End Function

#End Region

#Region "Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestClass1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    $$
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestClass2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C$$
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestClass3() As Task
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
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestClass4() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                $$Class C
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestClass5() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C  $$
                    Sub M()
                    End Sub
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestClassInNestedNamespaces1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace X.Y
                Class C$$
                    Sub M()
                    End Sub
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("X.Y"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestClassInNestedNamespaces2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace X
                Namespace Y
                    Class C$$
                        Sub M()
                        End Sub
                    End Class
                End Namespace
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("X.Y"),
                [Class]("C"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestMethod1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    Sub M()
                        $$
                    End Sub
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestMethod2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    Sub M()$$
                    End Sub
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestMethod3() As Task
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
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestMethod4() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    $$Sub M()
                    End Sub
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestMethod5() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
            $$        Sub M()
                    End Sub
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("M()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestField1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
            $$        Private i As Integer
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("i As Integer"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestField2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    Private $$i As Integer
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("i As Integer"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestProperty1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
            $$        ReadOnly Property P As Integer = 42
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("P As Integer"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestProperty2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    ReadOnly Property $$P As Integer = 42
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("P As Integer"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestEvent1() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
            $$        Event E()
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("E()"))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ClassView)>
        Public Async Function VisualBasic_TestEvent2() As Task
            Dim workspace =
<Workspace>
    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBTestAssembly">
        <Document>
            Namespace N
                Class C
                    Event $$E()
                End Class
            End Namespace
        </Document>
    </Project>
</Workspace>

            Await TestAsync(workspace,
                Package("VBTestAssembly"),
                [Namespace]("N"),
                [Class]("C"),
                Member("E()"))
        End Function

#End Region

        Private Async Function TestAsync(
            workspaceDefinition As XElement,
            ParamArray presentationNodes As NodeVerifier()
        ) As Task

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
                Dim hostDocument = workspace.DocumentWithCursor
                Assert.True(hostDocument IsNot Nothing, "Test defined without cursor position")

                Dim textView = hostDocument.GetTextView()
                Dim subjectBuffer = hostDocument.GetTextBuffer()

                Dim navigationTool = New MockNavigationTool(canonicalNodes:=Nothing, presentationNodes:=presentationNodes)
                Dim serviceProvider = New MockServiceProvider(navigationTool)
                Dim commandHandler = New MockSyncClassViewCommandHandler(serviceProvider, workspace.GetService(Of IWaitIndicator))

                commandHandler.ExecuteCommand(
                    args:=New SyncClassViewCommandArgs(textView, subjectBuffer),
                    nextHandler:=Sub() Exit Sub)

                navigationTool.VerifyNavInfo()
            End Using

        End Function

    End Class
End Namespace
