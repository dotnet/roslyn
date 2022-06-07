' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodePropertyTests
        Inherits AbstractCodePropertyTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=11)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_AutoProperty()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=21, absoluteOffset:=29, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=3, lineOffset:=1, absoluteOffset:=42, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=32)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint_AutoProperty_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
End Class
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=21, absoluteOffset:=61, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=4, lineOffset:=1, absoluteOffset:=74, lineLength:=9)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=5, absoluteOffset:=45, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=5, absoluteOffset:=13, lineLength:=31)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=7, lineOffset:=5, absoluteOffset:=120, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=7, lineOffset:=5, absoluteOffset:=120, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=30, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=7, lineOffset:=5, absoluteOffset:=120, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=7, lineOffset:=17, absoluteOffset:=132, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=7, lineOffset:=17, absoluteOffset:=132, lineLength:=16)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=152, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=152, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=62, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=8, lineOffset:=5, absoluteOffset:=152, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=8, lineOffset:=17, absoluteOffset:=164, lineLength:=16)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=8, lineOffset:=17, absoluteOffset:=164, lineLength:=16)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_AutoProperty()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=22, absoluteOffset:=30, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=33, absoluteOffset:=41, lineLength:=32)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint_AutoProperty_Attribute()
            Dim code =
<Code>
Class C
    &lt;System.CLSCompliant(True)&gt;
    Public Property $$P As Integer
End Class
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=2, lineOffset:=32, absoluteOffset:=40, lineLength:=31)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=3, lineOffset:=22, absoluteOffset:=62, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=7)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=3, lineOffset:=33, absoluteOffset:=73, lineLength:=32)))
        End Sub

#End Region

#Region "Attributes"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes_AutoProperty()
            Dim code =
<Code>
Imports System

Class C1
    &lt;CLSCompliant(False)&gt;
    Public Property $$P As String
End Class
</Code>

            TestAttributes(code, IsElement("CLSCompliant"))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes_Property()
            Dim code =
<Code>
Imports System

Class C1
    &lt;CLSCompliant(False)&gt;
    Public Property $$P As String
        Get
        End Get
        Set(value As String)
        End Set
    End Property
End Class
</Code>

            TestAttributes(code, IsElement("CLSCompliant"))
        End Sub

#End Region

#Region "Getter tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetterIsNothingForAutoProp()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestGetter(code,
                       Sub(accessor)
                           Assert.Null(accessor)
                       End Sub)
        End Sub

#End Region

#Region "IsDefault tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDefault1()
            Dim code =
<Code>
Class C
    Public Default Property $$P(index as Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestIsDefault(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDefault2()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestIsDefault(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestIsDefault3()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestIsDefault(code, False)
        End Sub

#End Region

#Region "Name tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName1()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestName(code, "P")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestName2()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestName(code, "P")
        End Sub

#End Region

#Region "OverrideKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_None()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Abstract()
            Dim code =
<Code>
MustInherit Class C
    Public MustOverride Property $$P As Integer
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Virtual()
            Dim code =
<Code>
Class C
    Public Overridable Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindVirtual)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Override()
            Dim code =
<Code>
MustInherit Class A
    Public MustOverride Property P As Integer
End Class

Class C
    Inherits A

    Public Overrides Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_Sealed()
            Dim code =
<Code>
MustInherit Class A
    Public MustOverride Property P As Integer
End Class

Class C
    Inherits A

    Public NotOverridable Overrides Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindOverride Or EnvDTE80.vsCMOverrideKind.vsCMOverrideKindSealed)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestOverrideKind_New()
            Dim code =
<Code>
MustInherit Class A
    Public Property P As Integer
End Class

Class C
    Inherits A

    Public Shadows Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestOverrideKind(code, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNew)
        End Sub

#End Region

#Region "Prototype tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_UniqueSignature()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeUniqueSignature, "P:N.C.P")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_FullName()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeFullname, "N.C.P()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName1()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer = 42
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName2()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$ As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName3()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName, "C.P()")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName_ParamNames()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames, "C.P(index )")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName_ParamNames_ParamTypes()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "C.P(index As String)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName_ParamNames_ParamDefaultValues()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(index  = """")")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName_ParamTypes()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes, "C.P(String)")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName_ParamTypes_ParamDefaultValues()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(String = """")")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_ClassName_ParamNames_ParamTypes_ParamDefaultValues()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeClassName Or EnvDTE.vsCMPrototype.vsCMPrototypeParamNames Or EnvDTE.vsCMPrototype.vsCMPrototypeParamTypes Or EnvDTE.vsCMPrototype.vsCMPrototypeParamDefaultValues, "C.P(index As String = """")")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestPrototype_Type()
            Dim code =
<Code>
Namespace N
    Class C
        Property P$$(Optional index As String = "") As Integer
            Get
            End Get
            Set(value As Integer)
            End Set
        End Property
    End Class
End Namespace
</Code>

            TestPrototype(code, EnvDTE.vsCMPrototype.vsCMPrototypeType, "P()")
        End Sub

#End Region

#Region "ReadWrite tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_GetSet()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Get()
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
        Get
        End Get
    End Property
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Set()
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_GetSet_AutoProperty()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadWrite)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Get_AutoProperty()
            Dim code =
<Code>
Class C
    Public ReadOnly Property $$P As Integer
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindReadOnly)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestReadWrite_Set_AutoProperty()
            Dim code =
<Code>
Class C
    Public WriteOnly Property $$P As Integer
End Class
</Code>

            TestReadWrite(code, EnvDTE80.vsCMPropertyKind.vsCMPropertyKindWriteOnly)
        End Sub

#End Region

#Region "Setter tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestSetterIsNothingForAutoProp()
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            TestSetter(code,
                       Sub(accessor)
                           Assert.Null(accessor)
                       End Sub)
        End Sub

#End Region

#Region "Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType1()
            Dim code =
<Code>
Class C
    Property $$Goo As Integer
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Int32",
                             .AsString = "Integer",
                             .CodeTypeFullName = "System.Int32",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefInt
                         })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType2()
            Dim code =
<Code>
Class C
    Property $$Goo As New System.Text.StringBuilder
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.Text.StringBuilder",
                             .AsString = "System.Text.StringBuilder",
                             .CodeTypeFullName = "System.Text.StringBuilder",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType
                         })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType3()
            Dim code =
<Code>
Class C
    Property $$Goo As String
        Get

        End Get
        Set(value As String)

        End Set
    End Property
End Class
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsFullName = "System.String",
                             .AsString = "String",
                             .CodeTypeFullName = "System.String",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefString
                         })
        End Sub

#End Region

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_NormalProperty() As Task
            Dim code =
<Code>
Imports System

Class C
    Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_AutoProperty() As Task
            Dim code =
<Code>
Imports System

Class C
    Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    &lt;Serializable()&gt;
    Property P As Integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_NormalProperty_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WorkItem(2825, "https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_AutoProperty_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Imports System

Class C
    ''' &lt;summary&gt;&lt;/summary&gt;
    &lt;Serializable()&gt;
    Property P As Integer
End Class
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

#End Region

#Region "AddParameter tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter1() As Task
            Dim code =
<Code>
Class C
    Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property P(index As Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "index", .Type = "Integer"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter2() As Task
            Dim code =
<Code>
Class C
    Property $$P() As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property P(index As Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "index", .Type = "Integer"})
        End Function

#End Region

#Region "RemoveParameter tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter1() As Task
            Dim code =
<Code>
Class C
    Property $$P(index As Integer) As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property P() As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestRemoveChild(code, expected, "index")
        End Function

#End Region

#Region "Set IsDefault tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Default Public Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestSetIsDefault(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault2() As Task
            Dim code =
<Code>
Class C
    Default Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestSetIsDefault(code, expected, False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault3() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Default Public Property P As Integer
End Class
</Code>

            Await TestSetIsDefault(code, expected, True)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetIsDefault4() As Task
            Dim code =
<Code>
Class C
    Default Public Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P As Integer
End Class
</Code>

            Await TestSetIsDefault(code, expected, False)
        End Function

#End Region

#Region "Set OverrideKind tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind1() As Task
            Dim code =
<Code>
Class C
    Public Property $$P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public MustOverride Property P As Integer
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindAbstract)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind2() As Task
            Dim code =
<Code>
Class C
    Public MustOverride Property $$P As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P As Integer
        Get
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetOverrideKind3() As Task
            Dim code =
<Code>
Class C
    Public MustOverride Property $$P$
End Class
</Code>

            Dim expected =
<Code>
Class C
    Public Property P$
        Get
        End Get
        Set(value$)
        End Set
    End Property
End Class
</Code>

            Await TestSetOverrideKind(code, expected, EnvDTE80.vsCMOverrideKind.vsCMOverrideKindNone)
        End Function

#End Region

#Region "Set Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Class C
    Property $$Goo As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Goo As String
End Class
</Code>

            Await TestSetTypeProp(code, expected, "String")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Class C
    Property $$Goo As New System.Text.StringBuilder
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Goo As New Integer
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Class C
    Property $$Goo As String
        Get

        End Get
        Set(value As String)

        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Goo As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Property $$Goo$
        Get

        End Get
        Set(value As String)

        End Set
    End Property
End Class
</Code>

            Dim expected =
<Code>
Class C
    Property Goo$ As Integer
        Get

        End Get
        Set(value As Integer)

        End Set
    End Property
End Class
</Code>

            Await TestSetTypeProp(code, expected, "System.Int32")
        End Function

#End Region

#Region "AutoImplementedPropertyExtender"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoImplementedPropertyExtender_IsAutoImplemented1()
            Dim code =
<Code>
Public Class C
    Property $$P As Integer
End Class
</Code>

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, True)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoImplementedPropertyExtender_IsAutoImplemented2()
            Dim code =
<Code>
Public Class C
    Property $$P As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
</Code>

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAutoImplementedPropertyExtender_IsAutoImplemented3()
            Dim code =
<Code>
Public Interface I
    Property $$P As Integer
End Interface
</Code>

            TestAutoImplementedPropertyExtender_IsAutoImplemented(code, False)
        End Sub

#End Region

#Region "Parameter name tests"

        <WorkItem(1147885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
            Dim code =
<Code>
Class Program
    Property $$P([integer] As Integer) As Integer
        Get
            Return [integer]
        End Get
        Set(value As Integer)

        End Set
    End Property
    Sub Main(args As String())

    End Sub
End Class
</Code>

            TestAllParameterNames(code, "[integer]")
        End Sub

        <WorkItem(1147885, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters_2()
            Dim code =
<Code>
Class Program
    Property $$P([integer] As Integer, [string] as String) As Integer
        Get
            Return [integer]
        End Get
        Set(value As Integer)

        End Set
    End Property
    Sub Main(args As String())

    End Sub
End Class
</Code>

            TestAllParameterNames(code, "[integer]", "[string]")
        End Sub

#End Region

        Private Shared Function GetAutoImplementedPropertyExtender(codeElement As EnvDTE80.CodeProperty2) As IVBAutoPropertyExtender
            Return CType(codeElement.Extender(ExtenderNames.VBAutoPropertyExtender), IVBAutoPropertyExtender)
        End Function

        Protected Overrides Function AutoImplementedPropertyExtender_GetIsAutoImplemented(codeElement As EnvDTE80.CodeProperty2) As Boolean
            Return GetAutoImplementedPropertyExtender(codeElement).IsAutoImplemented
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
