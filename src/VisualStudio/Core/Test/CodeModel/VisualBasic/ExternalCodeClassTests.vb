' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class ExternalCodeClassTests
        Inherits AbstractCodeClassTests

        <ConditionalFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ClassMembersForWithEventsField()
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
                        Assert.Equal("x", member1.Name)
                        Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member1.Kind)

                        Dim member2 = members.Item(2)
                        Assert.Equal("D_E", member2.Name)
                        Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member2.Kind)
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