' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeVariableTests
        Inherits AbstractCodeVariableTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_Attribute()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_EnumMember()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint_EnumMember_Attribute()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_Attribute()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_EnumMember()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint_EnumMember_Attribute()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access1()
            Dim code =
    <Code>
Class C
    Dim $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access2()
            Dim code =
    <Code>
Class C
    Private $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPrivate)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access3()
            Dim code =
    <Code>
Class C
    Protected $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access4()
            Dim code =
    <Code>
Class C
    Protected Friend $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access5()
            Dim code =
    <Code>
Class C
    Friend $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessProject)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access6()
            Dim code =
    <Code>
Class C
    Public $$x as Integer
End Class
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Access7()
            Dim code =
    <Code>
Enum E
    $$Foo
End Enum
</Code>

            TestAccess(code, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

#End Region

#Region "Comment tests"

        <WorkItem(638909)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Comment1()
            Dim code =
<Code>
Class C
    ' Foo
    Dim $$i As Integer
End Class
</Code>

            Dim result = " Foo"

            TestComment(code, result)
        End Sub

#End Region

#Region "ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind1()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind2()
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind3()
            Dim code =
<Code>
Class C
    Const $$x As Integer
End Class
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind4()
            Dim code =
<Code>
Class C
    ReadOnly $$x As Integer
End Class
</Code>

            TestConstKind(code, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub ConstKind5()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InitExpression1()
            Dim code =
<Code>
Class C
    Dim i$$ As Integer = 42
End Class
</Code>

            TestInitExpression(code, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InitExpression2()
            Dim code =
<Code>
Class C
    Const $$i As Integer = 19 + 23
End Class
</Code>

            TestInitExpression(code, "19 + 23")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub InitExpression3()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant1()
            Dim code =
    <Code>
Enum E
    $$Foo
End Enum
</Code>
            TestIsConstant(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant2()
            Dim code =
    <Code>
Class C
    Dim $$x As Integer
End Class
</Code>
            TestIsConstant(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant3()
            Dim code =
    <Code>
Class C
    Const $$x As Integer = 0
End Class
</Code>
            TestIsConstant(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant4()
            Dim code =
    <Code>
Class C
    ReadOnly $$x As Integer = 0
End Class
</Code>
            TestIsConstant(code, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsConstant5()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared1()
            Dim code =
<Code>
Class C
    Dim $$x As Integer
End Class
</Code>

            TestIsShared(code, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub IsShared2()
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

        <WorkItem(638224)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_UniqueSignature()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName1()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName2()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName3()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_ClassName_InitExpression()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_FullName1()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_FullName2()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_FullName_InitExpression()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_NoName()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_NoName_InitExpression()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_NoName_InitExpression_Type()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_InitExpression_Type_ForAsNew()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Prototype_Type()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type1()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type2()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Type3()
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

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute1()
            Dim code =
<Code>
Imports System

Class C
    Dim $$foo As Integer
End Class
</Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    <Serializable()>
    Dim foo As Integer
End Class
]]></Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Sub

        <WorkItem(1087167)>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute2()
            Dim code =
<Code><![CDATA[
Imports System

Class C
    <Serializable>
    Dim $$foo As Integer
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    <Serializable>
    <CLSCompliant(True)>
    Dim foo As Integer
End Class
]]></Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True", .Position = 1})
        End Sub

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttribute_BelowDocComment()
            Dim code =
<Code><![CDATA[
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Dim $$foo As Integer
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    <CLSCompliant(True)>
    Dim foo As Integer
End Class
]]></Code>
            TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "True"})
        End Sub

#End Region

#Region "Set Access tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetEnumAccess1()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetEnumAccess2()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetEnumAccess3()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate, ThrowsArgumentException(Of EnvDTE.vsCMAccess)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess1()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess2()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess3()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess4()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess5()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess6()
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Dim $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Public i As Integer

End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess7()
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Public $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Protected Friend i As Integer

End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess8()
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Public $$i As Integer

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

    Dim i As Integer

End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess9()
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Public x As Integer

#End Region

End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPublic)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess10()
            Dim code =
<Code>
Class C

#Region "Foo"

    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim x As Integer

#End Region

End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess11()
            Dim code =
<Code>
Class C

#Region "Foo"

    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Protected Friend x As Integer

#End Region

End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess12()
            Dim code =
<Code><![CDATA[
Class C

#Region "Foo"

    <Bar>
    Public $$x As Integer

#End Region

End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C

#Region "Foo"

    <Bar>
    Protected Friend x As Integer

#End Region

End Class
]]></Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess13()
            Dim code =
<Code>
Class C

#Region "Foo"

    ' Comment comment comment
    Public $$x As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    ' Comment comment comment
    Protected Friend x As Integer

#End Region

End Class
</Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess14()
            Dim code =
<Code><![CDATA[
Class C

#Region "Foo"

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

#Region "Foo"

    ''' <summary>
    ''' Comment comment comment
    ''' </summary>
    Protected Friend x As Integer

#End Region

End Class
]]></Code>

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessProjectOrProtected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess15()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessPrivate Or EnvDTE.vsCMAccess.vsCMAccessWithEvents)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetAccess16()
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

            TestSetAccess(code, expected, EnvDTE.vsCMAccess.vsCMAccessDefault)
        End Sub

#End Region

#Region "Set ConstKind tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind1()
            Dim code =
<Code>
Enum
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Foo
End Enum
</Code>

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind2()
            Dim code =
<Code>
Enum
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Foo
End Enum
</Code>

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind3()
            Dim code =
<Code>
Enum
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum
    Foo
End Enum
</Code>

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst, ThrowsNotImplementedException(Of EnvDTE80.vsCMConstKind))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind4()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind5()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind6()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindConst)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind7()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind8()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindReadOnly)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetConstKind9()
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

            TestSetConstKind(code, expected, EnvDTE80.vsCMConstKind.vsCMConstKindNone)
        End Sub

#End Region

#Region "Set InitExpression tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression1()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression2()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression3()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression4()
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

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression5()
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

            TestSetInitExpression(code, expected, "19 + 23")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression6()
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

            TestSetInitExpression(code, expected, "")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression7()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo = 42
End Enum
</Code>

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression8()
            Dim code =
<Code>
Enum E
    $$Foo = 0
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo = 42
End Enum
</Code>

            TestSetInitExpression(code, expected, "42")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetInitExpression9()
            Dim code =
<Code>
Enum E
    $$Foo = 0
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            TestSetInitExpression(code, expected, Nothing)
        End Sub

#End Region

#Region "Set IsConstant tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant1()
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

            TestSetIsConstant(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant2()
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

            TestSetIsConstant(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant3()
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

            TestSetIsConstant(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant4()
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

            TestSetIsConstant(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant5()
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

            TestSetIsConstant(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant6()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            TestSetIsConstant(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsConstant7()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            TestSetIsConstant(code, expected, False, ThrowsNotImplementedException(Of Boolean))
        End Sub

#End Region

#Region "Set IsShared tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared1()
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

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared2()
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

            TestSetIsShared(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared3()
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

            TestSetIsShared(code, expected, True)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared4()
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

            TestSetIsShared(code, expected, False)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared5()
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

            TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetIsShared6()
            Dim code =
<Code>
Enum E
    $$Foo
End Enum
</Code>

            Dim expected =
<Code>
Enum E
    Foo
End Enum
</Code>

            TestSetIsShared(code, expected, True, ThrowsNotImplementedException(Of Boolean))
        End Sub

#End Region

#Region "Set Name tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code>
Class C
    Dim $$Foo As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Dim Bar As Integer
End Class
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName2()
            Dim code =
<Code>
Class C

#Region "Foo"

    Dim $$Foo As Integer

#End Region

End Class
</Code>

            Dim expected =
<Code>
Class C

#Region "Foo"

    Dim Bar As Integer

#End Region

End Class
</Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub

#End Region

#Region "Set Type tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType1()
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

            TestSetTypeProp(code, expected, "double")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType2()
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

            TestSetTypeProp(code, expected, "double")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType3()
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

            TestSetTypeProp(code, expected, "String")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType4()
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

            TestSetTypeProp(code, expected, "String")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetType5()
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

            TestSetTypeProp(code, expected, "String")
        End Sub

#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
