' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.Emit
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SymbolMatcherTests
        Inherits EditAndContinueTestBase

        <WorkItem(1533)>
        <Fact>
        Public Sub PreviousType_ArrayType()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As Integer = 0
    End Sub
    Class D : End Class
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x() As D = Nothing
    End Sub
    Class D : End Class
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = New VisualBasicSymbolMatcher(
                Nothing,
                compilation1.SourceAssembly,
                Nothing,
                compilation0.SourceAssembly,
                Nothing,
                Nothing)
            Dim elementType = compilation1.GetMember(Of TypeSymbol)("C.D")
            Dim member = compilation1.CreateArrayTypeSymbol(elementType)
            Dim other = matcher.MapReference(member)
            Assert.NotNull(other)
        End Sub

        <WorkItem(1533)>
        <Fact>
        Public Sub NoPreviousType_ArrayType()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x As Integer = 0
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Class C
    Shared Sub M()
        Dim x() As D = Nothing
    End Sub
    Class D : End Class
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = New VisualBasicSymbolMatcher(
                Nothing,
                compilation1.SourceAssembly,
                Nothing,
                compilation0.SourceAssembly,
                Nothing,
                Nothing)
            Dim elementType = compilation1.GetMember(Of TypeSymbol)("C.D")
            Dim member = compilation1.CreateArrayTypeSymbol(elementType)
            Dim other = matcher.MapReference(member)
            ' For a newly added type, there is no match in the previous generation.
            Assert.Null(other)
        End Sub

        <WorkItem(1533)>
                                   <Fact>
        Public Sub NoPreviousType_GenericType()
            Dim sources0 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Class C
    Shared Sub M()
        Dim x As Integer = 0
    End Sub
End Class
]]></file>
                           </compilation>
            Dim sources1 = <compilation>
                               <file name="a.vb"><![CDATA[
Imports System.Collections.Generic
Class C
    Shared Sub M()
        Dim x As List(Of D) = Nothing
    End Sub
    Class D : End Class
    Dim y As List(Of D)
End Class
]]></file>
                           </compilation>

            Dim compilation0 = CreateCompilationWithMscorlib(sources0, TestOptions.DebugDll)
            Dim compilation1 = compilation0.WithSource(sources1)
            Dim matcher = New VisualBasicSymbolMatcher(
                Nothing,
                compilation1.SourceAssembly,
                Nothing,
                compilation0.SourceAssembly,
                Nothing,
                Nothing)
            Dim member = compilation1.GetMember(Of FieldSymbol)("C.y")
            Dim other = matcher.MapReference(DirectCast(member.Type, Cci.ITypeReference))
            ' For a newly added type, there is no match in the previous generation.
            Assert.Null(other)
        End Sub
    End Class
End Namespace