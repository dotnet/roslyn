// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SymbolDisplayTests : CSharpTestBase
    {
        [Fact]
        public void TestClassNameOnlySimple()
        {
            var text = "class A {}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("A", 0).Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "A",
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestClassNameOnlyComplex()
        {
            var text = @"
namespace N1 {
    namespace N2.N3 {
        class C1 {
            class C2 {} } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("C1").Single().
                GetTypeMembers("C2").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C2",
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestFullyQualifiedFormat()
        {
            var text = @"
namespace N1 {
    namespace N2.N3 {
        class C1 {
            class C2 {} } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("C1").Single().
                GetTypeMembers("C2").Single();

            TestSymbolDescription(
                text,
                findSymbol,
                SymbolDisplayFormat.FullyQualifiedFormat,
                "global::N1.N2.N3.C1.C2",
                SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName, SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName, SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName, SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestClassNameAndContainingTypesSimple()
        {
            var text = "class A {}";

            Func<NamespaceSymbol, Symbol> findSymbol = global => global.GetTypeMembers("A", 0).Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "A",
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestClassNameAndContainingTypesComplex()
        {
            var text = @"
namespace N1 {
    namespace N2.N3 {
        class C1 {
            class C2 {} } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("C1").Single().
                GetTypeMembers("C2").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C1.C2",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestMethodNameOnlySimple()
        {
            var text = @"
class A {
    void M() {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("A", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat();

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M",
                SymbolDisplayPartKind.MethodName);
        }

        [Fact]
        public void TestMethodNameOnlyComplex()
        {
            var text = @"
namespace N1 {
    namespace N2.N3 {
        class C1 {
            class C2 {
                public static int[] M(int? x, C1 c) {} } } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("C1").Single().
                GetTypeMembers("C2").Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat();

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M",
                SymbolDisplayPartKind.MethodName);
        }

        [Fact]
        public void TestMethodAndParamsSimple()
        {
            var text = @"
class A {
    void M() {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("A", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private void M()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestMethodAndParamsComplex()
        {
            var text = @"
namespace N1 {
    namespace N2.N3 {
        class C1 {
            class C2 {
                public static int[] M(int? x, C1 c) {} } } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("C1").Single().
                GetTypeMembers("C2").Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "public static int[] M(int? x, C1 c)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //x
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C1
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //c
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestExtensionMethodAsStatic()
        {
            var text = @"
            class C1<T> { }
            class C2 {
                public static TSource M<TSource>(this C1<TSource> source, int index) {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C2").Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "public static TSource C2.M<TSource>(this C1<TSource> source, int index)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C2
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C1
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //source
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //index
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestExtensionMethodAsInstance()
        {
            var text = @"
            class C1<T> { }
            class C2 {
                public static TSource M<TSource>(this C1<TSource> source, int index) {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C2").Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.InstanceMethod,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "public TSource C1<TSource>.M<TSource>(int index)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C1
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //index
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestExtensionMethodAsDefault()
        {
            var text = @"
            class C1<T> { }
            class C2 {
                public static TSource M<TSource>(this C1<TSource> source, int index) {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C2").Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "public static TSource C2.M<TSource>(this C1<TSource> source, int index)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C2
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C1
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //source
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //index
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestReducedExtensionMethodAsStatic()
        {
            var text = @"
            class C1 { }
            class C2 {
                public static TSource M<TSource>(this C1 source, int index) {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
            {
                var type = global.GetTypeMember("C1");
                var method = (MethodSymbol)global.GetTypeMember("C2").GetMember("M");
                return method.ReduceExtensionMethod(type);
            };

            var format = new SymbolDisplayFormat(
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "public static TSource C2.M<TSource>(this C1 source, int index)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C2
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C1
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //source
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //index
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestReducedExtensionMethodAsInstance()
        {
            var text = @"
            class C1 { }
            class C2 {
                public static TSource M<TSource>(this C1 source, int index) {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
            {
                var type = global.GetTypeMember("C1");
                var method = (MethodSymbol)global.GetTypeMember("C2").GetMember("M");
                return method.ReduceExtensionMethod(type);
            };

            var format = new SymbolDisplayFormat(
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.InstanceMethod,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "public TSource C1.M<TSource>(int index)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C1
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //index
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestReducedExtensionMethodAsDefault()
        {
            var text = @"
            class C1 { }
            class C2 {
                public static TSource M<TSource>(this C1 source, int index) {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
            {
                var type = global.GetTypeMember("C1");
                var method = (MethodSymbol)global.GetTypeMember("C2").GetMember("M");
                return method.ReduceExtensionMethod(type);
            };

            var format = new SymbolDisplayFormat(
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "public TSource C1.M<TSource>(int index)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C1
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName, //TSource
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //index
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestNullParameters()
        {
            var text = @"
class A {
    int[][,][,,] f; }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("A", 0).Single().
                GetMembers("f").Single();

            SymbolDisplayFormat format = null;

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "A.f",
                SymbolDisplayPartKind.ClassName, //A
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName); //f
        }

        [Fact]
        public void TestArrayRank()
        {
            var text = @"
class A {
    int[][,][,,] f; }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("A", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "int[][,][,,] f",
                SymbolDisplayPartKind.Keyword, //int
                SymbolDisplayPartKind.Punctuation, //[
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation, //[
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation, //[
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName); //f
        }

        [Fact]
        public void TestOptionalParameters_Bool()
        {
            var text = @"
using System.Runtime.InteropServices;

class C
{
    void F([Optional]bool arg) { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("F").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void F(bool arg)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // F
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // arg
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestOptionalParameters_String()
        {
            var text = @"
class C
{
    void F(string s = ""f\t\r\noo"") { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("F").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                @"void F(string s = ""f\t\r\noo"")",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,    // F
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // s
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,   // =
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StringLiteral,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestOptionalParameters_ReferenceType()
        {
            var text = @"
using System.Runtime.InteropServices;

class C
{
    void F([Optional]C arg) { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("F").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void F(C arg)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // F
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // arg
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestOptionalParameters_Constrained_Class()
        {
            var text = @"
using System.Runtime.InteropServices;

class C
{
    void F<T>([Optional]T arg) where T : class { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("F").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void F(T arg)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // F
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // arg
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestOptionalParameters_Constrained_Struct()
        {
            var text = @"
using System.Runtime.InteropServices;

class C
{
    void F<T>([Optional]T arg) where T : struct { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("F").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void F(T arg)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // F
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // arg
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestOptionalParameters_Unconstrained()
        {
            var text = @"
using System.Runtime.InteropServices;

class C
{
    void F<T>([Optional]T arg) { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("F").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void F(T arg)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // F
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // arg
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestOptionalParameters_ArrayAndType()
        {
            var text = @"
using System.Runtime.InteropServices;

class C
{
    void F<T>(int a, [Optional]double[] arg, int b, [Optional]System.Type t) { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("F").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void F(int a, double[] arg, int b, Type t)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // F
                SymbolDisplayPartKind.Punctuation,

                SymbolDisplayPartKind.Keyword, // int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // a
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,

                SymbolDisplayPartKind.Keyword, // double
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // arg
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,

                SymbolDisplayPartKind.Keyword, // int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // b
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,

                SymbolDisplayPartKind.ClassName, // Type
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // t

                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestEscapeKeywordIdentifiers()
        {
            var text = @"
class @true {
    @true @false(@true @true, bool @bool = true) { return @true; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("true", 0).Single().
                GetMembers("false").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "@true @false(@true @true, bool @bool = true)",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, //@false
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //@true
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //@bool
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestNoEscapeKeywordIdentifiers()
        {
            var text = @"
class @true {
    @true @false(@true @true, bool @bool = true) { return @true; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("true", 0).Single().
                GetMembers("false").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "true false(true true, bool bool = true)",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // @false
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // @true
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // @bool
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestExplicitMethodImplNameOnly()
        {
            var text = @"
interface I {
    void M(); }
class C : I {
    void I.M() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("I.M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.None);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M",
                SymbolDisplayPartKind.MethodName); //M
        }

        [Fact]
        public void TestExplicitMethodImplNameAndInterface()
        {
            var text = @"
interface I {
    void M(); }
class C : I {
    void I.M() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("I.M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "I.M",
                SymbolDisplayPartKind.InterfaceName, //I
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName); //M
        }

        [Fact]
        public void TestExplicitMethodImplNameAndInterfaceAndType()
        {
            var text = @"
interface I {
    void M(); }
class C : I {
    void I.M() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("I.M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeContainingType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C.I.M",
                SymbolDisplayPartKind.ClassName, //C
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.InterfaceName, //I
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName); //M
        }

        [Fact]
        public void TestGlobalNamespaceCode()
        {
            var text = @"
class C { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "global::C",
                SymbolDisplayPartKind.Keyword, //global
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName); //C
        }

        [Fact]
        public void TestGlobalNamespaceHumanReadable()
        {
            var text = @"
class C { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global;

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "<global namespace>",
                SymbolDisplayPartKind.Text);
        }

        [Fact]
        public void TestSpecialTypes()
        {
            var text = @"
class C {
    int f; }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "int f",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestArrayAsterisks()
        {
            var text = @"
class C {
    int[][,][,,] f; }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseAsterisksInMultiDimensionalArrays);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Int32[][*,*][*,*,*] f",
                SymbolDisplayPartKind.StructName,
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
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestMetadataMethodNames()
        {
            var text = @"
class C {
    C() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers(".ctor").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                ".ctor",
                SymbolDisplayPartKind.MethodName);
        }

        [Fact]
        public void TestArityForGenericTypes()
        {
            var text = @"
class C<T, U, V> { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 3).Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C`3",
                SymbolDisplayPartKind.ClassName,
                InternalSymbolDisplayPartKind.Arity);
        }

        [Fact]
        public void TestGenericTypeParameters()
        {
            var text = @"
class C<in T, out U, V> { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 3).Single();

            var format = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C<T, U, V>",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestGenericTypeParametersAndVariance()
        {
            var text = @"
class C<in T, out U, V> { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 3).Single();

            var format = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C<in T, out U, V>",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
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
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestGenericTypeConstraints()
        {
            var text = @"
class C<T> where T : C<T> { }
";

            Func<NamespaceSymbol, Symbol> findType = global =>
                global.GetMember<NamedTypeSymbol>("C");

            Func<NamespaceSymbol, Symbol> findTypeParameter = global =>
                global.GetMember<NamedTypeSymbol>("C").TypeParameters.Single();

            var format = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints);

            TestSymbolDescription(text, findType,
                format,
                "C<T> where T : C<T>",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation);

            TestSymbolDescription(text, findTypeParameter,
                format,
                "T",
                SymbolDisplayPartKind.TypeParameterName);
        }

        [Fact]
        public void TestGenericMethodParameters()
        {
            var text = @"
class C { 
    void M<in T, out U, V>() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M<T, U, V>",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestGenericMethodParametersAndVariance()
        {
            var text = @"
class C { 
    void M<in T, out U, V>() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M<T, U, V>",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestGenericMethodConstraints()
        {
            var text = @"
class C<T>
{
    void M<U, V>() where V : class, U, T {}
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");

            var format = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M<U, V> where V : class, U, T",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName);
        }

        [Fact]
        public void TestMemberMethodNone()
        {
            var text = @"
class C {
    void M(int p) { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.None);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M",
                SymbolDisplayPartKind.MethodName);
        }

        [Fact]
        public void TestMemberMethodAll()
        {
            var text = @"
class C {
    void M(int p) { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private void C.M()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestMemberFieldNone()
        {
            var text = @"
class C {
    int f; }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.None);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "f",
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestMemberFieldAll()
        {
            var text =
@"class C {
    int f;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private Int32 C.f",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestMemberPropertyNone()
        {
            var text = @"
class C {
    int P { get; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("P").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.None);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "P",
                SymbolDisplayPartKind.PropertyName);
        }

        [Fact]
        public void TestMemberPropertyAll()
        {
            var text = @"
class C {
    int P { get; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("P").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private Int32 C.P",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName);
        }

        [Fact]
        public void TestMemberPropertyGetSet()
        {
            var text = @"
class C
{
    int P { get; }
    object Q { set; }
    object R { get { return null; } set { } }
}
";

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C", 0).Single().GetMembers("P").Single(),
                format,
                "Int32 P { get; }",
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C", 0).Single().GetMembers("Q").Single(),
                format,
                "Object Q { set; }",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C", 0).Single().GetMembers("R").Single(),
                format,
                "Object R { get; set; }",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestPropertyGetAccessor()
        {
            var text = @"
class C {
    int P { get; set; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("get_P").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private Int32 C.P.get",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);
        }

        [Fact]
        public void TestPropertySetAccessor()
        {
            var text = @"
class C {
    int P { get; set; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("set_P").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private void C.P.set",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);
        }

        [Fact]
        public void TestMemberEventAll()
        {
            var text = @"
class C {
    event System.Action E;
    event System.Action F { add { } remove { } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol1 = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMembers("E").Where(m => m.Kind == SymbolKind.Event).Single();

            Func<NamespaceSymbol, Symbol> findSymbol2 = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMember<EventSymbol>("F");

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol1,
                format,
                "private Action C.E",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);

            TestSymbolDescription(
                text,
                findSymbol2,
                format,
                "private Action C.F",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);
        }

        [Fact]
        public void TestMemberEventAddRemove()
        {
            var text = @"
class C {
    event System.Action E;
    event System.Action F { add { } remove { } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol1 = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMembers("E").Where(m => m.Kind == SymbolKind.Event).Single();

            Func<NamespaceSymbol, Symbol> findSymbol2 = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMember<EventSymbol>("F");

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor); // Does not affect events (did before rename).

            TestSymbolDescription(
                text,
                findSymbol1,
                format,
                "Action E",
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EventName);

            TestSymbolDescription(
                text,
                findSymbol2,
                format,
                "Action F",
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EventName);
        }

        [Fact]
        public void TestEventAddAccessor()
        {
            var text = @"
class C {
    event System.Action E { add { } remove { } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMembers("add_E").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private void C.E.add",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);
        }

        [Fact]
        public void TestEventRemoveAccessor()
        {
            var text = @"
class C {
    event System.Action E { add { } remove { } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMembers("remove_E").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private void C.E.remove",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);
        }

        [Fact]
        public void TestParameterMethodNone()
        {
            var text = @"
static class C {
    static void M(this object obj, ref short s, int i = 1) { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.None);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M()",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestMethodReturnType1()
        {
            var text = @"
static class C {
    static int M() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "System.Int32 M()",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestMethodReturnType2()
        {
            var text = @"
static class C {
    static void M() { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void M()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestParameterMethodNameTypeModifiers()
        {
            var text = @"
class C {
    void M(ref short s, int i, params string[] args) { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(ref Int16 s, Int32 i, params String[] args)",
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //s
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //i
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //args
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact()]
        public void TestParameterMethodNameAll()
        {
            var text = @"
static class C {
    static void M(this object self, ref short s, int i = 1, params string[] args) { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeDefaultValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M(this Object self, ref Int16 s, Int32 i = 1, params String[] args)",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //self
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //s
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //i
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //args
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestOptionalParameterBrackets()
        {
            var text = @"
class C {
    void M(int i = 0) { } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "M([Int32 i])",
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //i
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        /// <summary>
        /// "public" and "abstract" should not be included for interface members.
        /// </summary>
        [Fact]
        public void TestInterfaceMembers()
        {
            var text = @"
interface I
{
    int P { get; }
    object F();
}
abstract class C
{
    public abstract object F();
    interface I
    {
        void M();
    }
}";

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("I", 0).Single().GetMembers("P").Single(),
                format,
                "int P { get; }");

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("I", 0).Single().GetMembers("F").Single(),
                format,
                "object F()");

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C", 0).Single().GetMembers("F").Single(),
                format,
                "public abstract object F()");

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C", 0).Single().GetTypeMembers("I", 0).Single().GetMembers("M").Single(),
                format,
                "void M()");
        }

        [WorkItem(537447, "DevDiv")]
        [Fact]
        public void TestBug2239()
        {
            var text = @"
public class GC1<T> {}
public class X : GC1<BOGUS> {}
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("X", 0).Single().
                BaseType;

            var format = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "GC1<BOGUS>",
                SymbolDisplayPartKind.ClassName, //GC1
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ErrorTypeName, //BOGUS
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestAlias1()
        {
            var text = @"
using Foo = N1.N2.N3;

namespace N1 {
    namespace N2.N3 {
        class C1 {
            class C2 {} } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("C1").Single().
                GetTypeMembers("C2").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Foo.C1.C2",
                text.IndexOf("namespace", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.AliasName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestAlias2()
        {
            var text = @"
using Foo = N1.N2.N3.C1;

namespace N1 {
    namespace N2.N3 {
        class C1 {
            class C2 {} } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("C1").Single().
                GetTypeMembers("C2").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Foo.C2",
                text.IndexOf("namespace", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.AliasName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestAlias3()
        {
            var text = @"
using Foo = N1.C1;

namespace N1 {
    class Foo { }
    class C1 { }
}
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetTypeMembers("C1").Single();

            var format = SymbolDisplayFormat.MinimallyQualifiedFormat;

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C1",
                text.IndexOf("class Foo", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestMinimalNamespace1()
        {
            var text = @"
namespace N1 {
    namespace N2 {
        namespace N3 {
            class C1 {
                class C2 {} } } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3");

            var format = new SymbolDisplayFormat();

            TestSymbolDescription(text, findSymbol, format,
                "N1.N2.N3",
                text.IndexOf("N1", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName);

            TestSymbolDescription(text, findSymbol, format,
                "N2.N3",
                text.IndexOf("N2", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName);

            TestSymbolDescription(text, findSymbol, format,
                "N3",
                text.IndexOf("N3", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.NamespaceName);

            TestSymbolDescription(text, findSymbol, format,
                "N3",
                text.IndexOf("C1", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.NamespaceName);

            TestSymbolDescription(text, findSymbol, format,
                "N3",
                text.IndexOf("C2", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.NamespaceName);
        }

        [Fact]
        public void TestRemoveAttributeSuffix1()
        {
            var text = @"
class class1Attribute : System.Attribute { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("class1Attribute").Single();

            TestSymbolDescription(text, findSymbol,
                new SymbolDisplayFormat(),
                "class1Attribute",
                SymbolDisplayPartKind.ClassName);

            TestSymbolDescription(text, findSymbol,
                new SymbolDisplayFormat(miscellaneousOptions: SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix),
                "class1",
                0,
                true,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestRemoveAttributeSuffix2()
        {
            var text = @"
class classAttribute : System.Attribute { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("classAttribute").Single();

            TestSymbolDescription(text, findSymbol,
                new SymbolDisplayFormat(),
                "classAttribute",
                SymbolDisplayPartKind.ClassName);


            TestSymbolDescription(text, findSymbol,
                new SymbolDisplayFormat(miscellaneousOptions: SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix),
                "classAttribute",
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestRemoveAttributeSuffix3()
        {
            var text = @"
class class1Attribute { }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("class1Attribute").Single();

            TestSymbolDescription(text, findSymbol,
                new SymbolDisplayFormat(),
                "class1Attribute",
                SymbolDisplayPartKind.ClassName);

            TestSymbolDescription(text, findSymbol,
                new SymbolDisplayFormat(miscellaneousOptions: SymbolDisplayMiscellaneousOptions.RemoveAttributeSuffix),
                "class1Attribute",
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestMinimalClass1()
        {
            var text = @"
using System.Collections.Generic;

class C1 {
    private System.Collections.Generic.IDictionary<System.Collections.Generic.IList<System.Int32>, System.String> foo;
}
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                ((FieldSymbol)global.GetTypeMembers("C1").Single().GetMembers("foo").Single()).Type.TypeSymbol;

            var format = SymbolDisplayFormat.MinimallyQualifiedFormat;

            TestSymbolDescription(text, findSymbol, format,
                "IDictionary<IList<int>, string>",
                text.IndexOf("foo", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.InterfaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.InterfaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestMethodCustomModifierPositions()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
                {
                    TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                    TestReferences.NetFx.v4_0_21006.mscorlib
                });

            var globalNamespace = assemblies[0].GlobalNamespace;
            var @class = globalNamespace.GetMember<NamedTypeSymbol>("MethodCustomModifierCombinations");

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers);

            Verify(@class.GetMember<MethodSymbol>("Method1111").ToDisplayParts(format),
                "int modopt(IsConst) [] modopt(IsConst) Method1111(int modopt(IsConst) [] modopt(IsConst) a)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(@class.GetMember<MethodSymbol>("Method1000").ToDisplayParts(format),
                "int modopt(IsConst) [] Method1000(int[] a)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(@class.GetMember<MethodSymbol>("Method0100").ToDisplayParts(format),
                "int[] modopt(IsConst) Method0100(int[] a)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(@class.GetMember<MethodSymbol>("Method0010").ToDisplayParts(format),
                "int[] Method0010(int modopt(IsConst) [] a)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(@class.GetMember<MethodSymbol>("Method0001").ToDisplayParts(format),
                "int[] Method0001(int[] modopt(IsConst) a)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(@class.GetMember<MethodSymbol>("Method0000").ToDisplayParts(format),
                "int[] Method0000(int[] a)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestPropertyCustomModifierPositions()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;
            var @class = globalNamespace.GetMember<NamedTypeSymbol>("PropertyCustomModifierCombinations");

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers);

            Verify(@class.GetMember<PropertySymbol>("Property11").ToDisplayParts(format),
                "int modopt(IsConst) [] modopt(IsConst) Property11",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName);

            Verify(@class.GetMember<PropertySymbol>("Property10").ToDisplayParts(format),
                "int modopt(IsConst) [] Property10",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName);

            Verify(@class.GetMember<PropertySymbol>("Property01").ToDisplayParts(format),
                "int[] modopt(IsConst) Property01",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName);

            Verify(@class.GetMember<PropertySymbol>("Property00").ToDisplayParts(format),
                "int[] Property00",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName);
        }

        [Fact]
        public void TestFieldCustomModifierPositions()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;
            var @class = globalNamespace.GetMember<NamedTypeSymbol>("FieldCustomModifierCombinations");

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers);

            Verify(@class.GetMember<FieldSymbol>("field11").ToDisplayParts(format),
                "int modopt(IsConst) [] modopt(IsConst) field11",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);

            Verify(@class.GetMember<FieldSymbol>("field10").ToDisplayParts(format),
                "int modopt(IsConst) [] field10",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);

            Verify(@class.GetMember<FieldSymbol>("field01").ToDisplayParts(format),
                "int[] modopt(IsConst) field01",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);

            Verify(@class.GetMember<FieldSymbol>("field00").ToDisplayParts(format),
                "int[] field00",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestMultipleCustomModifier()
        {
            var assemblies = MetadataTestHelpers.GetSymbolsForReferences(new[]
            {
                TestReferences.SymbolsTests.CustomModifiers.Modifiers.dll,
                TestReferences.NetFx.v4_0_21006.mscorlib
            });

            var globalNamespace = assemblies[0].GlobalNamespace;
            var @class = globalNamespace.GetMember<NamedTypeSymbol>("Modifiers");

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.IncludeCustomModifiers);

            Verify(@class.GetMember<MethodSymbol>("F3").ToDisplayParts(format),
                "void F3(int modopt(int) modopt(IsConst) modopt(IsConst) p)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                InternalSymbolDisplayPartKind.Other, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.Punctuation, //modopt
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        private static void TestSymbolDescription(
            string source,
            Func<NamespaceSymbol, Symbol> findSymbol,
            SymbolDisplayFormat format,
            string expectedText,
            int position,
            bool minimal,
            params SymbolDisplayPartKind[] expectedKinds)
        {
            var comp = CreateCompilationWithMscorlib(source);
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var global = comp.GlobalNamespace;
            var symbol = findSymbol(global);

            var description = minimal
                ? symbol.ToMinimalDisplayParts(model, position, format)
                : symbol.ToDisplayParts(format);

            Verify(description, expectedText, expectedKinds);
        }

        private static void TestSymbolDescription(
            string source,
            Func<NamespaceSymbol, Symbol> findSymbol,
            SymbolDisplayFormat format,
            string expectedText,
            params SymbolDisplayPartKind[] expectedKinds)
        {
            var comp = CreateCompilationWithMscorlib(source);
            var global = comp.GlobalNamespace;
            var symbol = findSymbol(global);
            var description = symbol.ToDisplayParts(format);

            Verify(description, expectedText, expectedKinds);
        }

        private static void Verify(ImmutableArray<SymbolDisplayPart> actualParts, string expectedText, params SymbolDisplayPartKind[] expectedKinds)
        {
            Assert.Equal(expectedText, actualParts.ToDisplayString());
            if (expectedKinds.Length > 0)
            {
                for (int i = 0; i < Math.Min(expectedKinds.Length, actualParts.Length); i++)
                {
                    Assert.Equal(expectedKinds[i], actualParts[i].Kind);
                }

                Assert.Equal(expectedKinds.Length, actualParts.Length);
            }
        }

        [Fact]
        public void DelegateStyleRecursive()
        {
            var text = "public delegate void D(D param);";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("D", 0).Single();

            var format = new SymbolDisplayFormat(globalNamespaceStyle: SymbolDisplayFormat.CSharpErrorMessageFormat.GlobalNamespaceStyle,
                                                typeQualificationStyle: SymbolDisplayFormat.CSharpErrorMessageFormat.TypeQualificationStyle,
                                                genericsOptions: SymbolDisplayFormat.CSharpErrorMessageFormat.GenericsOptions,
                                                memberOptions: SymbolDisplayFormat.CSharpErrorMessageFormat.MemberOptions,
                                                parameterOptions: SymbolDisplayFormat.CSharpErrorMessageFormat.ParameterOptions,
                                                propertyStyle: SymbolDisplayFormat.CSharpErrorMessageFormat.PropertyStyle,
                                                localOptions: SymbolDisplayFormat.CSharpErrorMessageFormat.LocalOptions,
                                                kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword,
                                                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                                                miscellaneousOptions: SymbolDisplayFormat.CSharpErrorMessageFormat.MiscellaneousOptions);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "delegate void D(D)",
                SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.Space, SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.Space, SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.DelegateName, SymbolDisplayPartKind.Punctuation);

            format = new SymbolDisplayFormat(parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeType,
                                                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                                                miscellaneousOptions: SymbolDisplayFormat.CSharpErrorMessageFormat.MiscellaneousOptions);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void D(D param)",
                SymbolDisplayPartKind.Keyword, SymbolDisplayPartKind.Space, SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.DelegateName, SymbolDisplayPartKind.Space, SymbolDisplayPartKind.ParameterName, SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void GlobalNamespace1()
        {
            var text = @"public class Test
{
    public class System
    {
        public class Action
        {
        }
    }

    public global::System.Action field;
    public System.Action field2;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
            {
                var field = global.GetTypeMembers("Test", 0).Single().GetMembers("field").Single() as FieldSymbol;
                return field.Type.TypeSymbol;
            };

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "global::System.Action",
                text.IndexOf("global::System.Action", StringComparison.Ordinal),
                true /* minimal */,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName);
        }

        [Fact]
        public void GlobalNamespace2()
        {
            var text = @"public class Test
{
    public class System
    {
        public class Action
        {
        }
    }

    public global::System.Action field;
    public System.Action field2;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
            {
                var field = global.GetTypeMembers("Test", 0).Single().GetMembers("field").Single() as FieldSymbol;
                return field.Type.TypeSymbol;
            };

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "System.Action",
                text.IndexOf("global::System.Action", StringComparison.Ordinal),
                true /* minimal */,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName);
        }

        [Fact]
        public void GlobalNamespace3()
        {
            var text = @"public class Test
{
    public class System
    {
        public class Action
        {
        }
    }

    public System.Action field2;
    public global::System.Action field;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
            {
                var field = global.GetTypeMembers("Test", 0).Single().GetMembers("field2").Single() as FieldSymbol;
                return field.Type.TypeSymbol;
            };

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "System.Action",
                text.IndexOf("System.Action", StringComparison.Ordinal),
                true /* minimal */,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void DefaultParameterValues()
        {
            var text = @"
struct S
{
    void M(
        int i = 1,
        string str = ""hello"",
        object o = null
        S s = default(S))
    {
    }
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("M");

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(text, findSymbol,
                format,
                @"void S.M(int i = 1, string str = ""hello"", object o = null, S s = default(S))",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StringLiteral,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //)
                SymbolDisplayPartKind.Punctuation); //)
        }

        [Fact]
        public void DefaultParameterValues_TypeParameter()
        {
            var text = @"
struct S
{
    void M<T>(T t = default(T))
    {
    }
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("M");

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(text, findSymbol,
                format,
                @"void S.M<T>(T t = default(T))",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //<
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation, //>
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation, //)
                SymbolDisplayPartKind.Punctuation); //)
        }

        [Fact]
        public void DefaultParameterValues_Enum()
        {
            var text = @"
enum E
{
    A = 1,
    B = 2,
    C = 5,
}

struct S
{
    void P(E e = (E)1)
    {
    }
    void Q(E e = (E)3)
    {
    }
    void R(E e = (E)5)
    {
    }
}";

            Func<NamespaceSymbol, Symbol> findSymbol1 = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("P");
            Func<NamespaceSymbol, Symbol> findSymbol2 = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("Q");
            Func<NamespaceSymbol, Symbol> findSymbol3 = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("R");

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(text, findSymbol1,
                format,
                @"void S.P(E e = E.A)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation); //)

            TestSymbolDescription(text, findSymbol2,
                format,
                @"void S.Q(E e = (E)3)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //)
                SymbolDisplayPartKind.NumericLiteral,
                SymbolDisplayPartKind.Punctuation); //)

            TestSymbolDescription(text, findSymbol3,
                format,
                @"void S.R(E e = E.C)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation); //)
        }

        [Fact]
        public void DefaultParameterValues_FlagsEnum()
        {
            var text = @"
[System.FlagsAttribute]
enum E
{
    A = 1,
    B = 2,
    C = 5,
}

struct S
{
    void P(E e = (E)1)
    {
    }
    void Q(E e = (E)3)
    {
    }
    void R(E e = (E)5)
    {
    }
}";

            Func<NamespaceSymbol, Symbol> findSymbol1 = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("P");
            Func<NamespaceSymbol, Symbol> findSymbol2 = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("Q");
            Func<NamespaceSymbol, Symbol> findSymbol3 = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("R");

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(text, findSymbol1,
                format,
                @"void S.P(E e = E.A)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation); //)

            TestSymbolDescription(text, findSymbol2,
                format,
                @"void S.Q(E e = E.A | E.B)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //|
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation); //)

            TestSymbolDescription(text, findSymbol3,
                format,
                @"void S.R(E e = E.C)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void DefaultParameterValues_NegativeEnum()
        {
            var text = @"
[System.FlagsAttribute]
enum E : sbyte
{
    A = -2,
    A1 = -2,
    B = 1,
    B1 = 1,
    C = 0,
    C1 = 0,
}

struct S
{
    void P(E e = (E)(-2), E f = (E)(-1), E g = (E)0, E h = (E)(-3))
    {
    }
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("S").GetMember<MethodSymbol>("P");

            var format =
             new SymbolDisplayFormat(
                 globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                 memberOptions:
                     SymbolDisplayMemberOptions.IncludeParameters |
                     SymbolDisplayMemberOptions.IncludeType |
                     SymbolDisplayMemberOptions.IncludeContainingType,
                 parameterOptions:
                     SymbolDisplayParameterOptions.IncludeName |
                     SymbolDisplayParameterOptions.IncludeType |
                     SymbolDisplayParameterOptions.IncludeParamsRefOut |
                     SymbolDisplayParameterOptions.IncludeDefaultValue,
                 localOptions: SymbolDisplayLocalOptions.IncludeType,
                 miscellaneousOptions:
                     SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                     SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(text, findSymbol,
                format,
                @"void S.P(E e = E.A, E f = E.A | E.B, E g = E.C, E h = (E)-3)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //|
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, // =
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, // (
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, // )
                SymbolDisplayPartKind.NumericLiteral,
                SymbolDisplayPartKind.Punctuation); //)
        }

        [Fact]
        public void TestConstantFieldValue()
        {
            var text =
@"class C {
    const int f = 1;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeConstantValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private const Int32 C.f = 1",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral);
        }

        [Fact]
        public void TestConstantFieldValue_EnumMember()
        {
            var text =
@"
enum E { A, B, C }
class C {
    const E f = E.B;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeConstantValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private const E C.f = E.B",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestConstantFieldValue_EnumMember_Flags()
        {
            var text =
@"
[System.FlagsAttribute]
enum E { A = 1, B = 2, C = 4, D = A | B | C }
class C {
    const E f = E.D;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeConstantValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private const E C.f = E.D",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestEnumMember()
        {
            var text =
@"enum E { A, B, C }";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("E", 0).Single().
                GetMembers("B").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeConstantValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "E.B = 1",
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral);
        }

        [Fact]
        public void TestEnumMember_Flags()
        {
            var text =
@"[System.FlagsAttribute]
enum E { A = 1, B = 2, C = 4, D = A | B | C }";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("E", 0).Single().
                GetMembers("D").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeConstantValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "E.D = E.A | E.B | E.C",
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void TestEnumMember_FlagsWithoutAttribute()
        {
            var text =
@"enum E { A = 1, B = 2, C = 4, D = A | B | C }";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("E", 0).Single().
                GetMembers("D").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeContainingType |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeConstantValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "E.D = 7",
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral);
        }

        [Fact, WorkItem(545462, "DevDiv")]
        public void DateTimeDefaultParameterValue()
        {
            var text = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class C
{
    static void Foo([Optional][DateTimeConstant(100)] DateTime d) { }
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMember<MethodSymbol>("Foo");

            var format = new SymbolDisplayFormat(
                 memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                 parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                                   SymbolDisplayParameterOptions.IncludeName |
                                   SymbolDisplayParameterOptions.IncludeDefaultValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Foo(DateTime d)",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact, WorkItem(545681, "DevDiv")]
        public void TypeParameterFromMetadata()
        {
            var src1 = @"
public class LibG<T>
{
}
";

            var src2 = @"
public class Gen<V>
{
    public void M(LibG<V> p)
    {
    }
}
";
            var complib = CreateCompilationWithMscorlib(src1, assemblyName: "Lib");
            var compref = new CSharpCompilationReference(complib);
            var comp1 = CreateCompilationWithMscorlib(src2, references: new MetadataReference[] { compref }, assemblyName: "Comp1");

            var mtdata = comp1.EmitToArray();
            var mtref = MetadataReference.CreateFromImage(mtdata);
            var comp2 = CreateCompilationWithMscorlib("", references: new MetadataReference[] { mtref }, assemblyName: "Comp2");

            var tsym1 = comp1.SourceModule.GlobalNamespace.GetMember<NamedTypeSymbol>("Gen");
            Assert.NotNull(tsym1);
            var msym1 = tsym1.GetMember<MethodSymbol>("M");
            Assert.NotNull(msym1);
            Assert.Equal("Gen<V>.M(LibG<V>)", msym1.ToDisplayString());

            var tsym2 = comp2.GlobalNamespace.GetMember<NamedTypeSymbol>("Gen");
            Assert.NotNull(tsym2);
            var msym2 = tsym2.GetMember<MethodSymbol>("M");
            Assert.NotNull(msym2);
            Assert.Equal(msym1.ToDisplayString(), msym2.ToDisplayString());
        }

        [Fact, WorkItem(545625, "DevDiv")]
        public void ReverseArrayRankSpecifiers()
        {
            var text = @"
public class C
{
    C[][,] F;
}
";
            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("F").Type.TypeSymbol;

            var normalFormat = new SymbolDisplayFormat();
            var reverseFormat = new SymbolDisplayFormat(
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.ReverseArrayRankSpecifiers);

            TestSymbolDescription(
                text,
                findSymbol,
                normalFormat,
                "C[][,]",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);

            TestSymbolDescription(
                text,
                findSymbol,
                reverseFormat,
                "C[,][]",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact, WorkItem(546638, "DevDiv")]
        public void InvariantCultureNegatives()
        {
            var text = @"
public class C
{
    void M(
        sbyte p1 = (sbyte)-1,
        short p2 = (short)-1,
        int p3 = (int)-1,
        long p4 = (long)-1,
        float p5 = (float)-0.5,
        double p6 = (double)-0.5,
        decimal p7 = (decimal)-0.5)
    {
    }
}
";
            var oldCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = (CultureInfo)oldCulture.Clone();
                Thread.CurrentThread.CurrentCulture.NumberFormat.NegativeSign = "~";
                Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ",";

                var compilation = CreateCompilationWithMscorlib(text);
                compilation.VerifyDiagnostics();

                var symbol = compilation.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<MethodSymbol>("M");
                Assert.Equal("void C.M(" +
                    "[System.SByte p1 = -1], " +
                    "[System.Int16 p2 = -1], " +
                    "[System.Int32 p3 = -1], " +
                    "[System.Int64 p4 = -1], " +
                    "[System.Single p5 = -0.5], " +
                    "[System.Double p6 = -0.5], " +
                    "[System.Decimal p7 = -0.5])", symbol.ToTestDisplayString());
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = oldCulture;
            }
        }

        [Fact]
        public void TestMethodVB()
        {
            var text = @"
Class A
   Public Sub Foo(a As Integer)
   End Sub
End Class";

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var comp = CreateVisualBasicCompilation("c", text);
            var a = (ITypeSymbol)comp.GlobalNamespace.GetMembers("A").Single();
            var foo = a.GetMembers("Foo").Single();
            var parts = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.ToDisplayParts(foo, format);

            Verify(
                parts,
                "public void Foo(int a)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestWindowsRuntimeEvent()
        {
            var source = @"
class C
{
    event System.Action E;
}
";
            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface);

            var comp = CreateCompilation(source, WinRtRefs, TestOptions.ReleaseWinMD);
            var eventSymbol = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C").GetMember<EventSymbol>("E");
            Assert.True(eventSymbol.IsWindowsRuntimeEvent);

            Verify(
                eventSymbol.ToDisplayParts(format),
                "Action C.E",
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);

            Verify(
                eventSymbol.AddMethod.ToDisplayParts(format),
                "EventRegistrationToken C.E.add",
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(
                eventSymbol.RemoveMethod.ToDisplayParts(format),
                "void C.E.remove",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);
        }

        [WorkItem(791756, "DevDiv")]
        [Fact]
        public void KindOptions()
        {
            var source = @"
namespace N
{
    class C
    {
        event System.Action E;
    }
}
";
            var memberFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions: SymbolDisplayKindOptions.IncludeMemberKeyword);
            var typeFormat = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);
            var namespaceFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword);

            var comp = CreateCompilationWithMscorlib(source);
            var namespaceSymbol = comp.GlobalNamespace.GetMember<NamespaceSymbol>("N");
            var typeSymbol = namespaceSymbol.GetMember<NamedTypeSymbol>("C");
            var eventSymbol = typeSymbol.GetMember<EventSymbol>("E");

            Verify(
                namespaceSymbol.ToDisplayParts(memberFormat),
                "N",
                SymbolDisplayPartKind.NamespaceName);
            Verify(
                namespaceSymbol.ToDisplayParts(typeFormat),
                "N",
                SymbolDisplayPartKind.NamespaceName);
            Verify(
                namespaceSymbol.ToDisplayParts(namespaceFormat),
                "namespace N",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName);

            Verify(
                typeSymbol.ToDisplayParts(memberFormat),
                "N.C",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
            Verify(
                typeSymbol.ToDisplayParts(typeFormat),
                "class N.C",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
            Verify(
                typeSymbol.ToDisplayParts(namespaceFormat),
                "N.C",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);

            Verify(
                eventSymbol.ToDisplayParts(memberFormat),
                "event N.C.E",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);
            Verify(
                eventSymbol.ToDisplayParts(typeFormat),
                "N.C.E",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);
            Verify(
                eventSymbol.ToDisplayParts(namespaceFormat),
                "N.C.E",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);
        }


        [WorkItem(765287, "DevDiv")]
        [Fact]
        public void TestVbSymbols()
        {
            var vbComp = CreateVisualBasicCompilation(@"
Class Outer
    Class Inner(Of T)
    End Class

    Sub M(Of U)()
    End Sub

    WriteOnly Property P() As String
        Set(value)
        End Set
    End Property

    Private F As Integer

    Event E()

    Delegate Sub D()

    Function [Error]() As Missing
    End Function
End Class
", assemblyName: "VB");

            var outer = (INamedTypeSymbol)vbComp.GlobalNamespace.GetMembers("Outer").Single();
            var type = outer.GetMembers("Inner").Single();
            var method = outer.GetMembers("M").Single();
            var property = outer.GetMembers("P").Single();
            var field = outer.GetMembers("F").Single();
            var @event = outer.GetMembers("E").Single();
            var @delegate = outer.GetMembers("D").Single();
            var error = outer.GetMembers("Error").Single();

            Assert.IsNotType<Symbol>(type);
            Assert.IsNotType<Symbol>(method);
            Assert.IsNotType<Symbol>(property);
            Assert.IsNotType<Symbol>(field);
            Assert.IsNotType<Symbol>(@event);
            Assert.IsNotType<Symbol>(@delegate);
            Assert.IsNotType<Symbol>(error);

            // 1) Looks like C#.
            // 2) Doesn't blow up.
            Assert.Equal("Outer.Inner<T>", CSharp.SymbolDisplay.ToDisplayString(type, SymbolDisplayFormat.TestFormat));
            Assert.Equal("void Outer.M<U>()", CSharp.SymbolDisplay.ToDisplayString(method, SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.String Outer.P { set; }", CSharp.SymbolDisplay.ToDisplayString(property, SymbolDisplayFormat.TestFormat));
            Assert.Equal("System.Int32 Outer.F", CSharp.SymbolDisplay.ToDisplayString(field, SymbolDisplayFormat.TestFormat));
            Assert.Equal("event Outer.EEventHandler Outer.E", CSharp.SymbolDisplay.ToDisplayString(@event, SymbolDisplayFormat.TestFormat));
            Assert.Equal("Outer.D", CSharp.SymbolDisplay.ToDisplayString(@delegate, SymbolDisplayFormat.TestFormat));
            Assert.Equal("Missing Outer.Error()", CSharp.SymbolDisplay.ToDisplayString(error, SymbolDisplayFormat.TestFormat));
        }

        [Fact]
        public void FormatPrimitive()
        {
            // basic tests, more cases are covered by ObjectFormatterTests
            Assert.Equal("1", SymbolDisplay.FormatPrimitive(1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1", SymbolDisplay.FormatPrimitive((uint)1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1", SymbolDisplay.FormatPrimitive((byte)1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1", SymbolDisplay.FormatPrimitive((sbyte)1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1", SymbolDisplay.FormatPrimitive((short)1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1", SymbolDisplay.FormatPrimitive((ushort)1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1", SymbolDisplay.FormatPrimitive((long)1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1", SymbolDisplay.FormatPrimitive((ulong)1, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("x", SymbolDisplay.FormatPrimitive('x', quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("true", SymbolDisplay.FormatPrimitive(true, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1.5", SymbolDisplay.FormatPrimitive(1.5, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1.5", SymbolDisplay.FormatPrimitive((float)1.5, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("1.5", SymbolDisplay.FormatPrimitive((decimal)1.5, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("null", SymbolDisplay.FormatPrimitive(null, quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal("abc", SymbolDisplay.FormatPrimitive("abc", quoteStrings: false, useHexadecimalNumbers: false));
            Assert.Equal(null, SymbolDisplay.FormatPrimitive(SymbolDisplayFormat.TestFormat, quoteStrings: false, useHexadecimalNumbers: false));
        }

        [WorkItem(879984, "DevDiv")]
        [Fact]
        public void EnumAmbiguityResolution()
        {
            var source = @"
using System;

class Program
{
    static void M(E1 e1 = (E1)1, E2 e2 = (E2)1)
    {
    }
}

enum E1
{
    B = 1,
    A = 1,
}

[Flags]
enum E2 // Identical to E1, but has [Flags]
{
    B = 1,
    A = 1,
}
";
            var comp = CreateCompilationWithMscorlib(source);
            var method = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M");

            var memberFormat = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue);

            Assert.Equal("M(e1 = A, e2 = A)", method.ToDisplayString(memberFormat)); // Alphabetically first candidate chosen for both enums.
        }

        [Fact, WorkItem(1028003, "DevDiv")]
        public void UnconventionalExplicitInterfaceImplementation()
        {
            var il = @"
.class public auto ansi sealed DTest
       extends [mscorlib]System.MulticastDelegate
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(object 'object',
                               native int 'method') runtime managed
  {
  } // end of method DTest::.ctor

  .method public hidebysig newslot virtual 
          instance void  Invoke() runtime managed
  {
  } // end of method DTest::Invoke

  .method public hidebysig newslot virtual 
          instance class [mscorlib]System.IAsyncResult 
          BeginInvoke(class [mscorlib]System.AsyncCallback callback,
                      object 'object') runtime managed
  {
  } // end of method DTest::BeginInvoke

  .method public hidebysig newslot virtual 
          instance void  EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed
  {
  } // end of method DTest::EndInvoke

} // end of class DTest

.class interface public abstract auto ansi ITest
{
  .method public hidebysig newslot abstract virtual 
          instance void  M1() cil managed
  {
  } // end of method ITest::M1

  .method public hidebysig newslot specialname abstract virtual 
          instance int32  get_P1() cil managed
  {
  } // end of method ITest::get_P1

  .method public hidebysig newslot specialname abstract virtual 
          instance void  set_P1(int32 'value') cil managed
  {
  } // end of method ITest::set_P1

  .method public hidebysig newslot specialname abstract virtual 
          instance void  add_E1(class DTest 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  } // end of method ITest::add_E1

  .method public hidebysig newslot specialname abstract virtual 
          instance void  remove_E1(class DTest 'value') cil managed
  {
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
  } // end of method ITest::remove_E1

  .event DTest E1
  {
    .addon instance void ITest::add_E1(class DTest)
    .removeon instance void ITest::remove_E1(class DTest)
  } // end of event ITest::E1
  .property instance int32 P1()
  {
    .get instance int32 ITest::get_P1()
    .set instance void ITest::set_P1(int32)
  } // end of property ITest::P1
} // end of class ITest

.class public auto ansi beforefieldinit CTest
       extends [mscorlib]System.Object
       implements ITest
{
  .method public hidebysig newslot specialname virtual final 
          instance int32  get_P1() cil managed
  {
    .override ITest::get_P1
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  newobj     instance void [mscorlib]System.NotImplementedException::.ctor()
    IL_0006:  throw
  } // end of method CTest::ITest.get_P1

  .method public hidebysig newslot specialname virtual final 
          instance void  set_P1(int32 'value') cil managed
  {
    .override ITest::set_P1
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  newobj     instance void [mscorlib]System.NotImplementedException::.ctor()
    IL_0006:  throw
  } // end of method CTest::ITest.set_P1

  .method public hidebysig newslot specialname virtual final 
          instance void  add_E1(class DTest 'value') cil managed
  {
    .override ITest::add_E1
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  newobj     instance void [mscorlib]System.NotImplementedException::.ctor()
    IL_0006:  throw
  } // end of method CTest::ITest.add_E1

  .method public hidebysig newslot specialname virtual final 
          instance void  remove_E1(class DTest 'value') cil managed
  {
    .override ITest::remove_E1
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  newobj     instance void [mscorlib]System.NotImplementedException::.ctor()
    IL_0006:  throw
  } // end of method CTest::ITest.remove_E1

  .method public hidebysig newslot virtual final 
          instance void  M1() cil managed
  {
    .override ITest::M1
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  newobj     instance void [mscorlib]System.NotImplementedException::.ctor()
    IL_0006:  throw
  } // end of method CTest::ITest.M1

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method CTest::.ctor

  .event DTest E1
  {
    .addon instance void CTest::add_E1(class DTest)
    .removeon instance void CTest::remove_E1(class DTest)
  } // end of event CTest::ITest.E1
  .property instance int32 P1()
  {
    .get instance int32 CTest::get_P1()
    .set instance void CTest::set_P1(int32)
  } // end of property CTest::ITest.P1
} // end of class CTest
";

            var text = @"";
            var comp = CreateCompilationWithCustomILSource(text, il);

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface);

            var cTest = comp.GetTypeByMetadataName("CTest");
            var m1 = cTest.GetMember("M1");
            Assert.Equal("M1", m1.Name);
            Assert.Equal("M1", m1.ToDisplayString(format));

            var p1 = cTest.GetMember("P1");
            Assert.Equal("P1", p1.Name);
            Assert.Equal("P1", p1.ToDisplayString(format));

            var e1 = cTest.GetMember("E1");
            Assert.Equal("E1", e1.Name);
            Assert.Equal("E1", e1.ToDisplayString(format));
        }
    }
}
