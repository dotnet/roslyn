' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics.AddMissingReference
    Public Class AddMissingReferenceTests
        Inherits AbstractCrossLanguageUserDiagnosticTest

        Private Shared ReadOnly s_presentationCoreAssembly As Assembly
        Private Shared ReadOnly s_presentationFrameworkAssembly As Assembly
        Private Shared ReadOnly s_windowsBaseAssembly As Assembly
        Private Shared ReadOnly s_systemXamlAssembly As Assembly

        Shared Sub New()
            s_presentationCoreAssembly = Assembly.Load("PresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
            s_presentationFrameworkAssembly = Assembly.Load("PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
            s_windowsBaseAssembly = Assembly.Load("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")
            s_systemXamlAssembly = Assembly.Load("System.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace, language As String) As Tuple(Of DiagnosticAnalyzer, CodeFixProvider)
            Dim fixer As CodeFixProvider =
                CType(If(language = LanguageNames.CSharp,
                   DirectCast(New Microsoft.CodeAnalysis.CSharp.CodeFixes.AddMissingReference.AddMissingReferenceCodeFixProvider(), CodeFixProvider),
                   DirectCast(New Microsoft.CodeAnalysis.VisualBasic.CodeFixes.AddMissingReference.AddMissingReferenceCodeFixProvider(), CodeFixProvider)), CodeFixProvider)

            Return Tuple.Create(Of DiagnosticAnalyzer, CodeFixProvider)(Nothing, fixer)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddProjectReferenceBetweenCSharpProjects()
            TestAddProjectReference(<Workspace>
                                        <Project Language="C#" AssemblyName="ProjectA" CommonReferences="true">
                                            <Document>public class A { }</Document>
                                        </Project>
                                        <Project Language="C#" AssemblyName="ProjectB" CommonReferences="true">
                                            <ProjectReference>ProjectA</ProjectReference>
                                            <Document>public class B : A { }</Document>
                                        </Project>
                                        <Project Language="C#" AssemblyName="ProjectC" CommonReferences="true">
                                            <ProjectReference>ProjectB</ProjectReference>
                                            <Document>public class C : B$$ { }</Document>
                                        </Project>
                                    </Workspace>,
                                    "ProjectC", "ProjectA")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddProjectReferenceBetweenVisualBasicProjects()
            TestAddProjectReference(<Workspace>
                                        <Project Language="Visual Basic" AssemblyName="ProjectA" CommonReferences="true">
                                            <Document>Public Class A : End Class</Document>
                                        </Project>
                                        <Project Language="Visual Basic" AssemblyName="ProjectB" CommonReferences="true">
                                            <ProjectReference>ProjectA</ProjectReference>
                                            <Document>Public Class B : Inherits A : End Class</Document>
                                        </Project>
                                        <Project Language="Visual Basic" AssemblyName="ProjectC" CommonReferences="true">
                                            <ProjectReference>ProjectB</ProjectReference>
                                            <Document>Public Class C : Inherits $$B : End Class</Document>
                                        </Project>
                                    </Workspace>,
                                    "ProjectC", "ProjectA")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddProjectReferenceBetweenMixedLanguages1()
            TestAddProjectReference(<Workspace>
                                        <Project Language="C#" AssemblyName="ProjectA" CommonReferences="true">
                                            <Document>public class A { }</Document>
                                        </Project>
                                        <Project Language="Visual Basic" AssemblyName="ProjectB" CommonReferences="true">
                                            <ProjectReference>ProjectA</ProjectReference>
                                            <Document>Public Class B : Inherits A : End Class</Document>
                                        </Project>
                                        <Project Language="Visual Basic" AssemblyName="ProjectC" CommonReferences="true">
                                            <ProjectReference>ProjectB</ProjectReference>
                                            <Document>Public Class C : Inherits $$B : End Class</Document>
                                        </Project>
                                    </Workspace>,
                                    "ProjectC", "ProjectA")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddProjectReferenceBetweenMixedLanguages2()
            TestAddProjectReference(<Workspace>
                                        <Project Language="Visual Basic" AssemblyName="ProjectA" CommonReferences="true">
                                            <Document>Public Class A : End Class</Document>
                                        </Project>
                                        <Project Language="C#" AssemblyName="ProjectB" CommonReferences="true">
                                            <ProjectReference>ProjectA</ProjectReference>
                                            <Document>public class B : A { }</Document>
                                        </Project>
                                        <Project Language="C#" AssemblyName="ProjectC" CommonReferences="true">
                                            <ProjectReference>ProjectB</ProjectReference>
                                            <Document>public class C : B$$ { }</Document>
                                        </Project>
                                    </Workspace>,
                                    "ProjectC", "ProjectA")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddMetadataReferenceToVisualBasicProjectErrorCode30005()
            TestAddUnresolvedMetadataReference(
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VBProject" CommonReferences="true">
                        <ProjectReference>VBProject2</ProjectReference>
                        <Document>
                            Public Class Foo
                                Sub M()
                                    AddHandler ModuleWithEvent.E$$, Sub() Exit Sub
                                End Sub
                            End Class
                        </Document>
                    </Project>
                    <Project Language="Visual Basic" AssemblyName="VBProject2" CommonReferences="true">
                        <MetadataReference><%= s_windowsBaseAssembly.Location %></MetadataReference>
                        <Document>
                            Public Module ModuleWithEvent
                                Public Event E As System.Windows.PropertyChangedCallback
                            End Module
                        </Document>
                    </Project>
                </Workspace>,
                "VBProject", s_windowsBaseAssembly.FullName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddMetadataReferenceToVisualBasicProjectErrorCode30007()
            TestAddUnresolvedMetadataReference(
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VBProject" CommonReferences="true">
                        <MetadataReference><%= s_presentationCoreAssembly.Location %></MetadataReference>
                        <Document>Public Class Foo : Inherits System.Windows.UIElement$$ : End Class</Document>
                    </Project>
                </Workspace>,
                "VBProject", s_windowsBaseAssembly.FullName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddMetadataReferenceToVisualBasicProjectErrorCode30652()
            TestAddUnresolvedMetadataReference(
                <Workspace>
                    <Project Language="Visual Basic" AssemblyName="VBProject" CommonReferences="true">
                        <ProjectReference>VBProject2</ProjectReference>
                        <Document>
                            Public Class Foo
                                Sub M()
                                    ModuleWithEvent.E$$(Nothing)
                                End Sub
                            End Class
                        </Document>
                    </Project>
                    <Project Language="Visual Basic" AssemblyName="VBProject2" CommonReferences="true">
                        <MetadataReference><%= s_windowsBaseAssembly.Location %></MetadataReference>
                        <Document>
                            Public Module ModuleWithEvent
                                Public Sub E(x As System.Windows.DependencyProperty) : End Sub
                            End Module
                        </Document>
                    </Project>
                </Workspace>,
                "VBProject", s_windowsBaseAssembly.FullName)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsAddMissingReference)>
        Public Sub AddMetadataReferenceToCSharpProject()
            TestAddUnresolvedMetadataReference(
                <Workspace>
                    <Project Language="C#" AssemblyName="CSharpProject" CommonReferences="true">
                        <MetadataReference><%= s_presentationCoreAssembly.Location %></MetadataReference>
                        <Document>public class Foo : System.Windows.UIElement$$ { }</Document>
                    </Project>
                </Workspace>,
                "CSharpProject", s_windowsBaseAssembly.FullName)
        End Sub
    End Class
End Namespace
