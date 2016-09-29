' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SymbolDisplayTests
        Inherits BasicTestBase

        <Fact>
        Public Sub TestClassNameOnlySimple()
            Dim text =
<compilation>
    <file name="a.vb">
class A
end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("A", 0).Single()

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "A",
                {SymbolDisplayPartKind.ClassName})
        End Sub

        <Fact>
        Public Sub TestClassNameOnlyComplex()
            Dim text =
<compilation>
    <file name="a.vb">
namespace N1 
    namespace N2.N3 
        class C1 
            class C2
            end class
        end class
    end namespace
end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                     Return globalns.LookupNestedNamespace({"N1"}).
                                                                     LookupNestedNamespace({"N2"}).
                                                                     LookupNestedNamespace({"N3"}).
                                                                     GetTypeMembers("C1").Single().
                                                                     GetTypeMembers("C2").Single()
                                                                 End Function

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C2",
                {SymbolDisplayPartKind.ClassName})

            format = New SymbolDisplayFormat(
                            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C1.C2",
                {SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Operator, SymbolDisplayPartKind.ClassName})

            format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "N1.N2.N3.C1.C2",
                {SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.ClassName})
        End Sub

        <Fact>
        Public Sub TestFullyQualifiedFormat()
            Dim text =
<compilation>
    <file name="a.vb">
namespace N1 
    namespace N2.N3 
        class C1 
            class C2
            end class
        end class
    end namespace
end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                     Return globalns.LookupNestedNamespace({"N1"}).
                                                                     LookupNestedNamespace({"N2"}).
                                                                     LookupNestedNamespace({"N3"}).
                                                                     GetTypeMembers("C1").Single().
                                                                     GetTypeMembers("C2").Single()
                                                                 End Function

            TestSymbolDescription(
                text,
                findSymbol,
                SymbolDisplayFormat.FullyQualifiedFormat,
                "Global.N1.N2.N3.C1.C2",
                {SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.ClassName})
        End Sub

        <Fact>
        Public Sub TestMethodNameOnlySimple()
            Dim text =
<compilation>
    <file name="a.vb">
        class A 
            Sub M()
            End Sub
        End Class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("A", 0).Single().
                                                                                        GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat()

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M",
                {SymbolDisplayPartKind.MethodName})
        End Sub

        <Fact()>
        Public Sub TestMethodNameOnlyComplex()
            Dim text =
<compilation>
    <file name="a.vb">
namespace N1 
    namespace N2.N3 
        class C1 
            class C2
                public shared function M(nullable x as integer, c as C1) as Integer()
                end function
            end class
        end class
    end namespace
end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                                        LookupNestedNamespace({"N2"}).
                                                                                        LookupNestedNamespace({"N3"}).
                                                                                        GetTypeMembers("C1").Single().
                                                                                        GetTypeMembers("C2").Single().
                                                                                        GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat()

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M",
                {SymbolDisplayPartKind.MethodName})
        End Sub

        <Fact()>
        Public Sub TestClassNameOnlyWithKindSimple()
            Dim text =
<compilation>
    <file name="a.vb">
class A
end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("A", 0).Single()

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                kindOptions:=SymbolDisplayKindOptions.IncludeTypeKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Class A",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName})
        End Sub

        <Fact()>
        Public Sub TestClassWithKindComplex()
            Dim text =
<compilation>
    <file name="a.vb">
namespace N1 
    namespace N2.N3 
        class A 
        end class
    end namespace
end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                                         LookupNestedNamespace({"N2"}).
                                                                                         LookupNestedNamespace({"N3"}).
                                                                                         GetTypeMembers("A").Single()

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                kindOptions:=SymbolDisplayKindOptions.IncludeTypeKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Class N1.N2.N3.A",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName})
        End Sub

        <Fact()>
        Public Sub TestNamespaceWithKindSimple()
            Dim text =
<compilation>
    <file name="a.vb">
namespace N1 
    namespace N2.N3 
        class A 
        end class
    end namespace
end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                                         LookupNestedNamespace({"N2"}).
                                                                                         LookupNestedNamespace({"N3"})

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                kindOptions:=SymbolDisplayKindOptions.IncludeNamespaceKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Namespace N3",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName})
        End Sub

        <Fact()>
        Public Sub TestNamespaceWithKindComplex()
            Dim text =
<compilation>
    <file name="a.vb">
namespace N1 
    namespace N2.N3 
        class A 
        end class
    end namespace
end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                                         LookupNestedNamespace({"N2"}).
                                                                                         LookupNestedNamespace({"N3"})

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                kindOptions:=SymbolDisplayKindOptions.IncludeNamespaceKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Namespace N1.N2.N3",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName})
        End Sub

        <Fact()>
        Public Sub TestMethodAndParamsSimple()
            Dim text =
<compilation>
    <file name="a.vb">
        class A 
            Private sub M()
            End Sub
        end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("A", 0).Single().
                                                                                        GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private Sub M()",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})

            format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeType,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M()",
                {SymbolDisplayPartKind.MethodName,
                 SymbolDisplayPartKind.Punctuation,
                 SymbolDisplayPartKind.Punctuation})

        End Sub

        <Fact()>
        Public Sub TestMethodAndParamsComplex()
            Dim text =
<compilation>
    <file name="a.vb">
namespace N1 
    namespace N2.N3 
        class C1 
            class C2
                public shared function M(x? as integer, c as C1) as Integer()
                end function
            end class
        end class
    end namespace
end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                LookupNestedNamespace({"N2"}).
                                                                LookupNestedNamespace({"N3"}).
                                                                GetTypeMembers("C1").Single().
                                                                GetTypeMembers("C2").Single().
                                                                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Shared Function M(x As Integer?, c As C1) As Integer()",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact>
        Public Sub TestExtensionMethodAsStatic()
            Dim text =
<compilation>
    <file name="a.vb">
        class C1(Of T)
        end class
        module C2 
            &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
            public function M(Of TSource)(source As C1(Of TSource), index As Integer) As TSource
            end function
        end module
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("C2", 0).Single().
                                                                                        GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                extensionMethodStyle:=SymbolDisplayExtensionMethodStyle.StaticMethod,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeExtensionThis Or SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Function C2.M(Of TSource)(source As C1(Of TSource), index As Integer) As TSource",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ModuleName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName})
        End Sub

        <Fact>
        Public Sub TestExtensionMethodAsInstance()
            Dim text =
<compilation>
    <file name="a.vb">
        class C1(Of T)
        end class
        module C2 
            &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
            public function M(Of TSource)(source As C1(Of TSource), index As Integer) As TSource
            end function
        end module
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("C2", 0).Single().
                                                                                        GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                extensionMethodStyle:=SymbolDisplayExtensionMethodStyle.InstanceMethod,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeExtensionThis Or SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Function C1(Of TSource).M(index As Integer) As TSource",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName})
        End Sub

        <Fact>
        Public Sub TestExtensionMethodAsDefault()
            Dim text =
<compilation>
    <file name="a.vb">
        class C1(Of T)
        end class
        module C2 
            &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
            public function M(Of TSource)(source As C1(Of TSource), index As Integer) As TSource
            end function
        end module
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("C2", 0).Single().
                                                                                        GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                extensionMethodStyle:=SymbolDisplayExtensionMethodStyle.Default,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeExtensionThis Or SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Function C2.M(Of TSource)(source As C1(Of TSource), index As Integer) As TSource",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ModuleName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName})
        End Sub

        <Fact>
        Public Sub TestIrreducibleExtensionMethodAsInstance()
            Dim text =
<compilation>
    <file name="a.vb">
        class C1(Of T)
        end class
        module C2 
            &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
            public function M(Of TSource As Structure)(source As C1(Of TSource), index As Integer) As TSource
            end function
        end module
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                     Dim c2Type = globalns.GetTypeMember("C2")
                                                                     Dim method = c2Type.GetMember(Of MethodSymbol)("M")
                                                                     Return method.Construct(c2Type)
                                                                 End Function


            Dim format = New SymbolDisplayFormat(
                extensionMethodStyle:=SymbolDisplayExtensionMethodStyle.InstanceMethod,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeExtensionThis Or SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Function C2.M(Of C2)(source As C1(Of C2), index As Integer) As C2",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ModuleName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ModuleName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ModuleName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ModuleName})
        End Sub

        <Fact>
        Public Sub TestReducedExtensionMethodAsStatic()
            Dim text =
<compilation>
    <file name="a.vb">
        class C1
        end class
        module C2 
            &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
            public function M(Of TSource)(source As C1, index As Integer) As TSource
            end function
        end module
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                     Dim type = globalns.GetTypeMember("C1")
                                                                     Dim method = DirectCast(globalns.GetTypeMember("C2").GetMember("M"), MethodSymbol)
                                                                     Return method.ReduceExtensionMethod(type)
                                                                 End Function

            Dim format = New SymbolDisplayFormat(
                extensionMethodStyle:=SymbolDisplayExtensionMethodStyle.StaticMethod,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeExtensionThis Or SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Function C2.M(Of TSource)(source As C1, index As Integer) As TSource",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ModuleName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName})
        End Sub

        <Fact>
        Public Sub TestReducedExtensionMethodAsInstance()
            Dim text =
<compilation>
    <file name="a.vb">
        class C1
        end class
        module C2 
            &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
            public function M(Of TSource)(source As C1, index As Integer) As TSource
            end function
        end module
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                     Dim type = globalns.GetTypeMember("C1")
                                                                     Dim method = DirectCast(globalns.GetTypeMember("C2").GetMember("M"), MethodSymbol)
                                                                     Return method.ReduceExtensionMethod(type)
                                                                 End Function

            Dim format = New SymbolDisplayFormat(
                extensionMethodStyle:=SymbolDisplayExtensionMethodStyle.InstanceMethod,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeExtensionThis Or SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Function C1.M(Of TSource)(index As Integer) As TSource",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName})
        End Sub

        <Fact>
        Public Sub TestReducedExtensionMethodAsDefault()
            Dim text =
<compilation>
    <file name="a.vb">
        class C1
        end class
        module C2 
            &lt;System.Runtime.CompilerServices.ExtensionAttribute()&gt;
            public function M(Of TSource)(source As C1, index As Integer) As TSource
            end function
        end module
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                     Dim type = globalns.GetTypeMember("C1")
                                                                     Dim method = DirectCast(globalns.GetTypeMember("C2").GetMember("M"), MethodSymbol)
                                                                     Return method.ReduceExtensionMethod(type)
                                                                 End Function

            Dim format = New SymbolDisplayFormat(
                extensionMethodStyle:=SymbolDisplayExtensionMethodStyle.Default,
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeModifiers Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeExtensionThis Or SymbolDisplayParameterOptions.IncludeType Or SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Function C1.M(Of TSource)(index As Integer) As TSource",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName})
        End Sub

        <Fact()>
        Public Sub TestNothingParameters()
            Dim text =
            <compilation>
                <file name="a.vb">
class C1 
    dim public f as Integer()(,)(,,)
end class
    </file>
            </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("C1").Single().
                                                                GetMembers("f").Single()

            Dim format As SymbolDisplayFormat = Nothing

            ' default is show asterisks for VB. If this is changed, this test will fail
            ' in this case, please rewrite the test TestNoArrayAsterisks to TestArrayAsterisks
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public f As Integer()(*,*)(*,*,*)",
                {
                        SymbolDisplayPartKind.Keyword,
                        SymbolDisplayPartKind.Space,
                        SymbolDisplayPartKind.FieldName,
                        SymbolDisplayPartKind.Space,
                        SymbolDisplayPartKind.Keyword,
                        SymbolDisplayPartKind.Space,
                        SymbolDisplayPartKind.Keyword,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation
                })
        End Sub

        <Fact()>
        Public Sub TestArrayRank()
            Dim text =
            <compilation>
                <file name="a.vb">
class C1 
    dim public f as Integer()(,)(,,)
end class
    </file>
            </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("C1").Single().
                                                                GetMembers("f").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "f As Integer()(,)(,,)",
                {
                    SymbolDisplayPartKind.FieldName,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation
                })
        End Sub


        <Fact()>
        Public Sub TestEscapeKeywordIdentifiers()
            Dim text =
            <compilation>
                <file name="a.vb">
namespace N1
class [Integer]
Class [class]
Shared Sub [shared]([boolean] As System.String)
    If [boolean] Then
        Console.WriteLine("true")
    Else 
        Console.WriteLine("false")
    End If
End Sub
End Class
End Class
end namespace

namespace [Global]
namespace [Integer]
end namespace
end namespace

    </file>
            </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                     GetTypeMembers("Integer").Single().
                                                                     GetTypeMembers("class").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeType,
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            ' no escaping because N1 does not need to be escaped
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "N1.Integer.class",
                {
                    SymbolDisplayPartKind.NamespaceName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.ClassName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.ClassName
                })

            format = New SymbolDisplayFormat(
                        memberOptions:=SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeParameters,
                        parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeType,
                        typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                        miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)


            ' outer class needs escaping
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "[Integer].class",
                {
                    SymbolDisplayPartKind.ClassName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.ClassName
                })

            format = New SymbolDisplayFormat(
            memberOptions:=SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeType,
            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            ' outer class needs escaping
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "[class]",
                {
                    SymbolDisplayPartKind.ClassName
                })


            findSymbol = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                            GetTypeMembers("Integer").Single().
                            GetTypeMembers("class").Single().GetMembers("shared").Single()

            format = New SymbolDisplayFormat(
                                memberOptions:=SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeParameters,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                                parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeType,
                                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            ' actual test case from bug 4389
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Sub [shared]([boolean] As System.String)",
                {
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.MethodName,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.ParameterName,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.NamespaceName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.ClassName,
                    SymbolDisplayPartKind.Punctuation
                })


            format = New SymbolDisplayFormat(
                                memberOptions:=SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeParameters,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                                parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeType,
                                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            ' making sure that types still get escaped if no special type formatting was chosen
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Sub [shared]([boolean] As [String])",
                {
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.MethodName,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.ParameterName,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.ClassName,
                    SymbolDisplayPartKind.Punctuation
                })

            format = New SymbolDisplayFormat(
                                memberOptions:=SymbolDisplayMemberOptions.IncludeType Or SymbolDisplayMemberOptions.IncludeAccessibility Or SymbolDisplayMemberOptions.IncludeParameters,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                                parameterOptions:=SymbolDisplayParameterOptions.IncludeName Or SymbolDisplayParameterOptions.IncludeType,
                                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers Or SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            ' making sure that types don't get escaped if special type formatting was chosen
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Sub [shared]([boolean] As String)",
                {
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.MethodName,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.ParameterName,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Punctuation
                })


            findSymbol = Function(globalns) globalns.LookupNestedNamespace({"Global"})

            format = New SymbolDisplayFormat(
                    typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                    miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            ' making sure that types don't get escaped if special type formatting was chosen
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "[Global]",
                {
                    SymbolDisplayPartKind.NamespaceName
                })

            format = New SymbolDisplayFormat(
                    globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                    typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            ' never escape "the" Global namespace, but escape other ns named "global" always
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Global.Global",
                {
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.NamespaceName
                })

            findSymbol = Function(globalns) globalns

            ' never escape "the" Global namespace, but escape other ns named "global" always
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Global",
                {
                    SymbolDisplayPartKind.Keyword
                })


            findSymbol = Function(globalns) globalns.LookupNestedNamespace({"Global"}).LookupNestedNamespace({"Integer"})

            ' never escape "the" Global namespace, but escape other ns named "global" always
            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Global.Global.Integer",
                {
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.NamespaceName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.NamespaceName
                })

            format = New SymbolDisplayFormat(
                    globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                    miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "[Global].Integer",
                {
                    SymbolDisplayPartKind.NamespaceName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.NamespaceName
                })

            format = New SymbolDisplayFormat(
                    globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Omitted,
                    typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameOnly,
                    miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "[Integer]",
                {
                    SymbolDisplayPartKind.NamespaceName
                })

        End Sub

        <Fact()>
        Public Sub AlwaysEscapeMethodNamedNew()
            Dim text =
<compilation>
    <file name="a.vb">
Class C
Sub [New]()
End Sub
End Class
    </file>
</compilation>

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeContainingType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            ' The method should be escaped
            TestSymbolDescription(
                text,
                Function(globalns As NamespaceSymbol) globalns.GetTypeMembers("C").Single().GetMembers("New").Single(),
                format,
                "C.[New]",
                {
                    SymbolDisplayPartKind.ClassName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.MethodName
                })

            ' The constructor should not
            TestSymbolDescription(
                text,
                Function(globalns As NamespaceSymbol) globalns.GetTypeMembers("C").Single().InstanceConstructors.Single(),
                format,
                "C.New",
                {
                    SymbolDisplayPartKind.ClassName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.Keyword
                })
        End Sub

        <Fact()>
        Public Sub TestExplicitMethodImplNameOnly()
            Dim text =
<compilation>
    <file name="a.vb">
Interface I
    sub M()
    End Sub
end Interface
        
Class C Implements I
    sub I_M() implements I.M
    End Sub
end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMembers("I_M").Single()

            Dim format = New SymbolDisplayFormat(memberOptions:=SymbolDisplayMemberOptions.None)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "I_M",
                {SymbolDisplayPartKind.MethodName})
        End Sub


        <Fact()>
        Public Sub TestExplicitMethodImplNameAndInterface()
            Dim text =
<compilation>
    <file name="a.vb">
Interface I
    sub M()
    End Sub
end Interface
        
Class C Implements I
    sub I_M() implements I.M
    End Sub
end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMembers("I_M").Single()

            Dim format = New SymbolDisplayFormat(memberOptions:=SymbolDisplayMemberOptions.IncludeExplicitInterface)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "I_M",
                {SymbolDisplayPartKind.MethodName})
        End Sub



        <Fact()>
        Public Sub TestExplicitMethodImplNameAndInterfaceAndType()
            Dim text =
<compilation>
    <file name="a.vb">
Interface I
    sub M()
    End Sub
end Interface
        
Class C Implements I
    sub I_M() implements I.M
    End Sub
end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                                                            GetMembers("I_M").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeExplicitInterface Or SymbolDisplayMemberOptions.IncludeContainingType)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C.I_M",
                {SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
               SymbolDisplayPartKind.MethodName})
        End Sub

        <Fact()>
        Public Sub TestGlobalNamespaceCode()
            Dim text =
<compilation>
    <file name="a.vb">
Class C
end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C")

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Global.C",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName})
        End Sub

        <Fact()>
        Public Sub TestGlobalNamespaceHumanReadable()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C
        End Class

        namespace [Global]
        end namespace
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Global",
                {SymbolDisplayPartKind.Keyword})

            format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Global",
                {SymbolDisplayPartKind.Keyword})
        End Sub

        <Fact()>
        Public Sub TestSpecialTypes()

            Dim text =
<compilation>
    <file name="a.vb">
        Class C
            dim f as Integer
        End Class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                                                                GetMembers("f").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "f As Integer",
                {
                    SymbolDisplayPartKind.FieldName,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword
                })
        End Sub

        <Fact()>
        Public Sub TestNoArrayAsterisks()
            Dim text =
            <compilation>
                <file name="a.vb">
class C1 
    dim public f as Integer()(,)(,,)
end class
    </file>
            </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("C1").Single().
                                                                GetMembers("f").Single()

            Dim format As SymbolDisplayFormat = New SymbolDisplayFormat(
                            memberOptions:=SymbolDisplayMemberOptions.IncludeType,
                            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.None)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "f As Int32()(,)(,,)",
                {
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation
                })
        End Sub

        <Fact()>
        Public Sub TestMetadataMethodNames()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C
            New C()
            End Sub
        End Class
    </file>
</compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                                                                GetMembers(".ctor").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                compilerInternalOptions:=SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Sub .ctor",
                {
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.MethodName})

        End Sub

        <Fact()>
        Public Sub TestArityForGenericTypes()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C(Of T, U, V)
        End Class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C")

            Dim format = New SymbolDisplayFormat(
            memberOptions:=SymbolDisplayMemberOptions.IncludeType,
            compilerInternalOptions:=SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C`3",
                {
                SymbolDisplayPartKind.ClassName,
                InternalSymbolDisplayPartKind.Arity})
        End Sub

        <Fact()>
        Public Sub TestGenericTypeParameters()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C(Of In T, Out U, V) 
        End Class
    </file>
</compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C")

            Dim format = New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C(Of T, U, V)",
                {
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestGenericTypeParametersAndVariance()
            Dim text =
<compilation>
    <file name="a.vb">
        Interface I(Of In T, Out U, V) 
        End Interface
    </file>
</compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("I")
            Dim format = New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "I(Of In T, Out U, V)",
                {
                SymbolDisplayPartKind.InterfaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestGenericTypeConstraints()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C(Of T As C(Of T))
        End Class
    </file>
</compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C")
            Dim format = New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeTypeConstraints)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C(Of T As C(Of T))",
                {
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestGenericMethodParameters()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C
            Public Sub M(Of In T, Out U, V)()
            End Sub
        End Class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(Of T, U, V)",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestGenericMethodParametersAndVariance()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C
            Public Sub M(Of In T, Out U, V)()
            End Sub
        End Class
    </file>
</compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M")
            Dim format = New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeVariance)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(Of T, U, V)",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestGenericMethodConstraints()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C(Of T)
            Public Sub M(Of U, V As {T, Class, U})()
            End Sub
        End Class
    </file>
</compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M")
            Dim format = New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters Or SymbolDisplayGenericsOptions.IncludeTypeConstraints)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(Of U, V As {Class, T, U})",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestMemberMethodNone()
            Dim text =
<compilation>
    <file name="a.vb">
        Class C 
            Sub M(p as Integer)
            End Sub
        End Class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.None)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M",
                {SymbolDisplayPartKind.MethodName})
        End Sub

        <Fact()>
        Public Sub TestMemberMethodAll()
            Dim text =
<compilation>
    <file name="a.vb">
                    Class C 
                        Sub M(p as Integer)
                        End Sub
                    End Class
                </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Sub C.M()", {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestMemberDeclareMethodAll()
            Dim text =
<compilation>
    <file name="a.vb">
Class C 
    Declare Unicode Sub M Lib "foo" (p as Integer) 
End Class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Declare Unicode Sub C.M Lib ""foo"" ()", {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StringLiteral,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestMemberDeclareMethod_NoType()
            Dim text =
<compilation>
    <file name="a.vb">
                    Class C 
                        Declare Unicode Sub M Lib "foo" (p as Integer) 
                    End Class
                </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public C.M()", {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestMemberDeclareMethod_NoAccessibility_NoContainingType_NoParameters()
            Dim text =
<compilation>
    <file name="a.vb">
                    Class C 
                        Declare Unicode Sub M Lib "foo" Alias "bar" (p as Integer) 
                    End Class
                </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Declare Unicode Sub M Lib ""foo"" Alias ""bar""", {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StringLiteral,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StringLiteral})
        End Sub

        <Fact()>
        Public Sub TestMemberDeclareMethodNone()
            Dim text =
<compilation>
    <file name="a.vb">
Class C 
    Declare Unicode Sub M Lib "foo" (p as Integer) 
End Class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.None)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M", {
                SymbolDisplayPartKind.MethodName})
        End Sub

        <Fact()>
        Public Sub TestMemberFieldNone()
            Dim text =
    <compilation>
        <file name="a.vb">
                    Class C 
                        dim f as Integer
                    End Class
                </file>
    </compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                                                                    GetMembers("f").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.None)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "f",
                {SymbolDisplayPartKind.FieldName})
        End Sub

        <Fact()>
        Public Sub TestMemberFieldAll()
            Dim text =
    <compilation>
        <file name="a.vb">
                    Class C 
                        dim f as Integer
                    End Class
                </file>
    </compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                                                                    GetMembers("f").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeAccessibility Or
                                SymbolDisplayMemberOptions.IncludeContainingType Or
                                SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                                SymbolDisplayMemberOptions.IncludeModifiers Or
                                SymbolDisplayMemberOptions.IncludeParameters Or
                                SymbolDisplayMemberOptions.IncludeType)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private C.f As Int32",
                {SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName
                })
        End Sub

        <Fact>
        Public Sub TestConstantFieldValue()
            Dim text =
<compilation>
    <file name="a.vb">
Class C
    Const f As Integer = 1
End Class
    </file>
</compilation>

            Dim findSymbol = Function(globalns As NamespaceSymbol) _
                                 globalns.GetTypeMembers("C", 0).Single().
                                 GetMembers("f").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType Or
                    SymbolDisplayMemberOptions.IncludeConstantValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private Const C.f As Int32 = 1",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral)
        End Sub

        <Fact>
        Public Sub TestConstantFieldValue_EnumMember()
            Dim text =
<compilation>
    <file name="a.vb">
Enum E
    A
    B
    C
End Enum
Class C
    Const f As E = E.B
End Class
    </file>
</compilation>

            Dim findSymbol = Function(globalns As NamespaceSymbol) _
                                 globalns.GetTypeMembers("C", 0).Single().
                                 GetMembers("f").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType Or
                    SymbolDisplayMemberOptions.IncludeConstantValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private Const C.f As E = E.B",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName)
        End Sub

        <Fact>
        Public Sub TestConstantFieldValue_EnumMember_Flags()
            Dim text =
<compilation>
    <file name="a.vb">
&lt;System.FlagsAttribute&gt;
Enum E
    A = 1
    B = 2
    C = 4
    D = A Or B Or C
End Enum
Class C
    Const f As E = E.D
End Class
    </file>
</compilation>

            Dim findSymbol = Function(globalns As NamespaceSymbol) _
                                 globalns.GetTypeMembers("C", 0).Single().
                                 GetMembers("f").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType Or
                    SymbolDisplayMemberOptions.IncludeConstantValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private Const C.f As E = E.D",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName)
        End Sub

        <Fact>
        Public Sub TestEnumMember()
            Dim text =
<compilation>
    <file name="a.vb">
Enum E
    A
    B
    C
End Enum
    </file>
</compilation>

            Dim findSymbol = Function(globalns As NamespaceSymbol) _
                                 globalns.GetTypeMembers("E", 0).Single().
                                 GetMembers("B").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType Or
                    SymbolDisplayMemberOptions.IncludeConstantValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "E.B = 1",
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral)
        End Sub

        <Fact>
        Public Sub TestEnumMember_Flags()
            Dim text =
<compilation>
    <file name="a.vb">
&lt;System.FlagsAttribute&gt;
Enum E
    A = 1
    B = 2
    C = 4
    D = A Or B Or C
End Enum
    </file>
</compilation>

            Dim findSymbol = Function(globalns As NamespaceSymbol) _
                                 globalns.GetTypeMembers("E", 0).Single().
                                 GetMembers("D").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType Or
                    SymbolDisplayMemberOptions.IncludeConstantValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "E.D = E.A Or E.B Or E.C",
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName)
        End Sub

        <Fact>
        Public Sub TestEnumMember_FlagsWithoutAttribute()
            Dim text =
<compilation>
    <file name="a.vb">
Enum E
    A = 1
    B = 2
    C = 4
    D = A Or B Or C
End Enum
    </file>
</compilation>

            Dim findSymbol = Function(globalns As NamespaceSymbol) _
                                 globalns.GetTypeMembers("E", 0).Single().
                                 GetMembers("D").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType Or
                    SymbolDisplayMemberOptions.IncludeConstantValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "E.D = 7",
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral)
        End Sub

        <Fact()>
        Public Sub TestMemberPropertyNone()
            Dim text =
    <compilation>
        <file name="c.vb">
                            Class C 
                                Private ReadOnly Property P As Integer
                                    Get
                                        Return 0
                                    End Get
                                End Property
                            End Class
                        </file>
    </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                                        GetMembers("P").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.None)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "P",
                {SymbolDisplayPartKind.PropertyName})
        End Sub

        <Fact()>
        Public Sub TestMemberPropertyAll()
            Dim text =
    <compilation>
        <file name="c.vb">
                            Class C 
                                Public Default Readonly Property P(x As Object) as Integer
                                    Get
                                        return 23
                                    End Get
                                End Property
                            End Class
                        </file>
    </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                                                    GetMembers("P").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeContainingType Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=
                                SymbolDisplayParameterOptions.IncludeType Or
                                SymbolDisplayParameterOptions.IncludeName)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Public Default Property C.P(x As Object) As Int32",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName
                })


        End Sub

        <Fact()>
        Public Sub TestMemberPropertyGetSet()
            Dim text =
    <compilation>
        <file name="c.vb">
            Class C 
                Public ReadOnly Property P as integer
                    Get
                        Return 0
                    End Get
                End Property
                Public WriteOnly Property Q
                    Set
                    End Set
                End Property
                Public Property R
            End Class
        </file>
    </compilation>

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                propertyStyle:=SymbolDisplayPropertyStyle.ShowReadWriteDescriptor)

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMembers("P").Single(),
                format,
                "ReadOnly Property P As Int32",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName
                })

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMembers("Q").Single(),
                format,
                "WriteOnly Property Q As Object",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName
                })

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMembers("R").Single(),
                format,
                "Property R As Object",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName
                })
        End Sub

        <Fact()>
        Public Sub TestPropertyGetAccessor()
            Dim text =
        <compilation>
            <file name="c.vb">
                            Class C 
                                Private Property P As Integer
                                    Get
                                        Return 0
                                    End Get
                                    Set
                                    End Set
                                End Property
                            End Class
                        </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                            GetMembers("get_P").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                                SymbolDisplayMemberOptions.IncludeAccessibility Or
                                SymbolDisplayMemberOptions.IncludeContainingType Or
                                SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                                SymbolDisplayMemberOptions.IncludeModifiers Or
                                SymbolDisplayMemberOptions.IncludeParameters Or
                                SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=
                                SymbolDisplayParameterOptions.IncludeType Or
                                SymbolDisplayParameterOptions.IncludeName)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private Property Get C.P() As Int32",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName
            })

            format = New SymbolDisplayFormat(
                            memberOptions:=
                                            SymbolDisplayMemberOptions.IncludeAccessibility Or
                                            SymbolDisplayMemberOptions.IncludeContainingType Or
                                            SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                                            SymbolDisplayMemberOptions.IncludeModifiers Or
                                            SymbolDisplayMemberOptions.IncludeParameters Or
                                            SymbolDisplayMemberOptions.IncludeType,
                            parameterOptions:=
                                            SymbolDisplayParameterOptions.IncludeType Or
                                            SymbolDisplayParameterOptions.IncludeName)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private C.P() As Int32",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName
            })
        End Sub

        <Fact()>
        Public Sub TestPropertySetAccessor()
            Dim text =
    <compilation>
        <file name="c.vb">
                            Class C 
                                Private WriteOnly Property P As Integer
                                    Set
                                    End Set
                                End Property
                            End Class
                        </file>
    </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                                                            GetMembers("set_P").Single()

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                                SymbolDisplayMemberOptions.IncludeAccessibility Or
                                SymbolDisplayMemberOptions.IncludeContainingType Or
                                SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                                SymbolDisplayMemberOptions.IncludeModifiers Or
                                SymbolDisplayMemberOptions.IncludeParameters Or
                                SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                parameterOptions:=
                                SymbolDisplayParameterOptions.IncludeType Or
                                SymbolDisplayParameterOptions.IncludeName)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Private Property Set C.P(Value As Int32)",
                {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation
                })
        End Sub

        <Fact()>
        Public Sub TestParameterMethodNone()
            Dim text =
    <compilation>
        <file name="a.vb">
                    Class C 
                        Sub M(obj as object, byref s as short, i as integer = 1)
                        End Sub
                    End Class
                </file>
    </compilation>
            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                        GetMember(Of MethodSymbol)("M")



            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:=SymbolDisplayParameterOptions.None)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M()",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestOptionalParameterBrackets()
            Dim text =
    <compilation>
        <file name="a.vb">
                    Class C 
                        Sub M(Optional i As Integer = 0)
                        End Sub
                    End Class
                </file>
    </compilation>

            Dim findSymbol =
                Function(globalns As NamespaceSymbol) globalns _
                    .GetMember(Of NamedTypeSymbol)("C") _
                    .GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:=
                    SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                    SymbolDisplayParameterOptions.IncludeType Or
                    SymbolDisplayParameterOptions.IncludeName Or
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M([i As Int32])",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestOptionalParameterValue_String()
            Dim text =
<compilation>
    <file name="a.vb">
Imports Microsoft.VisualBasic

Class C 
    Sub M(Optional s As String = ChrW(&amp;HFFFE) &amp; "a" &amp; ChrW(0) &amp; vbCrLf)
    End Sub
End Class
            </file>
</compilation>

            Dim findSymbol =
                Function(globalns As NamespaceSymbol) globalns _
                    .GetMember(Of NamedTypeSymbol)("C") _
                    .GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:=
                    SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                    SymbolDisplayParameterOptions.IncludeType Or
                    SymbolDisplayParameterOptions.IncludeName Or
                    SymbolDisplayParameterOptions.IncludeDefaultValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(s As String = ChrW(&HFFFE) & ""a"" & vbNullChar & vbCrLf)",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NumericLiteral,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StringLiteral,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <Fact()>
        Public Sub TestOptionalParameterValue_Char()
            Dim text =
<compilation>
    <file name="a.vb">
Imports Microsoft.VisualBasic

Class C 
    Sub M(Optional a As Char = ChrW(&amp;HFFFE), Optional b As Char = ChrW(8))
    End Sub
End Class
            </file>
</compilation>

            Dim findSymbol =
                Function(globalns As NamespaceSymbol) globalns _
                    .GetMember(Of NamedTypeSymbol)("C") _
                    .GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:=
                    SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                    SymbolDisplayParameterOptions.IncludeType Or
                    SymbolDisplayParameterOptions.IncludeName Or
                    SymbolDisplayParameterOptions.IncludeDefaultValue)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(a As Char = ChrW(&HFFFE), b As Char = vbBack)",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NumericLiteral,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation
                })
        End Sub

        <Fact()>
        Public Sub TestOptionalParameterValue_InvariantCulture1()
            Dim text =
<compilation>
    <file name="a.vb">
Class C 
    Sub M(
        Optional p1 as SByte = -1, 
        Optional p2 as Short = -1, 
        Optional p3 as Integer = -1,
        Optional p4 as Long = -1,
        Optional p5 as Single = -0.5,
        Optional p6 as Double = -0.5,
        Optional p7 as Decimal = -0.5)
    End Sub
End Class
            </file>
</compilation>

            Dim oldCulture = Thread.CurrentThread.CurrentCulture
            Try
                Thread.CurrentThread.CurrentCulture = CType(oldCulture.Clone(), CultureInfo)
                Thread.CurrentThread.CurrentCulture.NumberFormat.NegativeSign = "~"
                Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ","

                Dim Compilation = CreateCompilationWithMscorlib(text)
                Compilation.VerifyDiagnostics()

                Dim methodSymbol = Compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M")
                Assert.Equal("Sub C.M(" +
                    "[p1 As System.SByte = -1], " +
                    "[p2 As System.Int16 = -1], " +
                    "[p3 As System.Int32 = -1], " +
                    "[p4 As System.Int64 = -1], " +
                    "[p5 As System.Single = -0.5], " +
                    "[p6 As System.Double = -0.5], " +
                    "[p7 As System.Decimal = -0.5])", methodSymbol.ToTestDisplayString())
            Finally
                Thread.CurrentThread.CurrentCulture = oldCulture
            End Try
        End Sub

        <Fact()>
        Public Sub TestOptionalParameterValue_InvariantCulture()
            Dim text =
<compilation>
    <file name="a.vb">
Class C 
    Sub M(
        Optional p1 as SByte = -1, 
        Optional p2 as Short = -1, 
        Optional p3 as Integer = -1,
        Optional p4 as Long = -1,
        Optional p5 as Single = -0.5,
        Optional p6 as Double = -0.5,
        Optional p7 as Decimal = -0.5)
    End Sub
End Class
            </file>
</compilation>

            Dim oldCulture = Thread.CurrentThread.CurrentCulture
            Try
                Thread.CurrentThread.CurrentCulture = CType(oldCulture.Clone(), CultureInfo)
                Thread.CurrentThread.CurrentCulture.NumberFormat.NegativeSign = "~"
                Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ","

                Dim Compilation = CreateCompilationWithMscorlib(text)
                Compilation.VerifyDiagnostics()

                Dim methodSymbol = Compilation.GlobalNamespace.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M")
                Assert.Equal("Sub C.M(" +
                    "[p1 As System.SByte = -1], " +
                    "[p2 As System.Int16 = -1], " +
                    "[p3 As System.Int32 = -1], " +
                    "[p4 As System.Int64 = -1], " +
                    "[p5 As System.Single = -0.5], " +
                    "[p6 As System.Double = -0.5], " +
                    "[p7 As System.Decimal = -0.5])", methodSymbol.ToTestDisplayString())
            Finally
                Thread.CurrentThread.CurrentCulture = oldCulture
            End Try
        End Sub

        <Fact()>
        Public Sub TestMethodReturnType1()
            Dim text =
    <compilation>
        <file name="a.vb">
                    Class C 
                        shared function M() as Integer
                        End Sub
                    End Class
                </file>
    </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Function M() As System.Int32",
                {
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.MethodName,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Punctuation,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.Keyword,
                    SymbolDisplayPartKind.Space,
                    SymbolDisplayPartKind.NamespaceName,
                    SymbolDisplayPartKind.Operator,
                    SymbolDisplayPartKind.StructName
                })
        End Sub

        <Fact()>
        Public Sub TestMethodReturnType2()
            Dim text =
        <compilation>
            <file name="a.vb">
                    Class C 
                        shared sub M()
                        End Sub
                    End Class
                </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").
                GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Sub M()",
                    {
                        SymbolDisplayPartKind.Keyword,
                        SymbolDisplayPartKind.Space,
                        SymbolDisplayPartKind.MethodName,
                        SymbolDisplayPartKind.Punctuation,
                        SymbolDisplayPartKind.Punctuation
                    })
        End Sub

        <Fact()>
        Public Sub TestParameterMethodNameTypeModifiers()
            Dim text =
        <compilation>
            <file name="a.vb">
                    Class C 
                        Public Sub M(byref s as short, i as integer , ParamArray args as string())
                        End Sub
                    End Class
                </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M")

            Dim format = New SymbolDisplayFormat(
                memberOptions:=SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:=
                    SymbolDisplayParameterOptions.IncludeParamsRefOut Or
                    SymbolDisplayParameterOptions.IncludeType Or
                    SymbolDisplayParameterOptions.IncludeName)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(ByRef s As Int16, i As Int32, ParamArray args As String())",
                {
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        ' "Public" and "MustOverride" should not be included for interface members.
        <Fact()>
        Public Sub TestInterfaceMembers()
            Dim text =
<compilation>
    <file name="a.vb">
    Interface I
        Property P As Integer
        Function F() As Object
    End Interface
    MustInherit Class C
        MustOverride Function F() As Object
        Interface I
            Sub M()
        End Interface
    End Class
    </file>
</compilation>

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                propertyStyle:=
                    SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                miscellaneousOptions:=
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("I", 0).Single().GetMembers("P").Single(),
                format,
                "Property P As Integer")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("I", 0).Single().GetMembers("F").Single(),
                format,
                "Function F() As Object")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMembers("F").Single(),
                format,
                "Public MustOverride Function F() As Object")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetTypeMembers("I", 0).Single().GetMember(Of MethodSymbol)("M"),
                format,
                "Sub M()")
        End Sub

        ' "Shared" should not be included for Module members.
        <Fact()>
        Public Sub TestSharedMembers()
            Dim text =
<compilation>
    <file name="a.vb">
    Class C
        Shared Sub M()
        End Sub
        Public Shared F As Integer
        Public Shared P As Object
    End Class
    Module M
        Sub M()
        End Sub
        Public F As Integer
        Public P As Object
    End Module
    </file>
</compilation>

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                miscellaneousOptions:=
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMember(Of MethodSymbol)("M"),
                format,
                "Public Shared Sub M()")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMembers("F").Single(),
                format,
                "Public Shared F As Integer")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMembers("P").Single(),
                format,
                "Public Shared P As Object")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("M", 0).Single().GetMember(Of MethodSymbol)("M"),
                format,
                "Public Sub M()")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("M", 0).Single().GetMembers("F").Single(),
                format,
                "Public F As Integer")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("M", 0).Single().GetMembers("P").Single(),
                format,
                "Public P As Object")
        End Sub

        <WorkItem(540253, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540253")>
        <Fact()>
        Public Sub TestOverloads()
            Dim text =
<compilation>
    <file name="a.vb">
    MustInherit Class B
        Protected MustOverride Overloads Function M(s As Single)
        Overloads Sub M()
        End Sub
        Friend NotOverridable Overloads WriteOnly Property P(x)
            Set(value)
            End Set
        End Property
        Overloads ReadOnly Property P(x, y)
            Get
                Return Nothing
            End Get
        End Property
        Public Overridable Overloads ReadOnly Property Q
            Get
                Return Nothing
            End Get
        End Property
    End Class
    </file>
</compilation>

            Dim format = New SymbolDisplayFormat(
                memberOptions:=
                    SymbolDisplayMemberOptions.IncludeAccessibility Or
                    SymbolDisplayMemberOptions.IncludeExplicitInterface Or
                    SymbolDisplayMemberOptions.IncludeModifiers Or
                    SymbolDisplayMemberOptions.IncludeParameters Or
                    SymbolDisplayMemberOptions.IncludeType,
                kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                propertyStyle:=
                    SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                parameterOptions:=
                    SymbolDisplayParameterOptions.IncludeName Or
                    SymbolDisplayParameterOptions.IncludeType)

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("B", 0).Single().GetMembers("M").First(),
                format,
                "Protected MustOverride Overloads Function M(s As Single) As Object")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("B", 0).Single().GetMembers("P").First(),
                format,
                "Friend NotOverridable Overloads WriteOnly Property P(x As Object) As Object")

            TestSymbolDescription(
                text,
                Function(globalns) globalns.GetTypeMembers("B", 0).Single().GetMembers("Q").First(),
                format,
                "Public Overridable Overloads ReadOnly Property Q As Object")
        End Sub

        <Fact>
        Public Sub TestAlias1()
            Dim text =
        <compilation>
            <file name="a.vb">Imports Foo=N1.N2.N3
            Namespace N1
                NAmespace N2
                    NAmespace N3
                        class C1 
                            class C2
                            End class
                        End class
                    ENd namespace
                end namespace
            end namespace
                </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                            LookupNestedNamespace({"N2"}).
                                                                            LookupNestedNamespace({"N3"}).
                                                                            GetTypeMembers("C1").Single().
                                                                            GetTypeMembers("C2").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Foo.C1.C2",
                code.IndexOf("Namespace", StringComparison.Ordinal),
                {
                SymbolDisplayPartKind.AliasName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName}, True)
        End Sub

        <Fact>
        Public Sub TestAlias2()
            Dim text =
        <compilation>
            <file name="a.vb">Imports Foo=N1.N2.N3.C1
            Namespace N1
                NAmespace N2
                    NAmespace N3
                        class C1 
                            class C2
                            End class
                        End class
                    ENd namespace
                end namespace
            end namespace
                </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                            LookupNestedNamespace({"N2"}).
                                                                            LookupNestedNamespace({"N3"}).
                                                                            GetTypeMembers("C1").Single().
                                                                            GetTypeMembers("C2").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            Dim format = New SymbolDisplayFormat(
                typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Foo.C2",
                code.IndexOf("Namespace", StringComparison.Ordinal),
                {
                SymbolDisplayPartKind.AliasName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName}, True)
        End Sub

        <Fact>
        Public Sub TestAlias3()
            Dim text =
        <compilation>
            <file name="a.vb">Imports Foo = N1.C1
            Namespace N1
                Class C1
                End Class
                Class Foo
                End Class
            end namespace
                </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.LookupNestedNamespace({"N1"}).
                                                                            GetTypeMembers("C1").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            Dim format = SymbolDisplayFormat.MinimallyQualifiedFormat

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C1",
                code.IndexOf("Class Foo", StringComparison.Ordinal),
                {
                SymbolDisplayPartKind.ClassName}, True)
        End Sub

        <Fact>
        Public Sub TestMinimalNamespace1()
            Dim text =
        <compilation>
            <file name="a.vb">
Imports Microsoft
Imports OUTER

namespace N0
end namespace
            namespace N1 
    namespace N2
        namespace N3 
            class C1 
                class C2
                end class
            end class
        end namespace
    end namespace
end namespace

Module Program
    Sub Main(args As String())
        Dim x As Microsoft.VisualBasic.Collection

        Dim y As OUTER.INNER.FOO
    End Sub
End Module

Namespace OUTER
    Namespace INNER
        Friend Class FOO
        End Class
    End Namespace
End Namespace

    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                     Return globalns.LookupNestedNamespace({"N1"}).
                                                                     LookupNestedNamespace({"N2"}).
                                                                     LookupNestedNamespace({"N3"})
                                                                 End Function

            Dim format = New SymbolDisplayFormat()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, format,
                "N1.N2.N3",
                code.IndexOf("N0", StringComparison.Ordinal), {
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName}, True)

            TestSymbolDescription(text, findSymbol, format,
                "N1.N2.N3",
                text.Value.IndexOf("N1", StringComparison.Ordinal), {
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName}, True)

            TestSymbolDescription(text, findSymbol, format,
                "N2.N3",
                text.Value.IndexOf("N2", StringComparison.Ordinal), {
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.NamespaceName}, True)

            TestSymbolDescription(text, findSymbol, format,
                "N3",
                text.Value.IndexOf("C1", StringComparison.Ordinal),
                {SymbolDisplayPartKind.NamespaceName}, True)

            TestSymbolDescription(text, findSymbol, format,
                "N3",
                text.Value.IndexOf("C2", StringComparison.Ordinal),
                {SymbolDisplayPartKind.NamespaceName}, True)


            Dim findFOO As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                  Return globalns.LookupNestedNamespace({"OUTER"}).
                                                                  LookupNestedNamespace({"INNER"}).GetTypeMembers("Foo").Single()
                                                              End Function

            TestSymbolDescription(text, findFOO, format,
                "INNER.FOO",
                text.Value.IndexOf("OUTER.INNER.FOO", StringComparison.Ordinal),
                {SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName}, True)

            Dim findCollection As Func(Of NamespaceSymbol, Symbol) = Function(globalns)
                                                                         Return globalns.LookupNestedNamespace({"Microsoft"}).
                                                                         LookupNestedNamespace({"VisualBasic"}).GetTypeMembers("Collection").Single()
                                                                     End Function

            TestSymbolDescription(text, findCollection, format,
                "VisualBasic.Collection",
                text.Value.IndexOf("Microsoft.VisualBasic.Collection", StringComparison.Ordinal),
                {SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName}, minimal:=True, references:={SystemRef, MsvbRef})
        End Sub

        <Fact>
        Public Sub TestMinimalClass1()
            Dim text =
        <compilation>
            <file name="a.vb">
imports System.Collections.Generic
class C1 
    Dim Private foo as System.Collections.Generic.IDictionary(Of System.Collections.Generic.IList(Of System.Int32), System.String)
end class

    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("C1").Single().
                                                                GetMembers("foo").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, Nothing,
                "C1.foo As IDictionary(Of IList(Of Integer), String)",
                code.IndexOf("foo", StringComparison.Ordinal), {
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.InterfaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.InterfaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation}, minimal:=True)
        End Sub

        <Fact()>
        Public Sub TestRemoveAttributeSuffix1()
            Dim text =
        <compilation>
            <file name="a.vb">
Class Class1Attribute
    Inherits System.Attribute
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("Class1Attribute").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, New SymbolDisplayFormat(),
                "Class1Attribute",
                SymbolDisplayPartKind.ClassName)

            TestSymbolDescription(text, findSymbol, New SymbolDisplayFormat(miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix),
                "Class1",
                code.IndexOf("Inherits System.Attribute", StringComparison.Ordinal), {
                SymbolDisplayPartKind.ClassName}, minimal:=True)
        End Sub

        <Fact>
        Public Sub TestRemoveAttributeSuffix2()
            Dim text =
        <compilation>
            <file name="a.vb">
Class ClassAttribute
    Inherits System.Attribute
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("ClassAttribute").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, New SymbolDisplayFormat(),
                "ClassAttribute",
                SymbolDisplayPartKind.ClassName)

            TestSymbolDescription(text, findSymbol,
                New SymbolDisplayFormat(miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix),
                "ClassAttribute",
                SymbolDisplayPartKind.ClassName)
        End Sub

        <Fact>
        Public Sub TestRemoveAttributeSuffix3()
            Dim text =
        <compilation>
            <file name="a.vb">
Class _Attribute
    Inherits System.Attribute
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("_Attribute").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, New SymbolDisplayFormat(),
                "_Attribute",
                SymbolDisplayPartKind.ClassName)

            TestSymbolDescription(text, findSymbol,
                New SymbolDisplayFormat(miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix),
                "_Attribute",
                SymbolDisplayPartKind.ClassName)
        End Sub

        <Fact>
        Public Sub TestRemoveAttributeSuffix4()
            Dim text =
        <compilation>
            <file name="a.vb">
Class Class1Attribute
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("Class1Attribute").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, New SymbolDisplayFormat(),
                "Class1Attribute",
                SymbolDisplayPartKind.ClassName)

            TestSymbolDescription(text, findSymbol,
                New SymbolDisplayFormat(miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix),
                "Class1Attribute",
                SymbolDisplayPartKind.ClassName)
        End Sub

        <WorkItem(537447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537447")>
        <Fact>
        Public Sub TestBug2239()
            Dim text =
        <compilation>
            <file name="a.vb">Imports Foo=N1.N2.N3
class GC1(Of T) 
end class

class X 
    inherits GC1(Of BOGUS)
End class

                </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("X").Single.BaseType

            Dim format = New SymbolDisplayFormat(
                genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "GC1(Of BOGUS)",
                {
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ErrorTypeName,
                SymbolDisplayPartKind.Punctuation})
        End Sub

        <WorkItem(538954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538954")>
        <Fact>
        Public Sub ParameterOptionsIncludeName()
            Dim text =
        <compilation>
            <file name="a.vb">
Class Class1 
    Sub Sub1(ByVal param1 As Integer)
    End Sub
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns)
                    Dim sub1 = CType(globalns.GetTypeMembers("Class1").Single().GetMembers("Sub1").Single(), MethodSymbol)
                    Return sub1.Parameters.Single()
                End Function

            Dim format = New SymbolDisplayFormat(parameterOptions:=SymbolDisplayParameterOptions.IncludeName)

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "param1",
                {SymbolDisplayPartKind.ParameterName})
        End Sub

        <WorkItem(539076, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539076")>
        <Fact>
        Public Sub Bug4878()
            Dim text =
        <compilation>
            <file name="a.vb">
Namespace Global
    Namespace Global ' invalid because nested, would need escaping
        Public Class c1
        End Class
    End Namespace
End Namespace
    </file>
        </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            Assert.Equal("[Global]", comp.SourceModule.GlobalNamespace.GetMembers().Single().ToDisplayString())

            Dim format = New SymbolDisplayFormat(
                    globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                    typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces)
            Assert.Equal("Global.Global", comp.SourceModule.GlobalNamespace.GetMembers().Single().ToDisplayString(format))

            Assert.Equal("Global.Global.c1", comp.SourceModule.GlobalNamespace.LookupNestedNamespace({"Global"}).GetTypeMembers.Single().ToDisplayString(format))
        End Sub

        <WorkItem(541005, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541005")>
        <Fact>
        Public Sub Bug7515()
            Dim text =
        <compilation>
            <file name="a.vb">
        Public Class C1
            Delegate Sub MyDel(x as MyDel)
        End Class
    </file>
        </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)
            Dim m_DelegateSignatureFormat As New SymbolDisplayFormat(
                                                            globalNamespaceStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GlobalNamespaceStyle,
                                                            typeQualificationStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.TypeQualificationStyle,
                                                            genericsOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.GenericsOptions,
                                                            memberOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MemberOptions,
                                                            parameterOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.ParameterOptions,
                                                            propertyStyle:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.PropertyStyle,
                                                            localOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.LocalOptions,
                                                            kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword Or SymbolDisplayKindOptions.IncludeNamespaceKeyword Or SymbolDisplayKindOptions.IncludeTypeKeyword,
                                                            delegateStyle:=SymbolDisplayDelegateStyle.NameAndSignature,
                                                            miscellaneousOptions:=SymbolDisplayFormat.VisualBasicErrorMessageFormat.MiscellaneousOptions)
            Assert.Equal("Delegate Sub C1.MyDel(x As C1.MyDel)", comp.SourceModule.GlobalNamespace.GetTypeMembers("C1").Single().
                                                                GetMembers("MyDel").Single().ToDisplayString(m_DelegateSignatureFormat))
        End Sub

        <WorkItem(542619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542619")>
        <Fact>
        Public Sub Bug9913()
            Dim text =
        <compilation>
            <file name="a.vb">
Public Class Test
    Public Class System
        Public Class Action
        End Class
    End Class
    Public field As Global.System.Action
    Public field2 As System.Action
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns)
                    Dim field = CType(globalns.GetTypeMembers("Test").Single().GetMembers("field").Single(), FieldSymbol)
                    Return field.Type
                End Function

            Dim format = New SymbolDisplayFormat(
                            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                            typeQualificationStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.TypeQualificationStyle,
                            genericsOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions,
                            memberOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions,
                            delegateStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.DelegateStyle,
                            extensionMethodStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.ExtensionMethodStyle,
                            parameterOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.ParameterOptions,
                            propertyStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.PropertyStyle,
                            localOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.LocalOptions,
                            kindOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.KindOptions,
                            miscellaneousOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions)

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Global.System.Action",
                code.IndexOf("Global.System.Action", StringComparison.Ordinal),
                {SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.DelegateName},
                minimal:=True)
        End Sub

        <WorkItem(542619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542619")>
        <Fact>
        Public Sub Bug9913_2()
            Dim text =
        <compilation>
            <file name="a.vb">
Public Class Test
    Public Class System
        Public Class Action
        End Class
    End Class
    Public field As Global.System.Action
    Public field2 As System.Action
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns)
                    Dim field = CType(globalns.GetTypeMembers("Test").Single().GetMembers("field").Single(), FieldSymbol)
                    Return field.Type
                End Function

            Dim format = New SymbolDisplayFormat(
                            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                            typeQualificationStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.TypeQualificationStyle,
                            genericsOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions,
                            memberOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions,
                            delegateStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.DelegateStyle,
                            extensionMethodStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.ExtensionMethodStyle,
                            parameterOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.ParameterOptions,
                            propertyStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.PropertyStyle,
                            localOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.LocalOptions,
                            kindOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.KindOptions,
                            miscellaneousOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions)

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "System.Action",
                code.IndexOf("Global.System.Action", StringComparison.Ordinal),
                {SymbolDisplayPartKind.NamespaceName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.DelegateName},
                minimal:=True)
        End Sub

        <WorkItem(542619, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542619")>
        <Fact>
        Public Sub Bug9913_3()
            Dim text =
        <compilation>
            <file name="a.vb">
Public Class Test
    Public Class System
        Public Class Action
        End Class
    End Class
    Public field2 As System.Action
    Public field As Global.System.Action
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns)
                    Dim field = CType(globalns.GetTypeMembers("Test").Single().GetMembers("field2").Single(), FieldSymbol)
                    Return field.Type
                End Function

            Dim format = New SymbolDisplayFormat(
                            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                            typeQualificationStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.TypeQualificationStyle,
                            genericsOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions,
                            memberOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions,
                            delegateStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.DelegateStyle,
                            extensionMethodStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.ExtensionMethodStyle,
                            parameterOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.ParameterOptions,
                            propertyStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.PropertyStyle,
                            localOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.LocalOptions,
                            kindOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.KindOptions,
                            miscellaneousOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions)

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "System.Action",
                code.IndexOf("System.Action", StringComparison.Ordinal),
                {SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.ClassName},
                minimal:=True)
        End Sub

        <Fact()>
        Public Sub TestMinimalOfContextualKeywordAsIdentifier()
            Dim text =
        <compilation>
            <file name="a.vb">
Class Take
    Class X
        Public Shared Sub Foo
        End Sub
    End Class
End Class
 
Class Z(Of T)
    Inherits Take
End Class
 
Module M
    Sub Main()
        Dim x = From y In ""
        Z(Of Integer).X.Foo ' Simplify Z(Of Integer).X
    End Sub
End Module


    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("Take").Single().
                                                                GetTypeMembers("X").Single().
                                                                GetMembers("Foo").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, Nothing,
                "Sub [Take].X.Foo()",
                code.IndexOf("Z(Of Integer).X.Foo", StringComparison.Ordinal), {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation}, minimal:=True)
        End Sub

        <Fact()>
        Public Sub TestMinimalOfContextualKeywordAsIdentifierTypeKeyword()
            Dim text =
        <compilation>
            <file name="a.vb">
Class [Type]
    Class X
        Public Shared Sub Foo
        End Sub
    End Class
End Class
 
Class Z(Of T)
    Inherits [Type]
End Class
 
Module M
    Sub Main()
        Dim x = From y In ""
        Z(Of Integer).X.Foo ' Simplify Z(Of Integer).X
    End Sub
End Module


    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("Type").Single().
                                                                GetTypeMembers("X").Single().
                                                                GetMembers("Foo").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, Nothing,
                "Sub Type.X.Foo()",
                code.IndexOf("Z(Of Integer).X.Foo", StringComparison.Ordinal), {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation}, minimal:=True)

            text =
                    <compilation>
                        <file name="a.vb">
Imports System
Class Foo
    Public Bar as Type
End Class
    </file>
                    </compilation>

            findSymbol = Function(globalns) globalns.GetTypeMembers("Foo").Single().GetMembers("Bar").Single()

            code = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, Nothing,
                "Foo.Bar As Type",
                code.IndexOf("Public Bar as Type", StringComparison.Ordinal), {
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName}, minimal:=True)
        End Sub

        <WorkItem(543938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543938")>
        <Fact>
        Public Sub Bug12025()
            Dim text =
        <compilation>
            <file name="a.vb">
Class CBase    
    Public Overridable Property [Class] As Integer
End Class
    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns)
                    Dim field = CType(globalns.GetTypeMembers("CBase").Single().GetMembers("Class").Single(), PropertySymbol)
                    Return field
                End Function

            Dim format = New SymbolDisplayFormat(
                            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                            typeQualificationStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.TypeQualificationStyle,
                            genericsOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions,
                            memberOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions,
                            delegateStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.DelegateStyle,
                            extensionMethodStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.ExtensionMethodStyle,
                            parameterOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.ParameterOptions,
                            propertyStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.PropertyStyle,
                            localOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.LocalOptions,
                            kindOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.KindOptions,
                            miscellaneousOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions)

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Property CBase.Class As Integer",
                code.IndexOf("Public Overridable Property [Class] As Integer", StringComparison.Ordinal),
                {SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.PropertyName,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword},
                minimal:=True)
        End Sub

        <Fact, WorkItem(544414, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544414")>
        Public Sub Bug12724()
            Dim text =
        <compilation>
            <file name="a.vb">
Class CBase    
    Public Overridable Property [Class] As Integer
    Public [Interface] As Integer
    Event [Event]()
    Public Overridable Sub [Sub]()
    Public Overridable Function [Function]()
    Class [Dim]
    End Class
End Class
    </file>
        </compilation>

            Dim findProperty As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns)
                    Dim field = CType(globalns.GetTypeMembers("CBase").Single().GetMembers("Class").Single(), PropertySymbol)
                    Return field
                End Function

            Dim format = New SymbolDisplayFormat(
                            globalNamespaceStyle:=SymbolDisplayGlobalNamespaceStyle.Included,
                            typeQualificationStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.TypeQualificationStyle,
                            genericsOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions,
                            memberOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions,
                            delegateStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.DelegateStyle,
                            extensionMethodStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.ExtensionMethodStyle,
                            parameterOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.ParameterOptions,
                            propertyStyle:=SymbolDisplayFormat.MinimallyQualifiedFormat.PropertyStyle,
                            localOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.LocalOptions,
                            kindOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.KindOptions,
                            miscellaneousOptions:=SymbolDisplayFormat.MinimallyQualifiedFormat.MiscellaneousOptions)

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(
                text,
                findProperty,
                format,
                "Property CBase.Class As Integer",
                code.IndexOf("Public Overridable Property [Class] As Integer", StringComparison.Ordinal),
                {SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.PropertyName,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword},
                minimal:=True)

            Dim findSub As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns) CType(globalns.GetTypeMembers("CBase").Single().GetMembers("Sub").Single(), MethodSymbol)

            TestSymbolDescription(
                text,
                findSub,
                format,
                "Sub CBase.Sub()",
                code.IndexOf("Public Overridable Sub [Sub]()", StringComparison.Ordinal),
                {SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.MethodName,
                 SymbolDisplayPartKind.Punctuation,
                 SymbolDisplayPartKind.Punctuation},
                minimal:=True)

            Dim findFunction As Func(Of NamespaceSymbol, Symbol) =
             Function(globalns) CType(globalns.GetTypeMembers("CBase").Single().GetMembers("Function").Single(), MethodSymbol)

            TestSymbolDescription(
                text,
                findFunction,
                format,
                "Function CBase.Function() As Object",
                code.IndexOf("Public Overridable Function [Function]()", StringComparison.Ordinal),
                {SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.MethodName,
                 SymbolDisplayPartKind.Punctuation,
                 SymbolDisplayPartKind.Punctuation,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword},
                minimal:=True)

            Dim findField As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns) CType(globalns.GetTypeMembers("CBase").Single().GetMembers("Interface").Single(), FieldSymbol)

            TestSymbolDescription(
                text,
                findField,
                format,
                "CBase.Interface As Integer",
                code.IndexOf("Public [Interface] As Integer", StringComparison.Ordinal),
                {SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.FieldName,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.Keyword},
                minimal:=True)

            Dim findEvent As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns) CType(globalns.GetTypeMembers("CBase").Single().GetMembers("Event").Single(), EventSymbol)

            TestSymbolDescription(
                text,
                findEvent,
                format,
                "Event CBase.Event()",
                code.IndexOf("Event [Event]()", StringComparison.Ordinal),
                {SymbolDisplayPartKind.Keyword,
                 SymbolDisplayPartKind.Space,
                 SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.EventName,
                 SymbolDisplayPartKind.Punctuation,
                 SymbolDisplayPartKind.Punctuation},
                minimal:=True)

            Dim findClass As Func(Of NamespaceSymbol, Symbol) =
                Function(globalns) CType(globalns.GetTypeMembers("CBase").Single().GetMembers("Dim").Single(), NamedTypeSymbol)

            TestSymbolDescription(
                text,
                findClass,
                New SymbolDisplayFormat(typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces),
                "CBase.Dim",
                code.IndexOf("Class [Dim]", StringComparison.Ordinal),
                {SymbolDisplayPartKind.ClassName,
                 SymbolDisplayPartKind.Operator,
                 SymbolDisplayPartKind.ClassName},
                minimal:=False)
        End Sub

        <Fact, WorkItem(543806, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543806")>
        Public Sub Bug11752()
            Dim text =
        <compilation>
            <file name="a.vb">
Class Explicit
    Class X
        Public Shared Sub Foo
        End Sub
    End Class
End Class
 
Class Z(Of T)
    Inherits Take
End Class
 
Module M
    Sub Main()
        Dim x = From y In ""
        Z(Of Integer).X.Foo ' Simplify Z(Of Integer).X
    End Sub
End Module


    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("Explicit").Single().
                                                                GetTypeMembers("X").Single().
                                                                GetMembers("Foo").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, Nothing,
                "Sub Explicit.X.Foo()",
                code.IndexOf("Z(Of Integer).X.Foo", StringComparison.Ordinal), {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation}, minimal:=True)
        End Sub

        <Fact(), WorkItem(529764, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529764")>
        Public Sub TypeParameterFromMetadata()
            Dim src1 =
        <compilation>
            <file name="lib.vb">
Public Class LibG(Of T)
End Class
    </file>
        </compilation>

            Dim src2 =
       <compilation>
           <file name="use.vb">
Public Class Gen(Of V)
    Public Sub M(p as LibG(Of V))
    End Sub

    Public Function F(p as Object) As Object
    End Function
End Class
    </file>
       </compilation>

            Dim dummy =
      <compilation>
          <file name="app.vb">
          </file>
      </compilation>

            Dim complib = CreateCompilationWithMscorlib(src1)
            Dim compref = New VisualBasicCompilationReference(complib)

            Dim comp1 = CreateCompilationWithMscorlibAndReferences(src2, references:={compref})

            Dim mtdata = comp1.EmitToArray()
            Dim mtref = MetadataReference.CreateFromImage(mtdata)
            Dim comp2 = CreateCompilationWithMscorlibAndReferences(dummy, references:={mtref})

            Dim tsym1 = comp1.SourceModule.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Gen")
            Assert.NotNull(tsym1)
            Dim msym1 = tsym1.GetMember(Of MethodSymbol)("M")
            Assert.NotNull(msym1)
            ' Public Sub M(p As LibG(Of V))
            ' C# is like - Gen(Of V).M(LibG(Of V))
            Assert.Equal("Public Sub M(p As LibG(Of V))", msym1.ToDisplayString())

            Dim tsym2 = comp2.GlobalNamespace.GetMember(Of NamedTypeSymbol)("Gen")
            Assert.NotNull(tsym2)
            Dim msym2 = tsym2.GetMember(Of MethodSymbol)("M")
            Assert.NotNull(msym2)
            Assert.Equal(msym1.ToDisplayString(), msym2.ToDisplayString())

        End Sub

        <Fact, WorkItem(545625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545625")>
        Public Sub ReverseArrayRankSpecifiers()
            Dim text =
<compilation>
    <file name="a.vb">
class C
    Private F as C()(,)
end class
    </file>
</compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetMember(Of NamedTypeSymbol)("C").GetMember(Of FieldSymbol)("F").Type

            Dim normalFormat As New SymbolDisplayFormat()
            Dim reverseFormat As New SymbolDisplayFormat(
                compilerInternalOptions:=SymbolDisplayCompilerInternalOptions.ReverseArrayRankSpecifiers)

            TestSymbolDescription(
                text,
                findSymbol,
                normalFormat,
                "C()(,)",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation)

            TestSymbolDescription(
                text,
                findSymbol,
                reverseFormat,
                "C(,)()",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation)
        End Sub

        <Fact>
        Public Sub TestMethodCSharp()
            Dim text =
<text>
class A
{
    public void Foo(int a)
    {
    }
}
</text>.Value

            Dim format = New SymbolDisplayFormat(memberOptions:=SymbolDisplayMemberOptions.IncludeParameters Or
                                                                SymbolDisplayMemberOptions.IncludeModifiers Or
                                                                SymbolDisplayMemberOptions.IncludeAccessibility Or
                                                                SymbolDisplayMemberOptions.IncludeType,
                                                 kindOptions:=SymbolDisplayKindOptions.IncludeMemberKeyword,
                                                 parameterOptions:=SymbolDisplayParameterOptions.IncludeType Or
                                                                   SymbolDisplayParameterOptions.IncludeName Or
                                                                   SymbolDisplayParameterOptions.IncludeDefaultValue,
                                                 miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

            Dim comp = CreateCSharpCompilation("c", text)
            Dim a = DirectCast(comp.GlobalNamespace.GetMembers("A").Single(), ITypeSymbol)
            Dim foo = a.GetMembers("Foo").Single()
            Dim parts = VisualBasic.SymbolDisplay.ToDisplayParts(foo, format)
            Verify(parts,
                   "Public Sub Foo(a As Integer)",
                   SymbolDisplayPartKind.Keyword,
                   SymbolDisplayPartKind.Space,
                   SymbolDisplayPartKind.Keyword,
                   SymbolDisplayPartKind.Space,
                   SymbolDisplayPartKind.MethodName,
                   SymbolDisplayPartKind.Punctuation,
                   SymbolDisplayPartKind.ParameterName,
                   SymbolDisplayPartKind.Space,
                   SymbolDisplayPartKind.Keyword,
                   SymbolDisplayPartKind.Space,
                   SymbolDisplayPartKind.Keyword,
                   SymbolDisplayPartKind.Punctuation)
        End Sub

        <Fact>
        Public Sub SupportSpeculativeSemanticModel()
            Dim text =
        <compilation>
            <file name="a.vb">
Class Explicit
    Class X
        Public Shared Sub Foo
        End Sub
    End Class
End Class
 
Class Z(Of T)
    Inherits Take
End Class
 
Module M
    Sub Main()
        Dim x = From y In ""
        Z(Of Integer).X.Foo ' Simplify Z(Of Integer).X
    End Sub
End Module


    </file>
        </compilation>

            Dim findSymbol As Func(Of NamespaceSymbol, Symbol) = Function(globalns) globalns.GetTypeMembers("Explicit").Single().
                                                                GetTypeMembers("X").Single().
                                                                GetMembers("Foo").Single()

            Dim code As String = DirectCast(text.FirstNode, XElement).FirstNode.ToString

            TestSymbolDescription(text, findSymbol, Nothing,
                "Sub Explicit.X.Foo()",
                code.IndexOf("Z(Of Integer).X.Foo", StringComparison.Ordinal), {
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Operator,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation},
                useSpeculativeSemanticModel:=True,
                minimal:=True)
        End Sub

        <WorkItem(765287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/765287")>
        <Fact>
        Public Sub TestCSharpSymbols()
            Dim csComp = CreateCSharpCompilation("CSharp", <![CDATA[
class Outer
{
    class Inner<T> { }

    void M<U>() { }

    string P { set { } }

    int F;

    event System.Action E;

    delegate void D();

    Missing Error() { }
}
]]>)

            Dim outer = DirectCast(csComp.GlobalNamespace.GetMembers("Outer").Single(), INamedTypeSymbol)
            Dim type = outer.GetMembers("Inner").Single()
            Dim method = outer.GetMembers("M").Single()
            Dim [property] = outer.GetMembers("P").Single()
            Dim field = outer.GetMembers("F").Single()
            Dim [event] = outer.GetMembers("E").Single()
            Dim [delegate] = outer.GetMembers("D").Single()
            Dim [error] = outer.GetMembers("Error").Single()

            Assert.IsNotType(Of Symbol)(type)
            Assert.IsNotType(Of Symbol)(method)
            Assert.IsNotType(Of Symbol)([property])
            Assert.IsNotType(Of Symbol)(field)
            Assert.IsNotType(Of Symbol)([event])
            Assert.IsNotType(Of Symbol)([delegate])
            Assert.IsNotType(Of Symbol)([error])

            ' 1) Looks like VB.
            ' 2) Doesn't blow up.
            Assert.Equal("Outer.Inner(Of T)", VisualBasic.SymbolDisplay.ToDisplayString(type, SymbolDisplayFormat.TestFormat))
            Assert.Equal("Sub Outer.M(Of U)()", VisualBasic.SymbolDisplay.ToDisplayString(method, SymbolDisplayFormat.TestFormat))
            Assert.Equal("WriteOnly Property Outer.P As System.String", VisualBasic.SymbolDisplay.ToDisplayString([property], SymbolDisplayFormat.TestFormat))
            Assert.Equal("Outer.F As System.Int32", VisualBasic.SymbolDisplay.ToDisplayString(field, SymbolDisplayFormat.TestFormat))
            Assert.Equal("Event Outer.E As System.Action", VisualBasic.SymbolDisplay.ToDisplayString([event], SymbolDisplayFormat.TestFormat))
            Assert.Equal("Outer.D", VisualBasic.SymbolDisplay.ToDisplayString([delegate], SymbolDisplayFormat.TestFormat))
            Assert.Equal("Function Outer.Error() As Missing", VisualBasic.SymbolDisplay.ToDisplayString([error], SymbolDisplayFormat.TestFormat))
        End Sub

        <Fact>
        Public Sub FormatPrimitive()
            Assert.Equal("Nothing", SymbolDisplay.FormatPrimitive(Nothing, quoteStrings:=True, useHexadecimalNumbers:=True))

            Assert.Equal("3", SymbolDisplay.FormatPrimitive(OutputKind.NetModule, quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H00000003", SymbolDisplay.FormatPrimitive(OutputKind.NetModule, quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("""x""c", SymbolDisplay.FormatPrimitive("x"c, quoteStrings:=True, useHexadecimalNumbers:=True))
            Assert.Equal("x", SymbolDisplay.FormatPrimitive("x"c, quoteStrings:=False, useHexadecimalNumbers:=True))
            Assert.Equal("""x""c", SymbolDisplay.FormatPrimitive("x"c, quoteStrings:=True, useHexadecimalNumbers:=False))
            Assert.Equal("x", SymbolDisplay.FormatPrimitive("x"c, quoteStrings:=False, useHexadecimalNumbers:=False))

            Assert.Equal("x", SymbolDisplay.FormatPrimitive("x", quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("""x""", SymbolDisplay.FormatPrimitive("x", quoteStrings:=True, useHexadecimalNumbers:=False))

            Assert.Equal("True", SymbolDisplay.FormatPrimitive(True, quoteStrings:=False, useHexadecimalNumbers:=False))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(1, quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H00000001", SymbolDisplay.FormatPrimitive(1, quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(CUInt(1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H00000001", SymbolDisplay.FormatPrimitive(CUInt(1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(CByte(1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H01", SymbolDisplay.FormatPrimitive(CByte(1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(CSByte(1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H01", SymbolDisplay.FormatPrimitive(CSByte(1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(CShort(1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H0001", SymbolDisplay.FormatPrimitive(CShort(1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(CUShort(1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H0001", SymbolDisplay.FormatPrimitive(CUShort(1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(CLng(1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H0000000000000001", SymbolDisplay.FormatPrimitive(CLng(1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1", SymbolDisplay.FormatPrimitive(CULng(1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("&H0000000000000001", SymbolDisplay.FormatPrimitive(CULng(1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1.1", SymbolDisplay.FormatPrimitive(1.1, quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("1.1", SymbolDisplay.FormatPrimitive(1.1, quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1.1", SymbolDisplay.FormatPrimitive(CSng(1.1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("1.1", SymbolDisplay.FormatPrimitive(CSng(1.1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("1.1", SymbolDisplay.FormatPrimitive(CDec(1.1), quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("1.1", SymbolDisplay.FormatPrimitive(CDec(1.1), quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal("#1/1/2000 12:00:00 AM#", SymbolDisplay.FormatPrimitive(#1/1/2000#, quoteStrings:=False, useHexadecimalNumbers:=False))
            Assert.Equal("#1/1/2000 12:00:00 AM#", SymbolDisplay.FormatPrimitive(#1/1/2000#, quoteStrings:=False, useHexadecimalNumbers:=True))

            Assert.Equal(Nothing, SymbolDisplay.FormatPrimitive(New Object(), quoteStrings:=False, useHexadecimalNumbers:=False))
        End Sub

#Region "Helpers"

        Private Sub TestSymbolDescription(
            text As XElement,
            findSymbol As Func(Of NamespaceSymbol, Symbol),
            format As SymbolDisplayFormat,
            expectedText As String,
            position As Integer,
            kinds As SymbolDisplayPartKind(),
            Optional minimal As Boolean = False,
            Optional useSpeculativeSemanticModel As Boolean = False,
            Optional references As IEnumerable(Of MetadataReference) = Nothing)

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib(text)

            If references IsNot Nothing Then
                comp = comp.AddReferences(references.ToArray())
            End If

            Dim symbol = findSymbol(comp.GlobalNamespace)

            Dim description As ImmutableArray(Of SymbolDisplayPart)
            If minimal Then
                Dim tree = comp.SyntaxTrees.First()
                Dim semanticModel As SemanticModel = comp.GetSemanticModel(tree)
                Dim tokenPosition = tree.GetRoot().FindToken(position).SpanStart

                If useSpeculativeSemanticModel Then
                    Dim newTree = tree.WithChangedText(tree.GetText())
                    Dim token = newTree.GetRoot().FindToken(position)
                    tokenPosition = token.SpanStart

                    Dim member = token.Parent.FirstAncestorOrSelf(Of MethodBlockBaseSyntax)()
                    Dim speculativeModel As SemanticModel = Nothing
                    semanticModel.TryGetSpeculativeSemanticModelForMethodBody(member.BlockStatement.Span.End, member, speculativeModel)
                    semanticModel = speculativeModel
                End If

                description = VisualBasic.SymbolDisplay.ToMinimalDisplayParts(symbol, semanticModel, tokenPosition, format)
            Else
                description = VisualBasic.SymbolDisplay.ToDisplayParts(symbol, format)
            End If

            Verify(description, expectedText, kinds)
        End Sub

        Private Sub TestSymbolDescription(
            text As XElement,
            findSymbol As Func(Of NamespaceSymbol, Symbol),
            format As SymbolDisplayFormat,
            expectedText As String,
            ParamArray kinds As SymbolDisplayPartKind())

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(text, additionalRefs:={SystemCoreRef})

            ' symbol:
            Dim symbol = findSymbol(comp.GlobalNamespace)
            Dim description = VisualBasic.SymbolDisplay.ToDisplayParts(symbol, format)
            Verify(description, expectedText, kinds)

            ' retargeted symbol:
            Dim retargetedAssembly = New Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting.RetargetingAssemblySymbol(comp.SourceAssembly, isLinked:=False)
            retargetedAssembly.SetCorLibrary(comp.SourceAssembly.CorLibrary)
            Dim retargetedSymbol = findSymbol(retargetedAssembly.GlobalNamespace)

            Dim retargetedDescription = VisualBasic.SymbolDisplay.ToDisplayParts(retargetedSymbol, format)
            Verify(retargetedDescription, expectedText, kinds)
        End Sub

        Private Shared Function Verify(parts As ImmutableArray(Of SymbolDisplayPart), expectedText As String, ParamArray kinds As SymbolDisplayPartKind()) As ImmutableArray(Of SymbolDisplayPart)
            Assert.Equal(expectedText, parts.ToDisplayString())

            If (kinds.Length > 0) Then
                AssertEx.Equal(kinds, parts.Select(Function(d) d.Kind))
            End If

            Return parts
        End Function
#End Region

    End Class
End Namespace
