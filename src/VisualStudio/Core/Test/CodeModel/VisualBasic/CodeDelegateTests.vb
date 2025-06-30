' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Extenders
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.CodeModel.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.VisualBasic
    Public Class CodeDelegateTests
        Inherits AbstractCodeDelegateTests

#Region "GetStartPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint1()
            Dim code =
<Code>
Delegate Sub $$Goo(i As Integer)
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=14, absoluteOffset:=14, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=30)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetStartPoint2()
            Dim code =
<Code>
&lt;System.CLSCompliant(True)&gt;
Delegate Sub $$Goo(i As Integer)
</Code>

            TestGetStartPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=29, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=29, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=29, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=14, absoluteOffset:=42, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=29, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=1, absoluteOffset:=29, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=1, absoluteOffset:=1, lineLength:=27)))
        End Sub

#End Region

#Region "GetEndPoint() tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint1()
            Dim code =
<Code>
Delegate Sub $$Goo(i As Integer)
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     NullTextPoint),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=1, lineOffset:=31, absoluteOffset:=31, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=1, lineOffset:=31, absoluteOffset:=31, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=1, lineOffset:=31, absoluteOffset:=31, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=1, lineOffset:=31, absoluteOffset:=31, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=1, lineOffset:=17, absoluteOffset:=17, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=1, lineOffset:=31, absoluteOffset:=31, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=1, lineOffset:=31, absoluteOffset:=31, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=1, lineOffset:=31, absoluteOffset:=31, lineLength:=30)))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGetEndPoint2()
            Dim code =
<Code>
&lt;System.CLSCompliant(True)&gt;
Delegate Sub $$Goo(i As Integer)
</Code>

            TestGetEndPoint(code,
                Part(EnvDTE.vsCMPart.vsCMPartAttributes,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartAttributesWithDelimiter,
                     TextPoint(line:=1, lineOffset:=28, absoluteOffset:=28, lineLength:=27)),
                Part(EnvDTE.vsCMPart.vsCMPartBody,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=59, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartBodyWithDelimiter,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=59, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeader,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=59, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartHeaderWithAttributes,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=59, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartName,
                     TextPoint(line:=2, lineOffset:=17, absoluteOffset:=45, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartNavigate,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=59, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWhole,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=59, lineLength:=30)),
                Part(EnvDTE.vsCMPart.vsCMPartWholeWithAttributes,
                     TextPoint(line:=2, lineOffset:=31, absoluteOffset:=59, lineLength:=30)))
        End Sub

#End Region

#Region "Attributes"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestAttributes1()
            Dim code =
<Code>
Imports System

&lt;CLSCompliant(False)&gt;
Delegate Sub $$D()
</Code>

            TestAttributes(code, IsElement("CLSCompliant"))
        End Sub

#End Region

#Region "BaseClass tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestBaseClass1()
            Dim code =
<Code>
Delegate Sub $$D()
</Code>

            TestBaseClass(code, "System.Delegate")
        End Sub

#End Region

#Region "Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType_Void()
            Dim code =
<Code>
Delegate Sub $$D()
</Code>

            TestTypeProp(code,
                         New CodeTypeRefData With {
                             .AsString = "Void",
                             .AsFullName = "System.Void",
                             .CodeTypeFullName = "System.Void",
                             .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefVoid
                         })
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestType_Int()
            Dim code =
<Code>
Delegate Function $$D() As Integer
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
        Public Sub TestType_SourceClass()
            Dim code =
<Code>
Class C : End Class
Delegate Function $$D() As C
</Code>

            TestTypeProp(code, New CodeTypeRefData With {.CodeTypeFullName = "C", .TypeKind = EnvDTE.vsCMTypeRef.vsCMTypeRefCodeType})
        End Sub

#End Region

#Region "Set Type tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType1() As Task
            Dim code =
<Code>
Delegate Sub $$D()
</Code>

            Dim expected =
<Code>
Delegate Function D() As Integer
</Code>

            Await TestSetTypeProp(code, expected, "Integer")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType2() As Task
            Dim code =
<Code>
Delegate Function $$D() As Integer
</Code>

            Dim expected =
<Code>
Delegate Function D() As Decimal
</Code>

            Await TestSetTypeProp(code, expected, "System.Decimal")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType3() As Task
            Dim code =
<Code>
Delegate Function $$D() As Integer
</Code>

            Dim expected =
<Code>
Delegate Sub D()
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType4() As Task
            Dim code =
<Code>
Class C
    Delegate Sub $$D()
End Class
</Code>

            Dim expected =
<Code>
Class C
    Delegate Function D() As Integer
End Class
</Code>

            Await TestSetTypeProp(code, expected, "Integer")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType5() As Task
            Dim code =
<Code>
Class C
    Delegate Function $$D() As Integer
End Class
</Code>

            Dim expected =
<Code>
Class C
    Delegate Sub D()
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestSetType6() As Task
            Dim code =
<Code>
Class C
    Delegate Sub $$D()
End Class
</Code>

            Dim expected =
<Code>
Class C
    Delegate Sub D()
End Class
</Code>

            Await TestSetTypeProp(code, expected, CType(Nothing, EnvDTE.CodeTypeRef))
        End Function

#End Region

#Region "AddParameter tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter1() As Task
            Dim code =
<Code>
Delegate Sub $$M()
</Code>

            Dim expected =
<Code>
Delegate Sub M(a As Integer)
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "a", .Type = "Integer"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter2() As Task
            Dim code =
<Code>
Delegate Sub $$M(a As Integer)
</Code>

            Dim expected =
<Code>
Delegate Sub M(b As String, a As Integer)
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter3() As Task
            Dim code =
<Code>
Delegate Sub $$M(b As String, a As Integer)
</Code>

            Dim expected =
<Code>
Delegate Sub M(b As String, c As Boolean, a As Integer)
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "c", .Type = "System.Boolean", .Position = 1})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddParameter4() As Task
            Dim code =
<Code>
Delegate Sub $$M(a As Integer)
</Code>

            Dim expected =
<Code>
Delegate Sub M(a As Integer, b As String)
</Code>

            Await TestAddParameter(code, expected, New ParameterData With {.Name = "b", .Type = "String", .Position = -1})
        End Function

#End Region

#Region "RemoveParameter tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter1() As Task
            Dim code =
<Code>
Delegate Sub $$M(a As Integer)
</Code>

            Dim expected =
<Code>
Delegate Sub M()
</Code>

            Await TestRemoveChild(code, expected, "a")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter2() As Task
            Dim code =
<Code>
Delegate Sub $$M(a As Integer, b As String)
</Code>

            Dim expected =
<Code>
Delegate Sub M(a As Integer)
</Code>

            Await TestRemoveChild(code, expected, "b")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter3() As Task
            Dim code =
<Code>
Delegate Sub $$M(a As Integer, b As String)
</Code>

            Dim expected =
<Code>
Delegate Sub M(b As String)
</Code>

            Await TestRemoveChild(code, expected, "a")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestRemoveParameter4() As Task
            Dim code =
<Code>
Delegate Sub $$M(a As Integer, b As String, c As Integer)
</Code>

            Dim expected =
<Code>
Delegate Sub M(a As Integer, c As Integer)
</Code>

            Await TestRemoveChild(code, expected, "b")
        End Function

#End Region

#Region "GenericExtender"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseTypesCount()
            Dim code =
<Code>
Delegate Sub $$D()
</Code>

            TestGenericNameExtender_GetBaseTypesCount(code, 1)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetBaseGenericName()
            Dim code =
<Code>
Delegate Sub $$D()
</Code>

            TestGenericNameExtender_GetBaseGenericName(code, 1, "System.MulticastDelegate")
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplementedTypesCount()
            Dim code =
<Code>
Delegate Sub $$D()
</Code>

            TestGenericNameExtender_GetImplementedTypesCountThrows(Of ArgumentException)(code)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestGenericExtender_GetImplTypeGenericName()
            Dim code =
<Code>
Delegate Sub $$D()
</Code>

            TestGenericNameExtender_GetImplTypeGenericName(code, 1, Nothing)
        End Sub

#End Region

#Region "Parameter name tests"

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters()
            Dim code =
<Code>
Delegate Sub $$D([integer] as Integer)
</Code>

            TestAllParameterNames(code, "[integer]")
        End Sub

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1147885")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Sub TestParameterNameWithEscapeCharacters_2()
            Dim code =
<Code>
Delegate Sub $$D([integer] as Integer, [string] as String)
</Code>

            TestAllParameterNames(code, "[integer]", "[string]")
        End Sub

#End Region

#Region "AddAttribute tests"

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute1() As Task
            Dim code =
<Code>
Imports System

Delegate Sub $$M()
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable()&gt;
Delegate Sub M()
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "Serializable"})
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute2() As Task
            Dim code =
<Code>
Imports System

&lt;Serializable&gt;
Delegate Sub $$M()
</Code>

            Dim expected =
<Code>
Imports System

&lt;Serializable&gt;
&lt;CLSCompliant(true)&gt;
Delegate Sub M()
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true", .Position = 1})
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/2825")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeModel)>
        Public Async Function TestAddAttribute_BelowDocComment() As Task
            Dim code =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
Delegate Sub $$M()
</Code>

            Dim expected =
<Code>
Imports System

''' &lt;summary&gt;&lt;/summary&gt;
&lt;CLSCompliant(true)&gt;
Delegate Sub M()
</Code>
            Await TestAddAttribute(code, expected, New AttributeData With {.Name = "CLSCompliant", .Value = "true"})
        End Function

#End Region

        Private Shared Function GetGenericExtender(codeElement As EnvDTE80.CodeDelegate2) As IVBGenericExtender
            Return CType(codeElement.Extender(ExtenderNames.VBGenericExtender), IVBGenericExtender)
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseTypesCount(codeElement As EnvDTE80.CodeDelegate2) As Integer
            Return GetGenericExtender(codeElement).GetBaseTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetImplementedTypesCount(codeElement As EnvDTE80.CodeDelegate2) As Integer
            Return GetGenericExtender(codeElement).GetImplementedTypesCount()
        End Function

        Protected Overrides Function GenericNameExtender_GetBaseGenericName(codeElement As EnvDTE80.CodeDelegate2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetBaseGenericName(index)
        End Function

        Protected Overrides Function GenericNameExtender_GetImplTypeGenericName(codeElement As EnvDTE80.CodeDelegate2, index As Integer) As String
            Return GetGenericExtender(codeElement).GetImplTypeGenericName(index)
        End Function

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property
    End Class
End Namespace
