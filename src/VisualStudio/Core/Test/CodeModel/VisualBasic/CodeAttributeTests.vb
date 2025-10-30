' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeAttributeTests
        Inherits AbstractCodeAttributeTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint3()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentStartPoint1()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(0,,Z:=42)&gt;

Class GooAttribute
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentStartPoint2()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(0,,Z:=42)&gt;

Class GooAttribute
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentStartPoint3()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(,Z:=42)&gt;

Class GooAttribute
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentStartPoint4()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(,Z:=42)&gt;

Class GooAttribute
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentEndPoint1()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(0,,Z:=42)&gt;

Class GooAttribute
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentEndPoint2()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(0,,Z:=42)&gt;

Class GooAttribute
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentEndPoint3()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(,Z:=42)&gt;

Class GooAttribute
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetAttributeArgumentEndPoint4()
            Dim code =
<Code>
Imports System

&lt;$$Assembly: Goo(,Z:=42)&gt;

Class GooAttribute
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetFullName1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestFullName(code, "System.SerializableAttribute")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetFullName2()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetParent1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestParent(code, IsElement("C", kind:=EnvDTE.vsCMElement.vsCMElementClass))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetParent2()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments2()
            Dim code =
<Code>
Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            TestAttributeArguments(code, NoElements)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments3()
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant(True)&gt;
Class C
End Class
</Code>

            TestAttributeArguments(code, IsAttributeArgument(value:="True"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments4()
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetArguments5_Omitted()
            Dim code =
<Code>
&lt;$$Goo(, Baz:=True)&gt;
Class GooAttribute
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetTarget1()
            Dim code =
<Code>
Imports System

&lt;Assembly: CLSCompliant$$(False)&gt;
</Code>

            TestTarget(code, "Assembly")
        End Sub
#End Region

#Region "Value tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetValue1()
            Dim code =
<Code>
Imports System

&lt;$$Serializable&gt;
Class C
End Class
</Code>

            TestValue(code, "")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetValue2()
            Dim code =
<Code>
Imports System

&lt;$$Serializable()&gt;
Class C
End Class
</Code>

            TestValue(code, "")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetValue3()
            Dim code =
<Code>
Imports System

&lt;$$CLSCompliant(False)&gt;
Class C
End Class
</Code>

            TestValue(code, "False")

        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetValue4()
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
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttributeArgument1() As Task
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

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "True"})

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttributeArgument2() As Task
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

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Value = "True"})

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddArgument3() As Task
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

            Await TestAddAttributeArgument(code, expectedCode, New AttributeArgumentData With {.Name = "AllowMultiple", .Value = "False", .Position = 1})

        End Function
#End Region

#Region "Delete tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete1() As Task
            Dim code =
<Code><![CDATA[
<$$Goo>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)

        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete2() As Task
            Dim code =
<Code><![CDATA[
<$$Goo, Bar>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar>
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete3() As Task
            Dim code =
<Code><![CDATA[
<Goo>
<$$Bar>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Goo>
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete4() As Task
            Dim code =
<Code><![CDATA[
<Assembly: $$Goo>
]]></Code>

            Dim expected =
<Code><![CDATA[
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete5() As Task
            Dim code =
<Code><![CDATA[
<Assembly: $$Goo, Assembly: Bar>
]]></Code>

            Dim expected =
<Code><![CDATA[
<Assembly: Bar>
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete6() As Task
            Dim code =
<Code><![CDATA[
<Assembly: Goo>
<Assembly: $$Bar>
]]></Code>

            Dim expected =
<Code><![CDATA[
<Assembly: Goo>
]]></Code>

            Await TestDelete(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete7() As Task
            Dim code =
<Code><![CDATA[
''' <summary>
''' Doc comment
''' </summary>
<$$Goo>
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

            Await TestDelete(code, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDelete8() As Task
            Dim code =
<Code><![CDATA[
<$$Goo> ' Comment comment comment
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
Class C
End Class
]]></Code>

            Await TestDelete(code, expected)
        End Function

#End Region

#Region "Delete attribute argument tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument1() As Task
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

            Await TestDeleteAttributeArgument(code, expected, 1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument2() As Task
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

            Await TestDeleteAttributeArgument(code, expected, 1)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestDeleteAttributeArgument3() As Task
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

            Await TestDeleteAttributeArgument(code, expected, 2)
        End Function

#End Region

#Region "Set Name tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_NewName() As Task
            Dim code =
<Code><![CDATA[
<$$Goo()>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar()>
Class C
End Class
]]></Code>

            Await TestSetName(code, expected, "Bar", NoThrow(Of String)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_SimpleNameToDottedName() As Task
            Dim code =
<Code><![CDATA[
<$$Goo()>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar.Baz()>
Class C
End Class
]]></Code>

            Await TestSetName(code, expected, "Bar.Baz", NoThrow(Of String)())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetName_DottedNameToSimpleName() As Task
            Dim code =
<Code><![CDATA[
<$$Goo()>
Class C
End Class
]]></Code>

            Dim expected =
<Code><![CDATA[
<Bar.Baz()>
Class C
End Class
]]></Code>

            Await TestSetName(code, expected, "Bar.Baz", NoThrow(Of String)())
        End Function
#End Region

#Region "Set Target tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget1() As Task
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

            Await TestSetTarget(code, expected, "Module")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget2() As Task
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

            Await TestSetTarget(code, expected, "Assembly")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetTarget3() As Task
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

            Await TestSetTarget(code, expected, "")
        End Function
#End Region

#Region "Set Value tests"
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue1() As Task
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

            Await TestSetValue(code, expected, "True")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue2() As Task
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

            Await TestSetValue(code, expected, "True")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue3() As Task
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

            Await TestSetValue(code, expected, "True")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetValue4() As Task
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

            Await TestSetValue(code, expected, "")
        End Function
#End Region

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace

