' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ExternalCodeClassTests
        Inherits AbstractCodeClassTests

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ExpectedClassMembers()
            Dim code =
<Code>
Class C$$
    ' fields
    Private _privateX As Integer
    Protected ProtectedX As Integer
    Friend InternalX As Integer
    Protected Friend ProtectedInternalX As Integer
    Public PublicX As Integer

    ' methods
    Private Sub PrivateM()
    End Sub
    Protected Sub ProtectedM()
    End Sub
    Friend Sub InternalM()
    End Sub
    Protected Friend Sub ProtectedInternalM()
    End Sub
    Public Sub PublicM()
    End Sub
End Class
</Code>

            TestElement(code,
                Sub(codeElement)
                    Dim members = codeElement.Members
                    Assert.Equal(9, members.Count)

                    Dim member1 = members.Item(1)
                    Assert.Equal("New", member1.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member1.Kind)

                    Dim member2 = members.Item(2)
                    Assert.Equal("ProtectedX", member2.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member2.Kind)

                    Dim member3 = members.Item(3)
                    Assert.Equal("InternalX", member3.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member3.Kind)

                    Dim member4 = members.Item(4)
                    Assert.Equal("ProtectedInternalX", member4.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member4.Kind)

                    Dim member5 = members.Item(5)
                    Assert.Equal("PublicX", member5.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member5.Kind)

                    Dim member6 = members.Item(6)
                    Assert.Equal("ProtectedM", member6.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member6.Kind)

                    Dim member7 = members.Item(7)
                    Assert.Equal("InternalM", member7.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member7.Kind)

                    Dim member8 = members.Item(8)
                    Assert.Equal("ProtectedInternalM", member8.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member8.Kind)

                    Dim member9 = members.Item(9)
                    Assert.Equal("PublicM", member9.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member9.Kind)
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassMembersForWithEventsField_Private()
            Dim code =
<Code>
Class C
    Event E(x As Integer)
End Class

Class D$$
    Inherits C

    Private WithEvents x As C

    Private Sub D_E(x As Integer) Handles Me.E
    End Sub
End Class
</Code>

            TestElement(code,
                Sub(codeElement)
                    Dim members = codeElement.Members
                    Assert.Equal(2, members.Count)

                    Dim member1 = members.Item(1)
                    Assert.Equal("New", member1.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member1.Kind)
                    Assert.Equal(EnvDTE.vsCMAccess.vsCMAccessPublic, CType(member1, EnvDTE.CodeFunction).Access)

                    Dim member2 = members.Item(2)
                    Assert.Equal("_x", member2.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member2.Kind)
                    Assert.Equal(EnvDTE.vsCMAccess.vsCMAccessPrivate, CType(member2, EnvDTE.CodeVariable).Access)
                End Sub)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassMembersForWithEventsField_Protected()
            Dim code =
<Code>
Class C
    Event E(x As Integer)
End Class

Class D$$
    Inherits C

    Protected WithEvents x As C

    Private Sub D_E(x As Integer) Handles Me.E
    End Sub
End Class
</Code>

            TestElement(code,
                Sub(codeElement)
                    Dim members = codeElement.Members
                    Assert.Equal(3, members.Count)

                    Dim member1 = members.Item(1)
                    Assert.Equal("New", member1.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member1.Kind)
                    Assert.Equal(EnvDTE.vsCMAccess.vsCMAccessPublic, CType(member1, EnvDTE.CodeFunction).Access)

                    Dim member2 = members.Item(2)
                    Assert.Equal("_x", member2.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member2.Kind)
                    Assert.Equal(EnvDTE.vsCMAccess.vsCMAccessPrivate, CType(member2, EnvDTE.CodeVariable).Access)

                    Dim member3 = members.Item(3)
                    Assert.Equal("x", member3.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member3.Kind)
                    Assert.Equal(EnvDTE.vsCMAccess.vsCMAccessProtected Or EnvDTE.vsCMAccess.vsCMAccessWithEvents, CType(member3, EnvDTE.CodeVariable).Access)
                End Sub)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace