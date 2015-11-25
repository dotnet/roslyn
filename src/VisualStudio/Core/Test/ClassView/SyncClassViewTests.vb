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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

#End Region

#Region "Visual Basic Tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.VsNavInfo)>
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

#End Region

        Private Async Function TestAsync(
            workspaceDefinition As XElement,
            Optional canonicalNodes As NodeVerifier() = Nothing,
            Optional presentationNodes As NodeVerifier() = Nothing
        ) As Task

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceDefinition, exportProvider:=VisualStudioTestExportProvider.ExportProvider)
                Dim hostDocument = workspace.DocumentWithCursor
                Assert.True(hostDocument IsNot Nothing, "Test defined without cursor position")

                Dim textView = hostDocument.GetTextView()
                Dim subjectBuffer = hostDocument.GetTextBuffer()

                Dim navigationTool = New MockNavigationTool(canonicalNodes, presentationNodes)
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
