' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeAttributeTests
        Inherits AbstractCodeAttributeTests

#Region "GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint1()
            Dim code =
<Code>Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=16)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint2()
            Dim code =
<Code>Imports System

&lt;$$CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=2, absoluteOffset:=18, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetStartPoint3()
            Dim code =
<Code>
&lt;$$Assembly: CLSCompliant(True)&gt;
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=12, absoluteOffset:=12, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=2, absoluteOffset:=2, lineLength:=30)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint1()
            Dim code =
<Code>Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=30, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=16)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint2()
            Dim code =
<Code>Imports System

&lt;$$CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=14, absoluteOffset:=30, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=20, absoluteOffset:=36, lineLength:=20)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetEndPoint3()
            Dim code =
<Code>
&lt;$$Assembly: CLSCompliant(True)&gt;
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=24, absoluteOffset:=24, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=30, absoluteOffset:=30, lineLength:=30)))
        End Sub

#End Region

#Region "AttributeArgument GetStartPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentStartPoint1()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentStartPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=25)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentStartPoint2()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentStartPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentStartPoint3()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentStartPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=15, absoluteOffset:=31, lineLength:=23)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentStartPoint4()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentStartPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=23)))
        End Sub

#End Region

#Region "AttributeArgument GetEndPoint() tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentEndPoint1()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentEndPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=17, absoluteOffset:=33, lineLength:=25)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentEndPoint2()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(0,,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(x As Integer, Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentEndPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=25)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentEndPoint3()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentEndPoint(code, 1,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=16, absoluteOffset:=32, lineLength:=23)))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetAttributeArgumentEndPoint4()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Foo(,Z:=42)&gt;

Class FooAttribute
    Inherits Attribute

    Sub New(Optional y As Integer = 0)

    End Sub

    Property Z As integer
End Class
</Code>

            TestAttributeArgumentEndPoint(code, 2,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=18, absoluteOffset:=34, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=38, lineLength:=23)))
        End Sub

#End Region

#Region "FullName tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetFullName1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestFullName(code, "System.SerializableAttribute")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetFullName2()
            Dim code =
<Code>
&lt;$$System.Serializable&gt;
Class C
End Class
</Code>

            TestFullName(code, "System.SerializableAttribute")
        End Sub

#End Region

#Region "Parent tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetParent1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetParent2()
            Dim code =
<Code>
Imports System

&lt;Serializable, $$CLSCompliant(False)&gt;
Class C
End Class
</Code>

            TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub
#End Region

#Region "Attribute argument tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments2()
            Dim code =
<Code>
Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments3()
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestAttributeArguments(code, IsAttributeArgument(value:="True"))
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments4()
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All, AllowMultiple := False)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            TestAttributeArguments(code, IsAttributeArgument(value:="AttributeTargets.All"), IsAttributeArgument(name:="AllowMultiple", value:="False"))

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetArguments5_Omitted()
            Dim code =
<Code>
&lt;$$Foo(, Baz:=True)&gt;
Class FooAttribute
    Inherits Attribute

    Sub New(Optional bar As String = Nothing)

    End Sub

    Public Property Baz As Boolean
        Get

        End Get
        Set(value As Boolean)

        End Set
    End Property
End Class
</Code>

            TestAttributeArguments(code, IsAttributeArgument(name:=""), IsAttributeArgument(name:="Baz", value:="True"))

        End Sub

#End Region

#Region "Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetTarget1()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            TestTarget(code, "Assembly")
        End Sub
#End Region

#Region "Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestValue(code, "")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue2()
            Dim code =
<Code>
Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            TestValue(code, "")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue3()
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant(False)&gt;
Class C
End Class
</Code>

            TestValue(code, "False")

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub GetValue4()
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All, AllowMultiple = False)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            TestValue(code, "AttributeTargets.All, AllowMultiple = False")
        End Sub
#End Region

#Region "AddAttributeArgument tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttributeArgument1()
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant&gt;
Class C
End Class
</Code>

            Dim expectedCode =
<Code>
Imports System

&lt;CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "True"})

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub AddAttributeArgument2()
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant()&gt;
Class C
End Class
</Code>

            Dim expectedCode =
<Code>
Imports System

&lt;CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "True"})

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAddArgument3()
            Dim code =
<Code>
Imports System

&lt;$$AttributeUsage(AttributeTargets.All)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            Dim expectedCode =
<Code>
Imports System

&lt;AttributeUsage(AttributeTargets.All, AllowMultiple:=False)&gt;
Class CAttribute
    Inherits Attribute
End Class
</Code>

            TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Name = "AllowMultiple", .Value = "False", .Position = 1})

        End Sub
#End Region

#Region "Delete tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete1()
            Dim code =
<Code><![CDATA[
<$$Foo>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
End Class
]]></Code>

            TestDelete(code, expected)

        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete2()
            Dim code =
<Code><![CDATA[
<$$Foo, Bar>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar>
Class C
End Class
]]></Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete3()
            Dim code =
<Code><![CDATA[
<Foo>
<$$Bar>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Foo>
Class C
End Class
]]></Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete4()
            Dim code =
<Code><![CDATA[
<Assembly: $$Foo>
]]></Code>

            Dim expected =
<Code><![CDATA[
]]></Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete5()
            Dim code =
<Code><![CDATA[
<Assembly: $$Foo, Assembly: Bar>
]]></Code>

            Dim expected =
<Code><![CDATA[
<Assembly: Bar>
]]></Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete6()
            Dim code =
<Code><![CDATA[
<Assembly: Foo>
<Assembly: $$Bar>
]]></Code>

            Dim expected =
<Code><![CDATA[
<Assembly: Foo>
]]></Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete7()
            Dim code =
<Code><![CDATA[
''' <summary>
''' Doc comment
''' </summary>
<$$Foo>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
''' <summary>
''' Doc comment
''' </summary>
Class C
End Class
]]></Code>

            TestDelete(code, expected)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub Delete8()
            Dim code =
<Code><![CDATA[
<$$Foo> ' Comment comment comment
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
End Class
]]></Code>

            TestDelete(code, expected)
        End Sub

#End Region

#Region "Delete attribute argument tests"

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DeleteAttributeArgument1()
            Dim code =
<Code><![CDATA[
<$$System.CLSCompliant(True)>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<System.CLSCompliant()>
Class C
End Class
]]></Code>

            TestDeleteAttributeArgument(code, expected, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DeleteAttributeArgument2()
            Dim code =
<Code><![CDATA[
<$$AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<AttributeUsage(AllowMultiple:=False)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            TestDeleteAttributeArgument(code, expected, 1)
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub DeleteAttributeArgument3()
            Dim code =
<Code><![CDATA[
<$$AttributeUsage(AttributeTargets.All, AllowMultiple:=False)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<AttributeUsage(AttributeTargets.All)>
Class CAttribute
    Inherits Attribute
End Class
]]></Code>

            TestDeleteAttributeArgument(code, expected, 2)
        End Sub

#End Region

#Region "Set Name tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetName1()
            Dim code =
<Code><![CDATA[
<$$Foo()>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar()>
Class C
End Class
]]></Code>

            TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Sub
#End Region

#Region "Set Target tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetTarget1()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Module: CLSCompliant(False)&gt;
</Code>

            TestSetTarget(code, expected, "Module")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetTarget2()
            Dim code =
<Code>
Imports System

&lt;CLSCompliant$$(False)&gt;
Class C
End Class
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(False)&gt;
Class C
End Class
</Code>

            TestSetTarget(code, expected, "Assembly")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetTarget3()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;CLSCompliant(False)&gt;
</Code>

            TestSetTarget(code, expected, "")
        End Sub
#End Region

#Region "Set Value tests"
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue1()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            TestSetValue(code, expected, "True")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue2()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$()&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            TestSetValue(code, expected, "True")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue3()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant(True)&gt;
</Code>

            TestSetValue(code, expected, "True")
        End Sub

        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub SetValue4()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            Dim expected =
<Code>
Imports System

&lt;Assembly: CLSCompliant()&gt;
</Code>

            TestSetValue(code, expected, "")
        End Sub
#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace


