﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.CSharp
    Public Class ExternalCodeClassTests
        Inherits AbstractCodeClassTests

#Region "Doc Comment"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment1()
            Dim code =
<Code>
/// &lt;summary&gt;This is my comment!&lt;/summary&gt;
class C$$
{
}
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "  <summary>This is my comment!</summary>" & vbCrLf & "</doc>")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestDocComment2()
            Dim code =
<Code>
/// &lt;summary&gt;This is my comment!&lt;/summary&gt;
/// &lt;remarks /&gt;
class C$$
{
}
</Code>

            TestDocComment(code, "<doc>" & vbCrLf & "  <summary>This is my comment!</summary>" & vbCrLf & "  <remarks />" & vbCrLf & "</doc>")
        End Sub

#End Region

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestExpectedClassMembers()
            Dim code =
<Code>
class C$$
{
    // fields
    private int _privateX;
    protected int ProtectedX;
    internal int InternalX;
    protected internal int ProtectedInternalX;
    public int PublicX;

    // methods
    private void PrivateM() { }
    protected void ProtectedM() { }
    internal void InternalM() { }
    protected internal void ProtectedInternalM() { }
    public void PublicM() { }
}
</Code>

            TestElement(code,
                Sub(codeElement)
                    Dim members = codeElement.Members
                    Assert.Equal(7, members.Count)

                    Dim member1 = members.Item(1)
                    Assert.Equal("ProtectedX", member1.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member1.Kind)

                    Dim member2 = members.Item(2)
                    Assert.Equal("ProtectedInternalX", member2.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member2.Kind)

                    Dim member3 = members.Item(3)
                    Assert.Equal("PublicX", member3.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementVariable, member3.Kind)

                    Dim member4 = members.Item(4)
                    Assert.Equal("ProtectedM", member4.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member4.Kind)

                    Dim member5 = members.Item(5)
                    Assert.Equal("ProtectedInternalM", member5.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member5.Kind)

                    Dim member6 = members.Item(6)
                    Assert.Equal("PublicM", member6.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member6.Kind)

                    Dim member7 = members.Item(7)
                    Assert.Equal("C", member7.Name)
                    Assert.Equal(EnvDTE.vsCMElement.vsCMElementFunction, member7.Kind)
                End Sub)
        End Sub

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.CSharp
            End Get
        End Property

        Protected Overrides ReadOnly Property TargetExternalCodeElements As Boolean
            Get
                Return True
            End Get
        End Property
    End Class
End Namespace
