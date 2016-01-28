' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Strict Off

Imports Microsoft.CodeAnalysis.Compilers
Imports Microsoft.CodeAnalysis.Services.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Services.Simplification
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Services.Editor.UnitTests.CodeActions.SimplifyTypeNames
    Partial Public Class NameSimplificationServiceTests
        Inherits AbstractNameSimplificationServiceTests

#Region "Normal CSharp Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyAllNodes_UseAlias()
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using MyType = System.Exception;
                    class A 
                    {
                        System.Exception c;
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
                  using MyType = System.Exception;
                  class A 
                  {
                      MyType c;
                  }
                </text>.Value

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyAllNodes_TwoAliases()
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using MyType1 = System.Exception;
                    namespace Root 
                    {
                        using MyType2 = System.Exception;

                        class A 
                        {
                            System.Exception c;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  using MyType1 = System.Exception;
                  namespace Root 
                  {
                      using MyType2 = MyType1;

                      class A 
                      {
                          MyType2 c;
                      }
                  }
                </text>.Value

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestSimplifyAllNodes_SimplifyTypeName()
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            System.Exception c;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  using System;
                  namespace Root 
                  {
                      class A 
                      {
                          Exception c;
                      }
                  }
                </text>.Value

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub TestGetChanges_SimplifyTypeName()
            Dim input =
        <Workspace>
            <Project Language="C#" CommonReferences="true">
                <Document>
                    using System;
                    namespace Root 
                    {
                        class A 
                        {
                            System.Exception c;
                        }
                    }
                </Document>
            </Project>
        </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(input)
                Dim document = GetDocument(workspace)
                Dim simplified = SimplificationService.Simplify(document)
                Dim decl = simplified.GetSyntaxRoot().DescendantNodes().Where(Function(n) n.ToString() = "c").First().Parent
                Dim type = decl.ChildNodesAndTokens(0)

                Assert.Equal("Exception", type.ToString())
            End Using
        End Sub

#End Region

#Region "Normal Visual Basic Tests"

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub VisualBasic_TestSimplifyAllNodes_UseAlias()
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports Ex = System.Exception
                    Class A
                        Public e As System.Exception
                    End Class
                </Document>
            </Project>
        </Workspace>

            Dim expected =
                <text>
                  Imports Ex = System.Exception
                  Class A
                      Public e As Ex
                  End Class
                </text>.Value

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub VisualBasic_TestSimplifyAllNodes_SimplifyTypeName()
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports System
                      Namespace Root
                        Class A
                            Private e As System.Exception
                        End Class
                    End Namespace
                </Document>
            </Project>
        </Workspace>

            Dim expected =
              <text>
                  Imports System
                  Namespace Root
                      Class A
                          Private e As Exception
                      End Class
                  End Namespace
                </text>.Value

            Test(input, expected)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub VisualBasic_TestGetChanges_SimplifyTypeName()
            Dim input =
        <Workspace>
            <Project Language="Visual Basic" CommonReferences="true">
                <Document>
                    Imports Ex = System.Exception
                    Namespace Root
                        Class A
                            Private e As System.Exception
                        End Class
                    End Namespace
                </Document>
            </Project>
        </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(input)
                Dim document = GetDocument(workspace)
                Dim changes = SimplificationService.Simplify(document).GetTextChanges(document)

                Assert.Equal("Ex", changes.FirstOrDefault().NewText)
                Assert.Equal(TextSpan.FromBounds(163, 179), changes.FirstOrDefault().Span)
            End Using
        End Sub

        <WorkItem(547117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547117")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub VisualBasic_TestGetChanges_SimplifyTypeName_Array_1()
            Dim input =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                    Module Program
                    Dim Foo() As Integer

                    Sub Main(args As String())
                        Program.Foo(23) = 23
                    End Sub
                    End Module
                </Document>
                        </Project>
                    </Workspace>

            Dim expected =
              <text>
                    Module Program
                    Dim Foo() As Integer

                    Sub Main(args As String())
                        Foo(23) = 23
                    End Sub
                    End Module
                </text>.Value

            Test(input, expected)
        End Sub

        <WorkItem(547117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/547117")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyTypeNames)>
        Public Sub VisualBasic_TestGetChanges_SimplifyTypeName_Array_2()
            Dim input =
                    <Workspace>
                        <Project Language="Visual Basic" CommonReferences="true">
                            <Document>
                    Module Program
                    Dim Bar() As Action(Of Integer)

                    Sub Main(args As String())
                        Program.Bar(2)(2)
                    End Sub
                    End Module
                </Document>
                        </Project>
                    </Workspace>

            Dim expected =
              <text>
                    Module Program
                    Dim Bar() As Action(Of Integer)

                    Sub Main(args As String())
                        Bar(2)(2)
                    End Sub
                    End Module
                </text>.Value

            Test(input, expected)
        End Sub

#End Region
    End Class
End Namespace
