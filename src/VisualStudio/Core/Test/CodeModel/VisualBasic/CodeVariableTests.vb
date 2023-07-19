' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeVariableTests
        Inherits AbstractCodeVariableTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
            Dim code =
<Code>
Class C
    Dim i$$ As Integer
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=9, absoluteOffset:=17, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=20)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim i$$ As Integer
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=9, absoluteOffset:=49, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_EnumMember()
            Dim code =
<Code>
Enum E
    A$$
End Enum
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=5)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_EnumMember_Attribute()
            Dim code =
<Code>
Enum E
    &lt;System.CLSCompliant(True)&gt;
    A$$
End Enum
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=44, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=12, lineLength:=31)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
            Dim code =
<Code>
Class C
    Dim i$$ As Integer
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=10, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=20)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Dim i$$ As Integer
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=10, absoluteOffset:=50, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=20)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_EnumMember()
            Dim code =
<Code>
Enum E
    A$$
End Enum
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=6, absoluteOffset:=13, lineLength:=5)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_EnumMember_Attribute()
            Dim code =
<Code>
Enum E
    &lt;System.CLSCompliant(True)&gt;
    A$$
End Enum
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=39, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=39, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=6, absoluteOffset:=45, lineLength:=5)))
        End Sub

#End Region

#Region "Access tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess1()
            Dim code =
    <Code>
Class C
    Dim $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess2()
            Dim code =
    <Code>
Class C
    Private $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess3()
            Dim code =
    <Code>
Class C
    Protected $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess4()
            Dim code =
    <Code>
Class C
    Protected Friend $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess5()
            Dim code =
    <Code>
Class C
    Friend $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess6()
            Dim code =
    <Code>
Class C
    Public $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAccess7()
            Dim code =
    <Code>
Enum E
    $$Goo
End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Comment tests"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638909")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestComment1()
            Dim code =
<Code>
Class C
    ' Goo
    Dim $$i As Integer
End Class
</Code>

            Dim result = " Goo"

            TestComment(code, result)
        End Sub

#End Region

#Region "ConstKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind1()
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind2()
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind3()
            Dim code =
<Code>
Class C
    Const $$x As Integer
End Class
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind4()
            Dim code =
<Code>
Class C
    ReadOnly $$x As Integer
End Class
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestConstKind5()
            Dim code =
<Code>
Class C
    ReadOnly Const $$x As Integer
End Class
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

#End Region

#Region "InitExpression tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInitExpression1()
            Dim code =
<Code>
Class C
    Dim i$$ As Integer = 42
End Class
</Code>

            TestInitExpression(code, "42")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInitExpression2()
            Dim code =
<Code>
Class C
    Const $$i As Integer = 19 + 23
End Class
</Code>

            TestInitExpression(code, "19 + 23")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestInitExpression3()
            Dim code =
<Code>
Enum E
    $$i = 19 + 23
End Enum
</Code>

            TestInitExpression(code, "19 + 23")
        End Sub

#End Region

#Region "IsConstant tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant1()
            Dim code =
    <Code>
Enum E
    $$Goo
End Enum
</Code>
            TestIsConstant(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant2()
            Dim code =
    <Code>
Class C
    Dim $$x As Integer
End Class
</Code>
            TestIsConstant(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant3()
            Dim code =
    <Code>
Class C
    Const $$x As Integer = 0
End Class
</Code>
            TestIsConstant(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant4()
            Dim code =
    <Code>
Class C
    ReadOnly $$x As Integer = 0
End Class
</Code>
            TestIsConstant(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsConstant5()
            Dim code =
    <Code>
Class C
    WithEvents $$x As Integer
End Class
</Code>
            TestIsConstant(code, False)
        End Sub

#End Region

#Region "IsShared tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared1()
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            TestIsShared(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsShared2()
            Dim code =
<Code>
Class C
    Shared $$x As Integer
End Class
</Code>

            TestIsShared(code, True)
        End Sub

#End Region

#Region "Name tests"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638224")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName_EnumMember()
            Dim code =
<Code>
Enum SomeEnum
    A$$
End Enum
</Code>

            TestName(code, "A")
        End Sub

#End Region

#Region "Prototype tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_UniqueSignature()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "F:N.C.x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName1()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "Private C.x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName2()
            Dim code =
<Code>
Namespace N
    Class C(Of T)
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "Private C(Of T).x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName3()
            Dim code =
<Code>
Namespace N
    Class C
        Public ReadOnly $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "Public C.x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName_InitExpression()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "Private C.x = 42")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName1()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "Private N.C.x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName2()
            Dim code =
<Code>
Namespace N
    Class C(Of T)
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "Private N.C(Of T).x")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName_InitExpression()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "Private N.C.x = 42")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_NoName()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeNoName, "Private ")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_NoName_InitExpression()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeNoName Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression, "Private  = 42")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_NoName_InitExpression_Type()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeNoName Or EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "Private  As Integer = 42")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_InitExpression_Type_ForAsNew()
            ' Amusingly, this will *crash* Dev10.

            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As New System.Text.StringBuilder
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeInitExpression Or EnvDTE.vsCMPrototype.vsCMPrototypeType, "Private x As System.Text.StringBuilder")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Type()
            Dim code =
<Code>
Namespace N
    Class C
        Dim $$x As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "Private x As Integer")
        End Sub

#End Region

#Region "Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
            Dim code =
<Code>
Class C
    Dim $$a As Integer
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Integer",
                             .AsFullName = "System.Int32",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType2()
            Dim code =
<Code>
Class C
    WithEvents $$a As Object
End Class
</Code>

            TestTypeProp(code,
             New CodeTypeRefData With {
                 .AsString = "Object",
                 .AsFullName = "System.Object",
                 .CodeTypeFullName = "System.Object",
                 .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefObject
             })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType3()
            Dim code =
<Code>
Class C
    Private $$a As New Object
End Class
</Code>

            TestTypeProp(code,
             New CodeTypeRefData With {
                 .AsString = "Object",
                 .AsFullName = "System.Object",
                 .CodeTypeFullName = "System.Object",
                 .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefObject
             })
        End Sub

#End Region

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
Imports System

Class C
    Dim $$goo As Integer
End Class
</Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    <Serializable()>
    Dim goo As Integer
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1087167")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code><![CDATA[
Imports System

Class C
    <Serializable>
    Dim $$goo As Integer
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    <Serializable>
    <CLSCompliant(True)>
    Dim goo As Integer
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
            Dim code =
<Code><![CDATA[
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Dim $$goo As Integer
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    <CLSCompliant(True)>
    Dim goo As Integer
End Class
]]></Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Function

#End Region

#Region "Set Access tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess1() As Task
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo
End Enum
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess2() As Task
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo
End Enum
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetEnumAccess3() As Task
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo
End Enum
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess2() As Task
            Dim code =
<Code>
Class C
    Public $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess3() As Task
            Dim code =
<Code>
Class C
    Private $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess4() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess5() As Task
            Dim code =
<Code>
Class C
    Public $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Protected Friend i As Integer
End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess6() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    Dim x As Integer

#End Region

    Dim $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    Dim x As Integer

#End Region

    Public i As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess7() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    Dim x As Integer

#End Region

    Public $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    Dim x As Integer

#End Region

    Protected Friend i As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess8() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    Dim x As Integer

#End Region

    Public $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    Dim x As Integer

#End Region

    Dim i As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess9() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    Dim $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    Public x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess10() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    Dim x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess11() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    Protected Friend x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess12() As Task
            Dim code =
<Code><![CDATA[
Class C

#Region "Goo"

    <Bar>
    Public $$x As Integer

#End Region

End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C

#Region "Goo"

    <Bar>
    Protected Friend x As Integer

#End Region

End Class
]]></Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess13() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    ' Comment comment comment
    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    ' Comment comment comment
    Protected Friend x As Integer

#End Region

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess14() As Task
            Dim code =
<Code><![CDATA[
Class C

#Region "Goo"

    ''' <summary>
    ''' Comment comment comment
    ''' </summary>
    Public $$x As Integer

#End Region

End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C

#Region "Goo"

    ''' <summary>
    ''' Comment comment comment
    ''' </summary>
    Protected Friend x As Integer

#End Region

End Class
]]></Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess15() As Task
            Dim code =
<Code>
Class C

    Dim $$x As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

    Private WithEvents x As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate Or EnvDTE.vsCMAccess.vsCMAccessWithEvents)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetAccess16() As Task
            Dim code =
<Code>
Class C

    Private WithEvents $$x As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

    Dim x As Integer

End Class
</Code>

            Await TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Function

#End Region

#Region "Set ConstKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind1() As Task
            Dim code =
<Code>
Enum
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Goo
End Enum
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind2() As Task
            Dim code =
<Code>
Enum
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Goo
End Enum
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind3() As Task
            Dim code =
<Code>
Enum
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Goo
End Enum
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind4() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind5() As Task
            Dim code =
<Code>
Class C
    Shared $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind6() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind7() As Task
            Dim code =
<Code>
Class C
    Const $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind8() As Task
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    ReadOnly x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetConstKind9() As Task
            Dim code =
<Code>
Class C
    ReadOnly $$x As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim x As Integer
End Class
</Code>

            Await TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Function

#End Region

#Region "Set InitExpression tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer = 42
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression2() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer, j As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer = 42, j As Integer
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression3() As Task
            Dim code =
<Code>
Class C
    Dim i As Integer, $$j As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer, j As Integer = 42
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression4() As Task
            ' The result below is a bit silly, but that's what the legacy Code Model does.

            Dim code =
<Code>
Class C
    Dim $$o As New Object
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim o As New Object = 42
End Class
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression5() As Task
            Dim code =
<Code>
Class C
    Const $$i As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer = 19 + 23
End Class
</Code>

            Await TestSetInitExpression(code, expected, "19 + 23")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression6() As Task
            Dim code =
<Code>
Class C
    Const $$i As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer
End Class
</Code>

            Await TestSetInitExpression(code, expected, "")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression7() As Task
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo = 42
End Enum
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression8() As Task
            Dim code =
<Code>
Enum E
    $$Goo = 0
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo = 42
End Enum
</Code>

            Await TestSetInitExpression(code, expected, "42")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetInitExpression9() As Task
            Dim code =
<Code>
Enum E
    $$Goo = 0
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo
End Enum
</Code>

            Await TestSetInitExpression(code, expected, Nothing)
        End Function

#End Region

#Region "Set IsConstant tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant2() As Task
            Dim code =
<Code>
Class C
    Const $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant3() As Task
            Dim code =
<Code>
Class C
    ReadOnly $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Const i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant4() As Task
            Dim code =
<Code>
Class C
    ReadOnly $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetIsConstant(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant5() As Task
            Dim code =
<Code>
Module C
    Dim $$i As Integer
End Module
</Code>

            Dim expected =
<Code>
Module C
    Const i As Integer
End Module
</Code>

            Await TestSetIsConstant(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant6() As Task
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo
End Enum
</Code>

            Await TestSetIsConstant(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsConstant7() As Task
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo
End Enum
</Code>

            Await TestSetIsConstant(code, expected, False, ThrowsNotImplementedException(Of Boolean))
        End Function

#End Region

#Region "Set IsShared tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared1() As Task
            Dim code =
<Code>
Class C
    Dim $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Shared i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared2() As Task
            Dim code =
<Code>
Class C
    Shared $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared3() As Task
            Dim code =
<Code>
Class C
    Private $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private Shared i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared4() As Task
            Dim code =
<Code>
Class C
    Private Shared $$i As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private i As Integer
End Class
</Code>

            Await TestSetIsShared(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared5() As Task
            Dim code =
<Code>
Module C
    Dim $$i As Integer
End Module
</Code>

            Dim expected =
<Code>
Module C
    Dim i As Integer
End Module
</Code>

            Await TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsShared6() As Task
            Dim code =
<Code>
Enum E
    $$Goo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Goo
End Enum
</Code>

            Await TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Function

#End Region

#Region "Set Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName1() As Task
            Dim code =
<Code>
Class C
    Dim $$Goo As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim Bar As Integer
End Class
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName2() As Task
            Dim code =
<Code>
Class C

#Region "Goo"

    Dim $$Goo As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Goo"

    Dim Bar As Integer

#End Region

End Class
</Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

#End Region

#Region "Set Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Class C
    Dim $$a As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim a As Double
End Class
</Code>

            Await TestSetTypeProp(code, expected, "double")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Class C
    Dim $$a, b As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim a, b As Double
End Class
</Code>

            Await TestSetTypeProp(code, expected, "double")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Class C
    Private $$a As New Object
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private a As New String
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Private $$a As New Object, x As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private a As New String, x As Integer = 0
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType5() As Task
            Dim code =
<Code>
Class C
    Private a As New Object, x$$ As Integer = 0
End Class
</Code>

            Dim expected =
<Code>
Class C
    Private a As New Object, x As String = 0
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
