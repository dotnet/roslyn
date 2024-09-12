// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Basic.Reference.Assemblies;

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

        [Fact, WorkItem(46985, "https://github.com/dotnet/roslyn/issues/46985")]
        public void TestRecordNameOnlySimple()
        {
            var text = "record A {}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("A", 0).Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "A",
                SymbolDisplayPartKind.RecordClassName);
        }

        [Fact, WorkItem(46985, "https://github.com/dotnet/roslyn/issues/46985")]
        public void TestRecordNameOnlyComplex()
        {
            var text = @"
namespace N1 {
    namespace N2.N3 {
        record R1 {
            record R2 {} } } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetNestedNamespace("N2").
                GetNestedNamespace("N3").
                GetTypeMembers("R1").Single().
                GetTypeMembers("R2").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "R2",
                SymbolDisplayPartKind.RecordClassName);
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
                SymbolDisplayPartKind.ExtensionMethodName, //M
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
                return method.ReduceExtensionMethod(type, null!);
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
                return method.ReduceExtensionMethod(type, null!);
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
                SymbolDisplayPartKind.ExtensionMethodName, //M
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
                return method.ReduceExtensionMethod(type, null!);
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
                SymbolDisplayPartKind.ExtensionMethodName, //M
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
        public void TestPrivateProtected()
        {
            var text = @"class C2 { private protected void M() {} }";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C2").Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeAccessibility |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeContainingType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "private protected void C2.M()",
                SymbolDisplayPartKind.Keyword, // private
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // protected
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // void
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, //C2
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
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
        public void TestEscapeRecordKeywordIdentifiers_EscapesTypeNames()
        {
            var text = @"
class @record {
    @record @struct(@record @true, string name, bool @bool = true) { return @record; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("record", 0).Single().
                GetMembers("struct").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "@record @struct(@record @true, string name, bool @bool = true)",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, //@struct
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //@record
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //string
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
        public void TestEscapeRecordKeywordIdentifiers_DoesNotEscapesMethodNames()
        {
            var text = @"
class C {
    C record() { return default; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("C", 0).Single().
                GetMembers("record").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "C record()",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, //record
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/74117")]
        public void TestRecordStructName()
        {
            var text = @"
public record struct @decimal {
    void M(@decimal p1) { return; } }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("decimal", 0).Single().
                GetMembers("M").Single();

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "void M(@decimal p1)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, //M
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.RecordStructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, //p1
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
                SymbolDisplayPartKind.ClassName);
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
        public void TestNestedType1()
        {
            var text = @"
class C
{
    class D
    {
    }
}
";

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes | SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C").Single().GetTypeMember("D"),
                format,
                "C+D",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestNestedType2()
        {
            var text = @"
class C
{
    class D<T>
    {
    }
}
";

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes | SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C").Single().GetTypeMember("D"),
                format,
                "C+D`1",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                InternalSymbolDisplayPartKind.Arity);
        }

        [Fact]
        public void TestNestedType3()
        {
            var text = @"
class C<T>
{
    class D
    {
    }
}
";

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes | SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C").Single().GetTypeMember("D"),
                format,
                "C`1+D",
                SymbolDisplayPartKind.ClassName,
                InternalSymbolDisplayPartKind.Arity,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestNestedType4()
        {
            var text = @"
class C<T>
{
    class D<U, V>
    {
    }
}
";

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                compilerInternalOptions: SymbolDisplayCompilerInternalOptions.UseArityForGenericTypes | SymbolDisplayCompilerInternalOptions.UsePlusForNestedTypes);

            TestSymbolDescription(
                text,
                global => global.GetTypeMembers("C").Single().GetTypeMember("D"),
                format,
                "C`1+D`2",
                SymbolDisplayPartKind.ClassName,
                InternalSymbolDisplayPartKind.Arity,
                SymbolDisplayPartKind.Punctuation,
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

            // Without SymbolDisplayParameterOptions.IncludeParamsRefOut.
            TestSymbolDescription(
                text,
                findSymbol,
                format.WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName),
                "M(Int16 s, Int32 i, String[] args)",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            // Without SymbolDisplayParameterOptions.IncludeType, drops
            // ref/out/params modifiers. (VB retains ByRef/ParamArray.)
            TestSymbolDescription(
                text,
                findSymbol,
                format.WithParameterOptions(SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeName),
                "M(s, i, args)",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
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

        [WorkItem(537447, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537447")]
        [Fact]
        public void TestBug2239()
        {
            var text = @"
public class GC1<T> {}
public class X : GC1<BOGUS> {}
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetTypeMembers("X", 0).Single().
                BaseType();

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
using Goo = N1.N2.N3;

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
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Goo.C1.C2",
                text.IndexOf("namespace", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.AliasName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestAliases_AliasesNamedRecordAreEscaped()
        {
            var text = @"
using @record = N1;

namespace N1 {
    class C1 {} }
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetNestedNamespace("N1").
                GetTypeMembers("C1").Single();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces, miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "@record.C1",
                text.IndexOf("namespace", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.AliasName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void TestAlias2()
        {
            var text = @"
using Goo = N1.N2.N3.C1;

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
                "Goo.C2",
                text.IndexOf("namespace", StringComparison.Ordinal),
                true,
                SymbolDisplayPartKind.AliasName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void TestAlias3(string ob, string cb)
        {
            var text = @"
using Goo = N1.C1;

namespace N1 " + ob + @"
    class Goo { }
    class C1 { }
" + cb + @"
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
                text.IndexOf("class Goo", StringComparison.Ordinal),
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

        public class ScriptGlobals
        {
            public void Method(int p) { Event.Invoke(); }
            public delegate void MyDelegate(int x);
            public int Field;
            public int Property => 1;
            public event Action Event;

            public class NestedType
            {
                public void Method(int p) { Event.Invoke(); }
                public delegate void MyDelegate(int x);
                public int Field;
                public int Property => 1;
                public event Action Event;
            }
        }

        [Fact]
        public void TestMembersInScriptGlobals()
        {
            var text = @"1";
            var tree = SyntaxFactory.ParseSyntaxTree(text, TestOptions.Script);
            var hostReference = MetadataReference.CreateFromFile(typeof(ScriptGlobals).Assembly.Location);

            var comp = CSharpCompilation.CreateScriptCompilation(
                "submission1",
                tree,
                TargetFrameworkUtil.GetReferences(TargetFramework.Standard).Concat(hostReference),
                returnType: typeof(object),
                globalsType: typeof(ScriptGlobals));

            var model = comp.GetSemanticModel(tree);
            var hostTypeSymbol = comp.GetHostObjectTypeSymbol();

            var methodSymbol = hostTypeSymbol.GetMember("Method");
            var delegateSymbol = hostTypeSymbol.GetMember("MyDelegate");
            var fieldSymbol = hostTypeSymbol.GetMember("Field");
            var propertySymbol = hostTypeSymbol.GetMember("Property");
            var eventSymbol = hostTypeSymbol.GetMember("Event");

            var nestedTypeSymbol = (TypeSymbol)hostTypeSymbol.GetMember("NestedType");
            var nestedMethodSymbol = nestedTypeSymbol.GetMember("Method");
            var nestedDelegateSymbol = nestedTypeSymbol.GetMember("MyDelegate");
            var nestedFieldSymbol = nestedTypeSymbol.GetMember("Field");
            var nestedPropertySymbol = nestedTypeSymbol.GetMember("Property");
            var nestedEventSymbol = nestedTypeSymbol.GetMember("Event");

            Verify(methodSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "void Method(int p)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(delegateSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "MyDelegate(int x)",
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(fieldSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "int Field",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);

            Verify(propertySymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "int Property { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(eventSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "event System.Action Event",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EventName);

            Verify(nestedTypeSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "NestedType",
                SymbolDisplayPartKind.ClassName);

            Verify(nestedMethodSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "void NestedType.Method(int p)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(nestedDelegateSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "NestedType.MyDelegate(int x)",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(nestedFieldSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "int NestedType.Field",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName);

            Verify(nestedPropertySymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "int NestedType.Property { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(nestedEventSymbol.ToMinimalDisplayParts(model, position: 0, s_memberSignatureDisplayFormat),
                "event System.Action NestedType.Event",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);
        }

        private static readonly SymbolDisplayFormat s_memberSignatureDisplayFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeModifiers |
                    SymbolDisplayMemberOptions.IncludeRef |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                delegateStyle:
                    SymbolDisplayDelegateStyle.NameAndSignature,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword,
                propertyStyle:
                    SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeDefaultValue |
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets,
                localOptions:
                    SymbolDisplayLocalOptions.IncludeRef |
                    SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                    SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);

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
    private System.Collections.Generic.IDictionary<System.Collections.Generic.IList<System.Int32>, System.String> goo;
}
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                ((FieldSymbol)global.GetTypeMembers("C1").Single().GetMembers("goo").Single()).Type;

            var format = SymbolDisplayFormat.MinimallyQualifiedFormat;

            TestSymbolDescription(text, findSymbol, format,
                "IDictionary<IList<int>, string>",
                text.IndexOf("goo", StringComparison.Ordinal),
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
                    Net40.References.mscorlib
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
                Net40.References.mscorlib
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
                Net40.References.mscorlib
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
                Net40.References.mscorlib
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
            var comp = CreateCompilation(source);
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
            TestSymbolDescription(source, findSymbol, format, null, expectedText, expectedKinds);
        }

        private static void TestSymbolDescription(
            string source,
            Func<NamespaceSymbol, Symbol> findSymbol,
            SymbolDisplayFormat format,
            CSharpParseOptions parseOptions,
            string expectedText,
            params SymbolDisplayPartKind[] expectedKinds)
        {
            var comp = CreateCompilation(source, parseOptions: parseOptions);
            var global = comp.GlobalNamespace;
            var symbol = findSymbol(global);
            var description = symbol.ToDisplayParts(format);

            Verify(description, expectedText, expectedKinds);
        }

        private static void Verify(ImmutableArray<SymbolDisplayPart> actualParts, string expectedText, params SymbolDisplayPartKind[] expectedKinds)
        {
            AssertEx.Equal(expectedText, actualParts.ToDisplayString());
            if (expectedKinds.Length > 0)
            {
                AssertEx.Equal(expectedKinds, actualParts.Select(p => p.Kind), itemInspector: p => $"                SymbolDisplayPartKind.{p}");
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
                return field.Type;
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
                return field.Type;
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
                return field.Type;
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
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //|
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //|
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
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
        public void DefaultParameterValues_NullableEnum()
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
    void P(E? e = null, E? f = E.A, E? g = E.A | E.B, E?h = 0, E? i = (E)(-3))
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
                @"void S.P(E? e = null, E? f = E.A, E? g = E.A | E.B, E? h = E.C, E? i = (E)-3)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation, //(
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //?
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //?
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //?
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //|
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //?
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation, //=
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //.
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Punctuation, //,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation, //?
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
                SymbolDisplayPartKind.ConstantName,
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
                SymbolDisplayPartKind.ConstantName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EnumMemberName);
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
                SymbolDisplayPartKind.ConstantName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EnumMemberName);
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
                SymbolDisplayPartKind.EnumMemberName,
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
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EnumMemberName);
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
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral);
        }

        [Fact, WorkItem(545462, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545462")]
        public void DateTimeDefaultParameterValue()
        {
            var text = @"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class C
{
    static void Goo([Optional][DateTimeConstant(100)] DateTime d) { }
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("C").
                GetMember<MethodSymbol>("Goo");

            var format = new SymbolDisplayFormat(
                 memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                 parameterOptions: SymbolDisplayParameterOptions.IncludeType |
                                   SymbolDisplayParameterOptions.IncludeName |
                                   SymbolDisplayParameterOptions.IncludeDefaultValue);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "Goo(DateTime d)",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact, WorkItem(545681, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545681")]
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
            var complib = CreateCompilation(src1, assemblyName: "Lib");
            var compref = new CSharpCompilationReference(complib);
            var comp1 = CreateCompilation(src2, references: new MetadataReference[] { compref }, assemblyName: "Comp1");

            var mtdata = comp1.EmitToArray();
            var mtref = MetadataReference.CreateFromImage(mtdata);
            var comp2 = CreateCompilation("", references: new MetadataReference[] { mtref }, assemblyName: "Comp2");

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

        [Fact, WorkItem(545625, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545625")]
        public void ReverseArrayRankSpecifiers()
        {
            var text = @"
public class C
{
    C[][,] F;
}
";
            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.GetMember<NamedTypeSymbol>("C").GetMember<FieldSymbol>("F").Type;

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

        [Fact, WorkItem(546638, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546638")]
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
            var newCulture = (CultureInfo)CultureInfo.CurrentUICulture.Clone();
            newCulture.NumberFormat.NegativeSign = "~";
            newCulture.NumberFormat.NumberDecimalSeparator = ",";
            using (new CultureContext(newCulture))
            {
                var compilation = CreateCompilation(text);
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
        }

        [Fact]
        public void TestMethodVB()
        {
            var text = @"
Class A
   Public Sub Goo(a As Integer)
   End Sub
End Class";

            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var comp = CreateVisualBasicCompilation("c", text);
            var a = (ITypeSymbol)comp.GlobalNamespace.GetMembers("A").Single();
            var goo = a.GetMembers("Goo").Single();
            var parts = Microsoft.CodeAnalysis.CSharp.SymbolDisplay.ToDisplayParts(goo, format);

            Verify(
                parts,
                "public void Goo(int a)",
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

            var comp = CreateEmptyCompilation(source, WinRtRefs, TestOptions.ReleaseWinMD);
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

        [WorkItem(791756, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/791756")]
        [Theory]
        [MemberData(nameof(FileScopedOrBracedNamespace))]
        public void KindOptions(string ob, string cb)
        {
            var source = @"
namespace N
" + ob + @"
    class C
    {
        event System.Action E;
    }
" + cb + @"
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

            var comp = CreateCompilation(source);
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

        [WorkItem(765287, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/765287")]
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

            Assert.False(type is Symbol);
            Assert.False(method is Symbol);
            Assert.False(property is Symbol);
            Assert.False(field is Symbol);
            Assert.False(@event is Symbol);
            Assert.False(@delegate is Symbol);
            Assert.False(error is Symbol);

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
            Assert.Null(SymbolDisplay.FormatPrimitive(SymbolDisplayFormat.TestFormat, quoteStrings: false, useHexadecimalNumbers: false));
        }

        [WorkItem(879984, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/879984")]
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
            var comp = CreateCompilation(source);
            var method = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("Program").GetMember<MethodSymbol>("M");

            var memberFormat = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions: SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue);

            Assert.Equal("M(e1 = A, e2 = A)", method.ToDisplayString(memberFormat)); // Alphabetically first candidate chosen for both enums.
        }

        [Fact, WorkItem(1028003, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1028003")]
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
            var comp = CreateCompilationWithILAndMscorlib40(text, il);

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

        [WorkItem(6262, "https://github.com/dotnet/roslyn/issues/6262")]
        [Fact]
        public void FormattedSymbolEquality()
        {
            var source =
@"class A { }
class B { }
class C<T> { }";
            var compilation = CreateCompilation(source);
            var sA = compilation.GetMember<NamedTypeSymbol>("A");
            var sB = compilation.GetMember<NamedTypeSymbol>("B");
            var sC = compilation.GetMember<NamedTypeSymbol>("C");
            var f1 = new SymbolDisplayFormat();
            var f2 = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeParameters);

            Assert.False(new FormattedSymbol(sA, f1).Equals((object)sA));
            Assert.False(new FormattedSymbol(sA, f1).Equals(null));

            Assert.True(new FormattedSymbol(sA, f1).Equals(new FormattedSymbol(sA, f1)));
            Assert.False(new FormattedSymbol(sA, f1).Equals(new FormattedSymbol(sA, f2)));
            Assert.False(new FormattedSymbol(sA, f1).Equals(new FormattedSymbol(sB, f1)));
            Assert.False(new FormattedSymbol(sA, f1).Equals(new FormattedSymbol(sB, f2)));

            Assert.False(new FormattedSymbol(sC, f1).Equals(new FormattedSymbol(sC.Construct(sA), f1)));
            Assert.True(new FormattedSymbol(sC.Construct(sA), f1).Equals(new FormattedSymbol(sC.Construct(sA), f1)));

            Assert.False(new FormattedSymbol(sA, new SymbolDisplayFormat()).Equals(new FormattedSymbol(sA, new SymbolDisplayFormat())));

            Assert.True(new FormattedSymbol(sA, f1).GetHashCode().Equals(new FormattedSymbol(sA, f1).GetHashCode()));
        }

        [Fact, CompilerTrait(CompilerFeature.Tuples)]
        public void Tuple()
        {
            var text = @"
public class C
{
    public (int, string) f;
}
";
            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.
                GetTypeMembers("C").Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular,
                "(Int32, String) f",
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName, // Int32
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, // String
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact, CompilerTrait(CompilerFeature.Tuples)]
        public void TupleCollapseTupleTypes()
        {
            var text = @"
public class C
{
    public (int, string) f;
}
";
            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.
                GetTypeMembers("C").Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType, miscellaneousOptions: SymbolDisplayMiscellaneousOptions.CollapseTupleTypes);

            var comp = CreateCompilation(text, parseOptions: TestOptions.Regular);
            var global = comp.GlobalNamespace;
            var symbol = findSymbol(global);
            var description = symbol.ToDisplayParts(format);

            var firstPart = description[0];
            Assert.True(((ITypeSymbol)firstPart.Symbol).IsTupleType);
            Assert.Equal(SymbolDisplayPartKind.StructName, firstPart.Kind);

            Assert.Equal(SymbolDisplayPartKind.Space, description[1].Kind);
            Assert.Equal(SymbolDisplayPartKind.FieldName, description[2].Kind);
        }

        [WorkItem(18311, "https://github.com/dotnet/roslyn/issues/18311")]
        [Fact, CompilerTrait(CompilerFeature.Tuples)]
        public void TupleWith1Arity()
        {
            var text = @"
using System;
public class C
{
    public ValueTuple<int> f;
}
" + TestResources.NetFX.ValueTuple.tuplelib_cs;
            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.
                GetTypeMembers("C").Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType,
                                                 genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular,
                "ValueTuple<Int32> f",
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact, CompilerTrait(CompilerFeature.Tuples)]
        public void TupleWithNames()
        {
            var text = @"
public class C
{
    public (int x, string y) f;
}
";
            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.
                GetTypeMembers("C").Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular,
                "(Int32 x, String y) f",
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName, // Int32
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName, // x
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName, // String
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName, // y
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact, CompilerTrait(CompilerFeature.Tuples)]
        public void LongTupleWithSpecialTypes()
        {
            var text = @"
public class C
{
    public (int, string, bool, byte, long, ulong, short, ushort) f;
}
";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.
                GetTypeMembers("C").Single().
                GetMembers("f").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType,
                                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular,
                "(int, string, bool, byte, long, ulong, short, ushort) f",
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword, // int
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // string
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // bool
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // byte
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // long
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // ulong
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // short
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // ushort
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact, CompilerTrait(CompilerFeature.Tuples)]
        public void TupleProperty()
        {
            var text = @"
class C
{
   (int Item1, string Item2) P { get; set; }
}
";
            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                global.
                GetTypeMembers("C").Single().
                GetMembers("P").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType,
                                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular,
                "(int Item1, string Item2) P",
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword, // int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName, // Item1
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // string
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName, // Item2
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName);
        }

        [Fact, CompilerTrait(CompilerFeature.Tuples)]
        public void TupleQualifiedNames()
        {
            var text =
@"using NAB = N.A.B;
namespace N
{
    class A
    {
        internal class B {}
    }
    class C<T>
    {
        // offset 1
    }
}
class C
{
#pragma warning disable CS0169
   (int One, N.C<(object[], NAB Two)>, int, object Four, int, object, int, object, N.A Nine) f;
#pragma warning restore CS0169
    // offset 2
}";
            var format = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions: SymbolDisplayMemberOptions.IncludeType,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            var comp = (Compilation)CreateCompilationWithMscorlib46(text, references: new[] { SystemRuntimeFacadeRef, ValueTupleRef });
            comp.VerifyDiagnostics();
            var symbol = comp.GetMember("C.f");

            // Fully qualified format.
            Verify(
                SymbolDisplay.ToDisplayParts(symbol, format),
                "(int One, global::N.C<(object[], global::N.A.B Two)>, int, object Four, int, object, int, object, global::N.A Nine) f");

            // Minimally qualified format.
            Verify(
                SymbolDisplay.ToDisplayParts(symbol, SymbolDisplayFormat.MinimallyQualifiedFormat),
                "(int One, C<(object[], B Two)>, int, object Four, int, object, int, object, A Nine) C.f");

            // ToMinimalDisplayParts.
            var model = comp.GetSemanticModel(comp.SyntaxTrees.First());
            Verify(
                SymbolDisplay.ToMinimalDisplayParts(symbol, model, text.IndexOf("offset 1"), format),
                "(int One, C<(object[], NAB Two)>, int, object Four, int, object, int, object, A Nine) f");
            Verify(
                SymbolDisplay.ToMinimalDisplayParts(symbol, model, text.IndexOf("offset 2"), format),
                "(int One, N.C<(object[], NAB Two)>, int, object Four, int, object, int, object, N.A Nine) f");
        }

        [Fact]
        [WorkItem(23970, "https://github.com/dotnet/roslyn/pull/23970")]
        public void ThisDisplayParts()
        {
            var text =
@"
class A
{
    void M(int @this)
    {
        this.M(@this);
    }
}";
            var comp = CreateCompilation(text);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
            Assert.Equal("this.M(@this)", invocation.ToString());

            var actualThis = ((MemberAccessExpressionSyntax)invocation.Expression).Expression;
            Assert.Equal("this", actualThis.ToString());

            Verify(
                SymbolDisplay.ToDisplayParts(model.GetSymbolInfo(actualThis).Symbol, SymbolDisplayFormat.MinimallyQualifiedFormat),
                "A this",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword);

            var escapedThis = invocation.ArgumentList.Arguments[0].Expression;
            Assert.Equal("@this", escapedThis.ToString());

            Verify(
                SymbolDisplay.ToDisplayParts(model.GetSymbolInfo(escapedThis).Symbol, SymbolDisplayFormat.MinimallyQualifiedFormat),
                "int @this",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName);
        }

        [WorkItem(11356, "https://github.com/dotnet/roslyn/issues/11356")]
        [Fact]
        public void RefReturn()
        {
            var sourceA =
@"public delegate ref int D();
public class C
{
    public ref int F(ref int i) => ref i;
    int _p;
    public ref int P => ref _p;
    public ref int this[int i] => ref _p;
}";
            var compA = CreateEmptyCompilation(sourceA, new[] { MscorlibRef });
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();
            // From C# symbols.
            RefReturnInternal(compA);

            var compB = CreateVisualBasicCompilation(GetUniqueName(), "", referencedAssemblies: new[] { MscorlibRef, refA });
            compB.VerifyDiagnostics();
            // From VB symbols.
            RefReturnInternal(compB);
        }

        private static void RefReturnInternal(Compilation comp)
        {
            var formatBase = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            var formatWithoutRef = formatBase.WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType);
            var formatWithRef = formatBase.WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeRef);
            var formatWithoutTypeWithRef = formatBase.WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeRef);

            var global = comp.GlobalNamespace;
            var type = global.GetTypeMembers("C").Single();
            var method = type.GetMembers("F").Single();
            var property = type.GetMembers("P").Single();
            var indexer = type.GetMembers().Where(m => m.Kind == SymbolKind.Property && ((IPropertySymbol)m).IsIndexer).Single();
            var @delegate = global.GetTypeMembers("D").Single();

            // Method without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutRef),
                "int F(ref int)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            // Property without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(property, formatWithoutRef),
                "int P { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Indexer without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(indexer, formatWithoutRef),
                "int this[int] { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Delegate without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(@delegate, formatWithoutRef),
                "int D()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);

            // Method with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithRef),
                "ref int F(ref int)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            // Property with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(property, formatWithRef),
                "ref int P { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Indexer with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(indexer, formatWithRef),
                "ref int this[int] { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Delegate with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(@delegate, formatWithRef),
                "ref int D()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);

            // Method without IncludeType, with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutTypeWithRef),
                "F(ref int)",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void RefReadonlyReturn()
        {
            var sourceA =
@"public delegate ref readonly int D();
public class C
{
    public ref readonly int F(in int i) => ref i;
    int _p;
    public ref readonly int P => ref _p;
    public ref readonly int this[in int i] => ref _p;
}";
            var compA = CreateCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();
            // From C# symbols.
            RefReadonlyReturnInternal(compA);

            var compB = CreateVisualBasicCompilation(GetUniqueName(), "", referencedAssemblies: new[] { MscorlibRef, refA });
            compB.VerifyDiagnostics();
            // From VB symbols.
            //RefReadonlyReturnInternal(compB);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void RefReadonlyReturn1()
        {
            var sourceA =
@"public delegate ref readonly int D();
public class C
{
    public ref readonly int F(in int i) => ref i;
    int _p;
    public ref readonly int P => ref _p;
    public ref readonly int this[in int i] => ref _p;
}";
            var compA = CreateCompilation(sourceA);
            compA.VerifyDiagnostics();
            var refA = compA.EmitToImageReference();
            // From C# symbols.
            RefReadonlyReturnInternal(compA);

            var compB = CreateVisualBasicCompilation(GetUniqueName(), "", referencedAssemblies: new[] { MscorlibRef, refA });
            compB.VerifyDiagnostics();
            // From VB symbols.
            //RefReadonlyReturnInternal(compB);
        }

        private static void RefReadonlyReturnInternal(Compilation comp)
        {
            var formatBase = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            var formatWithoutRef = formatBase.WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType);
            var formatWithRef = formatBase.WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeRef);
            var formatWithoutTypeWithRef = formatBase.WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeRef);

            var global = comp.GlobalNamespace;
            var type = global.GetTypeMembers("C").Single();
            var method = type.GetMembers("F").Single();
            var property = type.GetMembers("P").Single();
            var indexer = type.GetMembers().Where(m => m.Kind == SymbolKind.Property && ((IPropertySymbol)m).IsIndexer).Single();
            var @delegate = global.GetTypeMembers("D").Single();

            // Method without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutRef),
                "int F(in int)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            // Property without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(property, formatWithoutRef),
                "int P { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Indexer without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(indexer, formatWithoutRef),
                "int this[in int] { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Delegate without IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(@delegate, formatWithoutRef),
                "int D()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);

            // Method with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithRef),
                "ref readonly int F(in int)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            // Property with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(property, formatWithRef),
                "ref readonly int P { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Indexer with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(indexer, formatWithRef),
                "ref readonly int this[in int] { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            // Delegate with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(@delegate, formatWithRef),
                "ref readonly int D()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);

            // Method without IncludeType, with IncludeRef.
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutTypeWithRef),
                "F(in int)",
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);
        }

        [WorkItem(5002, "https://github.com/dotnet/roslyn/issues/5002")]
        [Fact]
        public void AliasInSpeculativeSemanticModel()
        {
            var text =
@"using A = N.M;
namespace N.M
{
    class B
    {
    }
}
class C
{
    static void M()
    {
    }
}";
            var comp = CreateCompilation(text);
            var tree = comp.SyntaxTrees.First();
            var model = comp.GetSemanticModel(tree);
            var methodDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            int position = methodDecl.Body.SpanStart;

            tree = CSharpSyntaxTree.ParseText(@"
class C
{
    static void M()
    {
    }
}");
            methodDecl = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Assert.True(model.TryGetSpeculativeSemanticModelForMethodBody(position, methodDecl, out model));
            var symbol = comp.GetMember<NamedTypeSymbol>("N.M.B");
            position = methodDecl.Body.SpanStart;
            var description = symbol.ToMinimalDisplayParts(model, position, SymbolDisplayFormat.MinimallyQualifiedFormat);
            Verify(description, "A.B", SymbolDisplayPartKind.AliasName, SymbolDisplayPartKind.Punctuation, SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void NullableReferenceTypes()
        {
            var source = @"
class A<T>
{
}
class B
{
    static object F1(object? o) => null!;
    static object?[] F2(object[]? o) => null;
    static A<object>? F3(A<object?> o) => null;
}";
            var comp = (Compilation)CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular8, options: WithNullableEnable());
            var formatWithoutNonNullableModifier = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeModifiers,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            var formatWithNonNullableModifier = formatWithoutNonNullableModifier
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier)
                .WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.None);

            var method = comp.GetMember<IMethodSymbol>("B.F1");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutNonNullableModifier),
                "static object F1(object? o)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithNonNullableModifier),
                "static object! F1(object? o)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            method = comp.GetMember<IMethodSymbol>("B.F2");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutNonNullableModifier),
                "static object?[] F2(object[]? o)");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithNonNullableModifier),
                "static object?[]! F2(object![]? o)");

            method = comp.GetMember<IMethodSymbol>("B.F3");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutNonNullableModifier),
                "static A<object>? F3(A<object?> o)");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithNonNullableModifier),
                "static A<object!>? F3(A<object?>! o)");
        }

        [Fact]
        public void NullableReferenceTypes2()
        {
            var source =
@"class A<T>
{
}
class B
{
    static object F1(object? o) => null!;
    static object?[] F2(object[]? o) => null;
    static A<object>? F3(A<object?> o) => null;
}";
            var comp = (Compilation)CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var formatWithoutNullableModifier = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeModifiers,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var formatWithNullableModifier = formatWithoutNullableModifier
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier)
                .WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.None);

            var method = comp.GetMember<IMethodSymbol>("B.F1");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutNullableModifier),
                "static object F1(object o)",
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
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithNullableModifier),
                "static object F1(object? o)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            method = comp.GetMember<IMethodSymbol>("B.F2");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutNullableModifier),
                "static object[] F2(object[] o)");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithNullableModifier),
                "static object?[] F2(object[]? o)");

            method = comp.GetMember<IMethodSymbol>("B.F3");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutNullableModifier),
                "static A<object> F3(A<object> o)");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithNullableModifier),
                "static A<object>? F3(A<object?> o)");
        }

        [WorkItem(31700, "https://github.com/dotnet/roslyn/issues/31700")]
        [Fact]
        public void NullableArrays()
        {
            var source =
@"#nullable enable
class C
{
    static object?[,][] F1;
    static object[,]?[] F2;
    static object[,][]? F3;
}";
            var comp = (Compilation)CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var formatWithoutModifiers = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeModifiers,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            var formatWithNullableModifier = formatWithoutModifiers.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
            var formatWithBothModifiers = formatWithNullableModifier.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier);

            var member = comp.GetMember("C.F1");
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithoutModifiers),
                "static object[,][] F1",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithNullableModifier),
                "static object?[,][] F1",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithBothModifiers),
                "static object?[]![,]! F1",
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
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.FieldName);

            member = comp.GetMember("C.F2");
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithoutModifiers),
                "static object[][,] F2");
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithNullableModifier),
                "static object[,]?[] F2");
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithBothModifiers),
                "static object![,]?[]! F2");

            member = comp.GetMember("C.F3");
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithoutModifiers),
                "static object[,][] F3");
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithNullableModifier),
                "static object[,][]? F3");
            Verify(
                SymbolDisplay.ToDisplayParts(member, formatWithBothModifiers),
                "static object![]![,]? F3");
        }

        [Fact]
        public void AllowDefaultLiteral()
        {
            var source =
@"using System.Threading;
class C
{
    void Method(CancellationToken cancellationToken = default(CancellationToken)) => throw null;
}
";

            var compilation = (Compilation)CreateCompilation(source);
            var formatWithoutAllowDefaultLiteral = SymbolDisplayFormat.MinimallyQualifiedFormat;
            Assert.False(formatWithoutAllowDefaultLiteral.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral));
            var formatWithAllowDefaultLiteral = formatWithoutAllowDefaultLiteral.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);
            Assert.True(formatWithAllowDefaultLiteral.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral));

            var method = compilation.GetMember<IMethodSymbol>("C.Method");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutAllowDefaultLiteral),
                "void C.Method(CancellationToken cancellationToken = default(CancellationToken))");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithAllowDefaultLiteral),
                "void C.Method(CancellationToken cancellationToken = default)");
        }

        [Fact]
        public void TypeParameterAnnotations_01()
        {
            var source =
@"#nullable enable
class C
{
    T F0<T>() => default;
    T? F1<T>() => default;
    T F2<T>() where T : class => default;
    T? F3<T>() where T : class => default;
    T F4<T>() where T : class? => default;
    T? F5<T>() where T : class? => default;
    T F6<T>() where T : struct => default;
    T? F7<T>() where T : struct => default;
    T F8<T>() where T : notnull => default;
    T F9<T>() where T : unmanaged => default;
}";
            var comp = (Compilation)CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var formatWithoutModifiers = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
            var formatWithNullableModifier = formatWithoutModifiers.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
            var formatWithBothModifiers = formatWithNullableModifier.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNotNullableReferenceTypeModifier);

            verify("C.F0", "T F0<T>()", "T F0<T>()", "T F0<T>()");
            verify("C.F1", "T F1<T>()", "T? F1<T>()", "T? F1<T>()");
            verify("C.F2", "T F2<T>() where T : class", "T F2<T>() where T : class", "T! F2<T>() where T : class!");
            verify("C.F3", "T F3<T>() where T : class", "T? F3<T>() where T : class", "T? F3<T>() where T : class!");
            verify("C.F4", "T F4<T>() where T : class", "T F4<T>() where T : class?", "T F4<T>() where T : class?");
            verify("C.F5", "T F5<T>() where T : class", "T? F5<T>() where T : class?", "T? F5<T>() where T : class?");
            verify("C.F6", "T F6<T>() where T : struct", "T F6<T>() where T : struct", "T F6<T>() where T : struct");
            verify("C.F7", "T? F7<T>() where T : struct", "T? F7<T>() where T : struct", "T? F7<T>() where T : struct");
            verify("C.F8", "T F8<T>() where T : notnull", "T F8<T>() where T : notnull", "T F8<T>() where T : notnull");
            verify("C.F9", "T F9<T>() where T : unmanaged", "T F9<T>() where T : unmanaged", "T F9<T>() where T : unmanaged");

            void verify(string memberName, string withoutModifiers, string withNullableModifier, string withBothModifiers)
            {
                var member = comp.GetMember(memberName);
                Verify(SymbolDisplay.ToDisplayParts(member, formatWithoutModifiers), withoutModifiers);
                Verify(SymbolDisplay.ToDisplayParts(member, formatWithNullableModifier), withNullableModifier);
                Verify(SymbolDisplay.ToDisplayParts(member, formatWithBothModifiers), withBothModifiers);
            }
        }

        [Fact]
        public void TypeParameterAnnotations_02()
        {
            var source =
@"#nullable enable
interface I<T> { }
class C
{
    T? F<T>(T?[] x, I<T?> y) => default;
}";
            var comp = (Compilation)CreateCompilation(source, parseOptions: TestOptions.Regular8);
            var format = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            var method = (IMethodSymbol)comp.GetMember("C.F");
            Verify(
                SymbolDisplay.ToDisplayParts(method, format),
                "T? F<T>(T?[] x, I<T?> y)",
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.InterfaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            var type = method.GetSymbol<MethodSymbol>().ReturnTypeWithAnnotations;
            Assert.Equal("T?", type.ToDisplayString(format));
        }

        [Theory]
        [InlineData("int", "0")]
        [InlineData("string", "null")]
        public void AllowDefaultLiteralNotNeeded(string type, string defaultValue)
        {
            var source =
$@"
class C
{{
    void Method1({type} parameter = {defaultValue}) => throw null;
    void Method2({type} parameter = default({type})) => throw null;
    void Method3({type} parameter = default) => throw null;
}}
";

            var compilation = (Compilation)CreateCompilation(source);
            var formatWithoutAllowDefaultLiteral = SymbolDisplayFormat.MinimallyQualifiedFormat;
            Assert.False(formatWithoutAllowDefaultLiteral.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral));
            var formatWithAllowDefaultLiteral = formatWithoutAllowDefaultLiteral.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);
            Assert.True(formatWithAllowDefaultLiteral.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral));

            var method1 = compilation.GetMember<IMethodSymbol>("C.Method1");
            Verify(
                SymbolDisplay.ToDisplayParts(method1, formatWithoutAllowDefaultLiteral),
                $"void C.Method1({type} parameter = {defaultValue})");
            Verify(
                SymbolDisplay.ToDisplayParts(method1, formatWithAllowDefaultLiteral),
                $"void C.Method1({type} parameter = {defaultValue})");

            var method2 = compilation.GetMember<IMethodSymbol>("C.Method2");
            Verify(
                SymbolDisplay.ToDisplayParts(method2, formatWithoutAllowDefaultLiteral),
                $"void C.Method2({type} parameter = {defaultValue})");
            Verify(
                SymbolDisplay.ToDisplayParts(method2, formatWithAllowDefaultLiteral),
                $"void C.Method2({type} parameter = {defaultValue})");

            var method3 = compilation.GetMember<IMethodSymbol>("C.Method3");
            Verify(
                SymbolDisplay.ToDisplayParts(method3, formatWithoutAllowDefaultLiteral),
                $"void C.Method3({type} parameter = {defaultValue})");
            Verify(
                SymbolDisplay.ToDisplayParts(method3, formatWithAllowDefaultLiteral),
                $"void C.Method3({type} parameter = {defaultValue})");
        }

        [Theory]
        [InlineData("int", "2")]
        [InlineData("string", "\"value\"")]
        public void AllowDefaultLiteralNotApplicable(string type, string defaultValue)
        {
            var source =
$@"
class C
{{
    void Method({type} parameter = {defaultValue}) => throw null;
}}
";

            var compilation = (Compilation)CreateCompilation(source);
            var formatWithoutAllowDefaultLiteral = SymbolDisplayFormat.MinimallyQualifiedFormat;
            Assert.False(formatWithoutAllowDefaultLiteral.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral));
            var formatWithAllowDefaultLiteral = formatWithoutAllowDefaultLiteral.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral);
            Assert.True(formatWithAllowDefaultLiteral.MiscellaneousOptions.IncludesOption(SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral));

            var method = compilation.GetMember<IMethodSymbol>("C.Method");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutAllowDefaultLiteral),
                $"void C.Method({type} parameter = {defaultValue})");
            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithAllowDefaultLiteral),
                $"void C.Method({type} parameter = {defaultValue})");
        }

        [Fact]
        public void UseLongHandValueTuple()
        {
            var source =
@"
class B
{
    static (int, (string, long)) F1((int, int)[] t) => throw null;
}";
            var comp = (Compilation)CreateCompilation(source);
            var formatWithoutLongHandValueTuple = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeModifiers,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var formatWithLongHandValueTuple = formatWithoutLongHandValueTuple.AddMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

            var method = comp.GetMember<IMethodSymbol>("B.F1");

            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithoutLongHandValueTuple),
                "static (int, (string, long)) F1((int, int)[] t)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(
                SymbolDisplay.ToDisplayParts(method, formatWithLongHandValueTuple),
                "static ValueTuple<int, ValueTuple<string, long>> F1(ValueTuple<int, int>[] t)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public void LocalFunction()
        {
            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        void Local() {}
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilation(srcTree);

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var local = root.DescendantNodes()
                .Where(n => n.Kind() == SyntaxKind.LocalFunctionStatement)
                .Single();
            var localSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(local);

            Assert.Equal(MethodKind.LocalFunction, localSymbol.MethodKind);

            Verify(localSymbol.ToDisplayParts(SymbolDisplayFormat.TestFormat),
                "void Local()",
                SymbolDisplayPartKind.Keyword, // void
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // Local
                SymbolDisplayPartKind.Punctuation, // (
                SymbolDisplayPartKind.Punctuation); // )
        }

        [Fact]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public void LocalFunctionForChangeSignature()
        {
            SymbolDisplayFormat changeSignatureFormat = new SymbolDisplayFormat(
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
                    extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                    memberOptions:
                        SymbolDisplayMemberOptions.IncludeType |
                        SymbolDisplayMemberOptions.IncludeExplicitInterface |
                        SymbolDisplayMemberOptions.IncludeAccessibility |
                        SymbolDisplayMemberOptions.IncludeModifiers |
                        SymbolDisplayMemberOptions.IncludeRef);

            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        void Local() {}
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilation(srcTree);

            var semanticModel = comp.GetSemanticModel(srcTree);
            var local = root.DescendantNodes()
                .Where(n => n.Kind() == SyntaxKind.LocalFunctionStatement)
                .Single();
            var localSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(local);

            Assert.Equal(MethodKind.LocalFunction, localSymbol.MethodKind);

            Verify(localSymbol.ToDisplayParts(changeSignatureFormat),
                "void Local",
                SymbolDisplayPartKind.Keyword, // void
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName // Local
                );
        }

        [Fact]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public void LocalFunction2()
        {
            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
using System.Threading.Tasks;
class C
{
    void M()
    {
        async unsafe Task<int> Local(ref int* x, out char? c)
        {
        }
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilationWithMscorlib461(new[] { srcTree });

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var local = root.DescendantNodes()
                .Where(n => n.Kind() == SyntaxKind.LocalFunctionStatement)
                .Single();

            var localSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(local);

            Assert.Equal(MethodKind.LocalFunction, localSymbol.MethodKind);

            Verify(localSymbol.ToDisplayParts(SymbolDisplayFormat.TestFormat),
                "System.Threading.Tasks.Task<System.Int32> Local(ref System.Int32* x, out System.Char? c)",
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.NamespaceName, // Threading
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.NamespaceName, // Tasks
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.ClassName, // Task
                SymbolDisplayPartKind.Punctuation, // <
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.StructName, // Int32
                SymbolDisplayPartKind.Punctuation, // >
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // Local
                SymbolDisplayPartKind.Punctuation, // (
                SymbolDisplayPartKind.Keyword, // ref
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.StructName, // Int32
                SymbolDisplayPartKind.Punctuation, // *
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // x
                SymbolDisplayPartKind.Punctuation, // ,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // out
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.StructName, // Char
                SymbolDisplayPartKind.Punctuation, // ?
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // c
                SymbolDisplayPartKind.Punctuation); // )
        }

        [Fact]
        public void RangeVariable()
        {
            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
using System.Linq;
class C
{
    void M()
    {
        var q = from x in new[] { 1, 2, 3 } where x < 3 select x;
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilation(srcTree);

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var queryExpression = root.DescendantNodes().OfType<QueryExpressionSyntax>().First();
            var fromClauseRangeVariableSymbol = (IRangeVariableSymbol)semanticModel.GetDeclaredSymbol(queryExpression.FromClause);

            Verify(
                fromClauseRangeVariableSymbol.ToMinimalDisplayParts(
                    semanticModel,
                    queryExpression.FromClause.Identifier.SpanStart,
                    SymbolDisplayFormat.MinimallyQualifiedFormat),
                "int x",
                SymbolDisplayPartKind.Keyword, //int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.RangeVariableName); // x
        }

        [Fact]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public void LocalFunction3()
        {
            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
using System.Threading.Tasks;
class C
{
    void M()
    {
        async unsafe Task<int> Local(in int* x, out char? c)
        {
        }
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilationWithMscorlib461(new[] { srcTree });

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var local = root.DescendantNodes()
                .Where(n => n.Kind() == SyntaxKind.LocalFunctionStatement)
                .Single();
            var localSymbol = (IMethodSymbol)semanticModel.GetDeclaredSymbol(local);

            Assert.Equal(MethodKind.LocalFunction, localSymbol.MethodKind);

            Verify(localSymbol.ToDisplayParts(SymbolDisplayFormat.TestFormat),
                "System.Threading.Tasks.Task<System.Int32> Local(in System.Int32* x, out System.Char? c)",
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.NamespaceName, // Threading
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.NamespaceName, // Tasks
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.ClassName, // Task
                SymbolDisplayPartKind.Punctuation, // <
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.StructName, // Int32
                SymbolDisplayPartKind.Punctuation, // >
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName, // Local
                SymbolDisplayPartKind.Punctuation, // (
                SymbolDisplayPartKind.Keyword, // in
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.StructName, // Int32
                SymbolDisplayPartKind.Punctuation, // *
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // x
                SymbolDisplayPartKind.Punctuation, // ,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, // out
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName, // System
                SymbolDisplayPartKind.Punctuation, // .
                SymbolDisplayPartKind.StructName, // Char
                SymbolDisplayPartKind.Punctuation, // ?
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName, // c
                SymbolDisplayPartKind.Punctuation); // )
        }

        [Fact]
        public void LocalVariable_01()
        {
            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        int x = 0;
        x++;
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilation(srcTree);

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var declarator = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator);

            Verify(
                local.ToMinimalDisplayParts(
                    semanticModel,
                    declarator.SpanStart,
                    SymbolDisplayFormat.MinimallyQualifiedFormat.AddLocalOptions(SymbolDisplayLocalOptions.IncludeRef)),
                "int x",
                SymbolDisplayPartKind.Keyword, //int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName); // x

            Assert.False(local.IsRef);
            Assert.Equal(RefKind.None, local.RefKind);
        }

        [Fact]
        public void LocalVariable_02()
        {
            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M(int y)
    {
        ref int x = y;
        x++;
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilation(srcTree);

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var declarator = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator);

            Verify(
                local.ToMinimalDisplayParts(
                    semanticModel,
                    declarator.SpanStart,
                    SymbolDisplayFormat.MinimallyQualifiedFormat.AddLocalOptions(SymbolDisplayLocalOptions.IncludeRef)),
                "ref int x",
                SymbolDisplayPartKind.Keyword, //ref
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, //int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName); // x

            Verify(
                local.ToMinimalDisplayParts(
                    semanticModel,
                    declarator.SpanStart,
                    SymbolDisplayFormat.MinimallyQualifiedFormat),
                "int x",
                SymbolDisplayPartKind.Keyword, //int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName); // x

            Assert.True(local.IsRef);
            Assert.Equal(RefKind.Ref, local.RefKind);
        }

        [Fact]
        public void LocalVariable_03()
        {
            var srcTree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M(int y)
    {
        ref readonly int x = y;
        x++;
    }
}");
            var root = srcTree.GetRoot();
            var comp = CreateCompilation(srcTree);

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var declarator = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single();
            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator);

            Verify(
                local.ToMinimalDisplayParts(
                    semanticModel,
                    declarator.SpanStart,
                    SymbolDisplayFormat.MinimallyQualifiedFormat.AddLocalOptions(SymbolDisplayLocalOptions.IncludeRef)),
                "ref readonly int x",
                SymbolDisplayPartKind.Keyword, //ref
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, //readonly
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword, //int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName); // x

            Verify(
                local.ToMinimalDisplayParts(
                    semanticModel,
                    declarator.SpanStart,
                    SymbolDisplayFormat.MinimallyQualifiedFormat),
                "int x",
                SymbolDisplayPartKind.Keyword, //int
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName); // x

            Assert.True(local.IsRef);
            Assert.Equal(RefKind.RefReadOnly, local.RefKind);
        }

        [Fact]
        [WorkItem(22507, "https://github.com/dotnet/roslyn/issues/22507")]
        public void EdgeCasesForEnumFieldComparer()
        {
            // A bad comparer could cause sorting the enum fields
            // to throw an exception due to inconsistency. See Repro22507
            // for an example of this problem.

            var lhs = new EnumField("E1", 0);
            var rhs = new EnumField("E2", 0x1000_0000_0000_0000);

            // This is a "reverse" comparer, so if lhs < rhs, return
            // value should be > 0
            // If the comparer subtracts and converts, result will be zero since
            // the bottom 32 bits are zero
            Assert.InRange(EnumField.Comparer.Compare(lhs, rhs), 1, int.MaxValue);

            lhs = new EnumField("E1", 0);
            rhs = new EnumField("E2", 0x1000_0000_0000_0001);
            Assert.InRange(EnumField.Comparer.Compare(lhs, rhs), 1, int.MaxValue);

            lhs = new EnumField("E1", 0x1000_0000_0000_000);
            rhs = new EnumField("E2", 0);
            Assert.InRange(EnumField.Comparer.Compare(lhs, rhs), int.MinValue, -1);

            lhs = new EnumField("E1", 0);
            rhs = new EnumField("E2", 0x1000_0000_8000_0000);
            Assert.InRange(EnumField.Comparer.Compare(lhs, rhs), 1, int.MaxValue);
        }

        [Fact]
        [WorkItem(22507, "https://github.com/dotnet/roslyn/issues/22507")]
        public void Repro22507()
        {
            var text = @"
using System;

[Flags]
enum E : long
{
    A = 0x0,
    B = 0x400,
    C = 0x100000,
    D = 0x200000,
    E = 0x2000000,
    F = 0x4000000,
    G = 0x8000000,
    H = 0x40000000,
    I = 0x80000000,
    J = 0x20000000000,
    K = 0x40000000000,
    L = 0x4000000000000,
    M = 0x8000000000000,
    N = 0x10000000000000,
    O = 0x20000000000000,
    P = 0x40000000000000,
    Q = 0x2000000000000000,
}
";
            TestSymbolDescription(
                text,
                g => g.GetTypeMembers("E").Single().GetField("A"),
                SymbolDisplayFormat.MinimallyQualifiedFormat
                    .AddMemberOptions(SymbolDisplayMemberOptions.IncludeConstantValue),
                "E.A = 0",
                SymbolDisplayPartKind.EnumName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EnumMemberName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NumericLiteral);
        }

        [Fact]
        public void TestRefStructs()
        {
            var source = @"
ref struct X { }
namespace Nested
{
    ref struct Y { }
}
";

            var comp = CreateCompilation(source).VerifyDiagnostics();
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var declarations = semanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => n.Kind() == SyntaxKind.StructDeclaration).Cast<BaseTypeDeclarationSyntax>().ToArray();
            Assert.Equal(2, declarations.Length);

            var format = SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword);

            Verify(semanticModel.GetDeclaredSymbol(declarations[0]).ToDisplayParts(format),
                "ref struct X",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName);

            Verify(semanticModel.GetDeclaredSymbol(declarations[1]).ToDisplayParts(format),
                "ref struct Nested.Y",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName);
        }

        [Fact]
        public void TestReadOnlyStructs()
        {
            var source = @"
readonly struct X { }
namespace Nested
{
    readonly struct Y { }
}
";

            var comp = CreateCompilation(source).VerifyDiagnostics();
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var declarations = semanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => n.Kind() == SyntaxKind.StructDeclaration).Cast<BaseTypeDeclarationSyntax>().ToArray();
            Assert.Equal(2, declarations.Length);

            var format = SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword);

            Verify(semanticModel.GetDeclaredSymbol(declarations[0]).ToDisplayParts(format),
                "readonly struct X",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName);

            Verify(semanticModel.GetDeclaredSymbol(declarations[1]).ToDisplayParts(format),
                "readonly struct Nested.Y",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName);
        }

        [Fact]
        public void TestReadOnlyRefStructs()
        {
            var source = @"
readonly ref struct X { }
namespace Nested
{
    readonly ref struct Y { }
}
";

            var comp = CreateCompilation(source).VerifyDiagnostics();
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var declarations = semanticModel.SyntaxTree.GetRoot().DescendantNodes().Where(n => n.Kind() == SyntaxKind.StructDeclaration).Cast<BaseTypeDeclarationSyntax>().ToArray();
            Assert.Equal(2, declarations.Length);

            var format = SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword);

            Verify(semanticModel.GetDeclaredSymbol(declarations[0]).ToDisplayParts(format),
                "readonly ref struct X",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName);

            Verify(semanticModel.GetDeclaredSymbol(declarations[1]).ToDisplayParts(format),
                "readonly ref struct Nested.Y",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName);
        }

        [Fact]
        public void TestReadOnlyMembers_Malformed()
        {
            var source = @"
struct X
{
    int P1 { }
    readonly int P2 { }
    readonly event System.Action E1 { }
    readonly event System.Action E2 { remove { } }
}
";
            var format = SymbolDisplayFormat.TestFormat
                .AddMemberOptions(SymbolDisplayMemberOptions.IncludeModifiers)
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var comp = CreateCompilation(source).VerifyDiagnostics(
                // (4,9): error CS0548: 'X.P1': property or indexer must have at least one accessor
                //     int P1 { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P1").WithArguments("X.P1").WithLocation(4, 9),
                // (5,18): error CS0548: 'X.P2': property or indexer must have at least one accessor
                //     readonly int P2 { }
                Diagnostic(ErrorCode.ERR_PropertyWithNoAccessors, "P2").WithArguments("X.P2").WithLocation(5, 18),
                // (6,34): error CS0065: 'X.E1': event property must have both add and remove accessors
                //     readonly event System.Action E1 { }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E1").WithArguments("X.E1").WithLocation(6, 34),
                // (7,34): error CS0065: 'X.E2': event property must have both add and remove accessors
                //     readonly event System.Action E2 { remove { } }
                Diagnostic(ErrorCode.ERR_EventNeedsBothAccessors, "E2").WithArguments("X.E2").WithLocation(7, 34));

            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var declaration = (BaseTypeDeclarationSyntax)semanticModel.SyntaxTree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.StructDeclaration);
            var members = semanticModel.GetDeclaredSymbol(declaration).GetMembers();

            Verify(members[0].ToDisplayParts(format), "int X.P1 { }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[1].ToDisplayParts(format), "int X.P2 { }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[2].ToDisplayParts(format), "event System.Action X.E1",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);

            Verify(members[3].ToDisplayParts(format), "readonly event System.Action X.E2",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);
        }

        [Fact]
        public void TestReadOnlyMembers()
        {
            var source = @"
struct X
{
    readonly void M() { }
    readonly int P1 { get => 123; }
    readonly int P2 { set {} }
    readonly int P3 { get => 123; set {} }
    int P4 { readonly get => 123; set {} }
    int P5 { get => 123; readonly set {} }
    readonly event System.Action E { add {} remove {} }
}
";
            var format = SymbolDisplayFormat.TestFormat
                .AddMemberOptions(SymbolDisplayMemberOptions.IncludeModifiers)
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var comp = CreateCompilation(source).VerifyDiagnostics();
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var declaration = (BaseTypeDeclarationSyntax)semanticModel.SyntaxTree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.StructDeclaration);
            var members = semanticModel.GetDeclaredSymbol(declaration).GetMembers();

            Verify(members[0].ToDisplayParts(format),
                "readonly void X.M()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[1].ToDisplayParts(format),
                "readonly int X.P1 { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[2].ToDisplayParts(format),
                "readonly int X.P1.get",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[3].ToDisplayParts(format),
                "readonly int X.P2 { set; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[4].ToDisplayParts(format),
                "readonly void X.P2.set",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[5].ToDisplayParts(format),
                "readonly int X.P3 { get; set; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
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

            Verify(members[6].ToDisplayParts(format),
                "readonly int X.P3.get",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[7].ToDisplayParts(format),
                "readonly void X.P3.set",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[8].ToDisplayParts(format),
                "int X.P4 { readonly get; set; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[9].ToDisplayParts(format),
                "readonly int X.P4.get",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[10].ToDisplayParts(format),
                "void X.P4.set",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[11].ToDisplayParts(format),
                "int X.P5 { get; readonly set; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[12].ToDisplayParts(format),
                "int X.P5.get",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[13].ToDisplayParts(format),
                "readonly void X.P5.set",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[14].ToDisplayParts(format),
                "readonly event System.Action X.E",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);

            Verify(members[15].ToDisplayParts(format),
                "readonly void X.E.add",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[16].ToDisplayParts(format),
                "readonly void X.E.remove",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);
        }

        [Fact]
        public void TestReadOnlyStruct_Members()
        {
            var source = @"
readonly struct X
{
    void M() { }
    int P1 { get => 123; }
    int P2 { set {} }
    int P3 { get => 123; readonly set {} }
    event System.Action E { add {} remove {} }
}
";
            var format = SymbolDisplayFormat.TestFormat
                .AddMemberOptions(SymbolDisplayMemberOptions.IncludeModifiers)
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var comp = CreateCompilation(source).VerifyDiagnostics();
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var declaration = (BaseTypeDeclarationSyntax)semanticModel.SyntaxTree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.StructDeclaration);
            var members = semanticModel.GetDeclaredSymbol(declaration).GetMembers();

            Verify(members[0].ToDisplayParts(format),
                "void X.M()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[1].ToDisplayParts(format),
                "int X.P1 { get; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[2].ToDisplayParts(format),
                "int X.P1.get",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[3].ToDisplayParts(format),
                "int X.P2 { set; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation);

            Verify(members[4].ToDisplayParts(format),
                "void X.P2.set",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[5].ToDisplayParts(format),
                "int X.P3 { get; set; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
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

            Verify(members[6].ToDisplayParts(format),
                "int X.P3.get",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[7].ToDisplayParts(format),
                "void X.P3.set",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.PropertyName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[8].ToDisplayParts(format),
                "event System.Action X.E",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName);

            Verify(members[9].ToDisplayParts(format),
                "void X.E.add",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);

            Verify(members[10].ToDisplayParts(format),
                "void X.E.remove",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.EventName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword);
        }

        [Theory, MemberData(nameof(FileScopedOrBracedNamespace))]
        public void TestReadOnlyStruct_Nested(string ob, string cb)
        {
            var source = @"
namespace Nested
" + ob + @"
    struct X
    {
        readonly void M() { }
    }
" + cb + @"
";
            var format = SymbolDisplayFormat.TestFormat
                .AddMemberOptions(SymbolDisplayMemberOptions.IncludeModifiers)
                .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var comp = CreateCompilation(source, parseOptions: TestOptions.RegularWithFileScopedNamespaces).VerifyDiagnostics();
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var declaration = (BaseTypeDeclarationSyntax)semanticModel.SyntaxTree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.StructDeclaration);
            var members = semanticModel.GetDeclaredSymbol(declaration).GetMembers();

            Verify(members[0].ToDisplayParts(format),
                "readonly void Nested.X.M()",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestPassingVBSymbolsToStructSymbolDisplay()
        {
            var source = @"
Structure X
End Structure";

            var comp = CreateVisualBasicCompilation(source).VerifyDiagnostics();
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());

            var structure = semanticModel.SyntaxTree.GetRoot().DescendantNodes().Single(n => n.RawKind == (int)VisualBasic.SyntaxKind.StructureStatement);
            var format = SymbolDisplayFormat.TestFormat.AddKindOptions(SymbolDisplayKindOptions.IncludeTypeKeyword);

            Verify(SymbolDisplay.ToDisplayParts(semanticModel.GetDeclaredSymbol(structure), format),
                "struct X",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName);
        }

        [Fact]
        public void EnumConstraint_Type()
        {
            TestSymbolDescription(
                "class X<T> where T : System.Enum { }",
                global => global.GetTypeMember("X"),
                SymbolDisplayFormat.TestFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints),
                "X<T> where T : System.Enum",
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
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void EnumConstraint()
        {
            TestSymbolDescription(
                "class X<T> where T : System.Enum { }",
                global => global.GetTypeMember("X").TypeParameters.Single().ConstraintTypes().Single(),
                SymbolDisplayFormat.TestFormat,
                "System.Enum",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void DelegateConstraint_Type()
        {
            TestSymbolDescription(
                "class X<T> where T : System.Delegate { }",
                global => global.GetTypeMember("X"),
                SymbolDisplayFormat.TestFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints),
                "X<T> where T : System.Delegate",
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
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void DelegateConstraint()
        {
            TestSymbolDescription(
                "class X<T> where T : System.Delegate { }",
                global => global.GetTypeMember("X").TypeParameters.Single().ConstraintTypes().Single(),
                SymbolDisplayFormat.TestFormat,
                "System.Delegate",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void MulticastDelegateConstraint_Type()
        {
            TestSymbolDescription(
                "class X<T> where T : System.MulticastDelegate { }",
                global => global.GetTypeMember("X"),
                SymbolDisplayFormat.TestFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints),
                "X<T> where T : System.MulticastDelegate",
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
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void MulticastDelegateConstraint()
        {
            TestSymbolDescription(
                "class X<T> where T : System.MulticastDelegate { }",
                global => global.GetTypeMember("X").TypeParameters.Single().ConstraintTypes().Single(),
                SymbolDisplayFormat.TestFormat,
                "System.MulticastDelegate",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void UnmanagedConstraint_Type()
        {
            TestSymbolDescription(
                "class X<T> where T : unmanaged { }",
                global => global.GetTypeMember("X"),
                SymbolDisplayFormat.TestFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeConstraints),
                "X<T> where T : unmanaged",
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
                SymbolDisplayPartKind.Keyword);
        }

        [Fact]
        public void UnmanagedConstraint_Method()
        {
            TestSymbolDescription(@"
class X
{
    void M<T>() where T : unmanaged, System.IDisposable { }
}",
                global => global.GetTypeMember("X").GetMethod("M"),
                SymbolDisplayFormat.TestFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeConstraints),
                "void X.M<T>() where T : unmanaged, System.IDisposable",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
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
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.InterfaceName);
        }

        [Fact]
        public void UnmanagedConstraint_Delegate()
        {
            TestSymbolDescription(
                "delegate void D<T>() where T : unmanaged;",
                global => global.GetTypeMember("D"),
                SymbolDisplayFormat.TestFormat.AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeConstraints),
                "D<T> where T : unmanaged",
                SymbolDisplayPartKind.DelegateName,
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
                SymbolDisplayPartKind.Keyword);
        }

        [Fact, WorkItem(27104, "https://github.com/dotnet/roslyn/issues/27104")]
        public void BadDiscardInForeachLoop_01()
        {
            var source = @"
class C
{
    void M()
    {
        foreach(_ in """")
        {
        }
    }
}";
            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,17): error CS8186: A foreach loop must declare its iteration variables.
                //         foreach(_ in "")
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "_").WithLocation(6, 17)
                );
            var tree = compilation.SyntaxTrees[0];
            var variable = tree.GetRoot().DescendantNodes().OfType<ForEachVariableStatementSyntax>().Single().Variable;
            var model = compilation.GetSemanticModel(tree);
            var symbol = model.GetSymbolInfo(variable).Symbol;
            Verify(
                symbol.ToMinimalDisplayParts(model, variable.SpanStart),
                "var _",
                SymbolDisplayPartKind.ErrorTypeName, // var
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Punctuation); // _
        }

        [Fact]
        public void ClassConstructorDeclaration()
        {
            TestSymbolDescription(
@"class C
{
    C() { }
}",
                global => global.GetTypeMember("C").Constructors[0],
                new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeContainingType),
                "C.C",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void ClassDestructorDeclaration()
        {
            TestSymbolDescription(
@"class C
{
    ~C() { }
}",
                global => global.GetTypeMember("C").GetMember("Finalize"),
                new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeContainingType),
                "C.~C",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void ClassStaticConstructorDeclaration()
        {
            TestSymbolDescription(
@"class C
{
    static C() { }
}",
                global => global.GetTypeMember("C").Constructors[0],
                new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeContainingType),
                "C.C",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void ClassStaticDestructorDeclaration()
        {
            TestSymbolDescription(
@"class C
{
    static ~C() { }
}",
                global => global.GetTypeMember("C").GetMember("Finalize"),
                new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeContainingType),
                "C.~C",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void ClassConstructorInvocation()
        {
            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

            var source =
@"class C
{
    C() 
    {
        var c = new C();
    }
}";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var constructor = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var symbol = model.GetSymbolInfo(constructor).Symbol;

            Verify(
                symbol.ToMinimalDisplayParts(model, constructor.SpanStart, format),
                "C.C",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.ClassName);
        }

        [Fact]
        public void StructConstructorDeclaration()
        {
            TestSymbolDescription(
@"struct S
{
    int i;

    S(int i)
    {
        this.i = i;
    }
}",
                global => global.GetTypeMember("S").Constructors[0],
                new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeContainingType),
                "S.S",
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName);
        }

        [Fact]
        public void StructConstructorInvocation()
        {
            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

            var source =
@"struct S
{
    int i;

    public S(int i)
    {
        this.i = i;
    }
}

class C
{
    C() 
    {
        var s = new S(1);
    }
}";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var constructor = tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var symbol = model.GetSymbolInfo(constructor).Symbol;

            Verify(
                symbol.ToMinimalDisplayParts(model, constructor.SpanStart, format),
                "S.S",
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName);
        }

        [Fact]
        [WorkItem(38794, "https://github.com/dotnet/roslyn/issues/38794")]
        public void LinqGroupVariableDeclaration()
        {
            var source =
@"using System.Linq;

class C
{
    void M(string[] a)
    {
        var v = from x in a
                group x by x.Length into g
                select g;
    }
}";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var continuation = tree.GetRoot().DescendantNodes().OfType<QueryContinuationSyntax>().Single();
            var symbol = model.GetDeclaredSymbol(continuation);

            Verify(
                symbol.ToMinimalDisplayParts(model, continuation.Identifier.SpanStart),
                "IGrouping<int, string> g",
                SymbolDisplayPartKind.InterfaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.RangeVariableName);
        }

        [Fact]
        public void NativeInt()
        {
            var source =
@"using System;
class A<T>
{
}
class B
{
    static void F1(nint x, nuint y) { }
    static void F2(nint x, IntPtr y) { }
    static void F3(nint? x, UIntPtr? y) { }
    static void F4(nint[] x, A<nuint> y) { }
}";
            var comp = CreateCompilation(new[] { source }, parseOptions: TestOptions.Regular9);
            var formatWithoutOptions = new SymbolDisplayFormat(
                memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeModifiers,
                parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
            var formatWithUnderlyingTypes = formatWithoutOptions.WithCompilerInternalOptions(SymbolDisplayCompilerInternalOptions.UseNativeIntegerUnderlyingType);

            var method = comp.GetMember<MethodSymbol>("B.F1");
            Verify(
                method.ToDisplayParts(formatWithUnderlyingTypes),
                "static void F1(IntPtr x, UIntPtr y)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
            Verify(
                method.ToDisplayParts(formatWithoutOptions),
                "static void F1(nint x, nuint y)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
            Verify(
                method.ToDisplayParts(formatWithoutOptions.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes)),
                "static void F1(nint x, nuint y)");

            method = comp.GetMember<MethodSymbol>("B.F2");
            Verify(
                method.ToDisplayParts(formatWithUnderlyingTypes),
                "static void F2(IntPtr x, IntPtr y)");
            Verify(
                method.ToDisplayParts(formatWithoutOptions),
                "static void F2(nint x, IntPtr y)");

            method = comp.GetMember<MethodSymbol>("B.F3");
            Verify(
                method.ToDisplayParts(formatWithUnderlyingTypes),
                "static void F3(IntPtr? x, UIntPtr? y)");
            Verify(
                method.ToDisplayParts(formatWithoutOptions),
                "static void F3(nint? x, UIntPtr? y)");

            method = comp.GetMember<MethodSymbol>("B.F4");
            Verify(
                method.ToDisplayParts(formatWithUnderlyingTypes),
                "static void F4(IntPtr[] x, A<UIntPtr> y)");
            Verify(
                method.ToDisplayParts(formatWithoutOptions),
                "static void F4(nint[] x, A<nuint> y)");
        }

        [Fact]
        public void RecordDeclaration()
        {
            var text = @"
record Person(string First, string Last);
";
            Func<NamespaceSymbol, Symbol> findSymbol = global => global.GetTypeMembers("Person").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType, kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
                "record Person",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.RecordClassName);
        }

        [Fact]
        public void RecordClassDeclaration()
        {
            var text = @"
record class Person(string First, string Last);
";
            Func<NamespaceSymbol, Symbol> findSymbol = global => global.GetTypeMembers("Person").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType, kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp9),
                "record Person",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.RecordClassName);
        }

        [Fact]
        public void RecordStructDeclaration()
        {
            var text = @"
record struct Person(string First, string Last);
";
            Func<NamespaceSymbol, Symbol> findSymbol = global => global.GetTypeMembers("Person").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType, kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular10,
                "record struct Person",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.RecordStructName);
        }

        [Fact]
        public void ReadOnlyRecordStructDeclaration()
        {
            var text = @"
readonly record struct Person(string First, string Last);
";
            Func<NamespaceSymbol, Symbol> findSymbol = global => global.GetTypeMembers("Person").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType, kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.Regular10,
                "readonly record struct Person",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.RecordStructName);
        }

        [Fact]
        public void ImplicitExtensionDeclaration()
        {
            var text = @"
class C { }
implicit extension R for C { }
";
            Func<NamespaceSymbol, Symbol> findSymbol = global => global.GetTypeMembers("R").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType, kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.RegularNext,
                "implicit extension R",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ExtensionName);
        }

        [Fact]
        public void ExplicitExtensionDeclaration()
        {
            var text = @"
class C { }
explicit extension R for C { }
";
            Func<NamespaceSymbol, Symbol> findSymbol = global => global.GetTypeMembers("R").Single();

            var format = new SymbolDisplayFormat(memberOptions: SymbolDisplayMemberOptions.IncludeType, kindOptions: SymbolDisplayKindOptions.IncludeTypeKeyword);

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                TestOptions.RegularNext,
                "explicit extension R",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ExtensionName);
        }

        [Fact, WorkItem(51222, "https://github.com/dotnet/roslyn/issues/51222")]
        public void TestFunctionPointerWithoutIncludeTypesInParameterOptions()
        {
            var text = @"
class A {
    delegate*<int, string> f;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                ((FieldSymbol)global.GetTypeMembers("A", 0).Single()
                .GetMembers("f").Single()).Type;

            var format = new SymbolDisplayFormat();

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "delegate*<Int32, String>");
        }

        [Fact, WorkItem(51222, "https://github.com/dotnet/roslyn/issues/51222")]
        public void TestFunctionPointerWithTupleParameter()
        {
            var text = @"
class A {
    delegate*<(int, string), void> f;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                ((FieldSymbol)global.GetTypeMembers("A", 0).Single()
                .GetMembers("f").Single()).Type;

            var format = new SymbolDisplayFormat();

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "delegate*<(Int32, String), Void>");
        }

        [Fact, WorkItem(51222, "https://github.com/dotnet/roslyn/issues/51222")]
        public void TestFunctionPointerWithTupleParameterWithNames()
        {
            var text = @"
class A {
    delegate*<(int i, string s), (int i, string s)> f;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                ((FieldSymbol)global.GetTypeMembers("A", 0).Single()
                .GetMembers("f").Single()).Type;

            var format = new SymbolDisplayFormat();

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "delegate*<(Int32 i, String s), (Int32 i, String s)>");
        }

        [Fact, WorkItem(51222, "https://github.com/dotnet/roslyn/issues/51222")]
        public void TestFunctionPointerWithRefParameters()
        {
            var text = @"
class A {
    delegate*<in int, ref readonly string> f;
}";

            Func<NamespaceSymbol, Symbol> findSymbol = global =>
                ((FieldSymbol)global.GetTypeMembers("A", 0).Single()
                .GetMembers("f").Single()).Type;

            var format = new SymbolDisplayFormat();

            TestSymbolDescription(
                text,
                findSymbol,
                format,
                "delegate*<in Int32, ref readonly String>");
        }

        private static readonly SymbolDisplayFormat s_fullDelegateFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            delegateStyle: SymbolDisplayDelegateStyle.NameAndSignature,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseSpecialTypes,
            kindOptions: SymbolDisplayKindOptions.IncludeNamespaceKeyword | SymbolDisplayKindOptions.IncludeTypeKeyword);

        [Fact]
        public void TestInferredDelegateType()
        {
            var source = @"
class C
{
    void M()
    {
        var v = (int i) => i.ToString();
    }
}
";

            var comp = CreateCompilation(source);
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var syntaxTree = semanticModel.SyntaxTree;
            var declaration = (LocalDeclarationStatementSyntax)semanticModel.SyntaxTree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.LocalDeclarationStatement);
            var type = semanticModel.GetTypeInfo(declaration.Declaration.Type).Type;

            Verify(type.ToDisplayParts(), "System.Func<int, string>",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            Verify(type.ToDisplayParts(s_fullDelegateFormat), "delegate string System.Func<int, string>(int arg)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestSynthesizedAnonymousDelegateType1()
        {
            var source = @"
class C
{
    void M()
    {
        var v = (ref int i) => i.ToString();
    }
}
";

            var comp = CreateCompilation(source);
            var semanticModel = comp.GetSemanticModel(comp.SyntaxTrees.Single());
            var syntaxTree = semanticModel.SyntaxTree;
            var declaration = (LocalDeclarationStatementSyntax)semanticModel.SyntaxTree.GetRoot().DescendantNodes().Single(n => n.Kind() == SyntaxKind.LocalDeclarationStatement);
            var type = semanticModel.GetTypeInfo(declaration.Declaration.Type).Type;

            Verify(type.ToDisplayParts(), "<anonymous delegate>",
                SymbolDisplayPartKind.DelegateName);

            Verify(type.ToDisplayParts(s_fullDelegateFormat), "delegate string <anonymous delegate>(ref int arg)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestSynthesizedAnonymousDelegateType2()
        {
            var source =
@"Class Program
    Shared Sub Main
        Dim f = Function(ByRef i As Integer) i.ToString()
    End Sub
End Class";

            var comp = CreateVisualBasicCompilation(source);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var name = tree.GetRoot().DescendantNodes().OfType<VisualBasic.Syntax.VariableDeclaratorSyntax>().Single();
            var type = ((ILocalSymbol)model.GetDeclaredSymbol(name.Names[0])).Type;

            Verify(SymbolDisplay.ToDisplayParts(type), "<anonymous delegate>",
                SymbolDisplayPartKind.DelegateName);

            Verify(SymbolDisplay.ToDisplayParts(type, s_fullDelegateFormat), "delegate string <anonymous delegate>(ref int i)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.DelegateName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void TestNullCheckedParameter()
        {
            var source = @"
class C
{
    void M(string s!!)
    {
    }
}
";

            var comp = CreateCompilation(source);
            var methodSymbol = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol();

            Verify(methodSymbol.ToDisplayParts(s_memberSignatureDisplayFormat), "void C.M(string s)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67464")]
        public void Parameter_Standalone()
        {
            var source = """
                class C
                {
                    void M(ref int p) { }
                }
                """;
            var comp = CreateCompilation(source);
            var methodSymbol = comp.GetMember<MethodSymbol>("C.M").GetPublicSymbol();
            var parameterSymbol = methodSymbol.Parameters.Single();

            var format = s_memberSignatureDisplayFormat.RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeName);

            Verify(methodSymbol.ToDisplayParts(format), "void C.M(ref int)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            Verify(methodSymbol.ToDisplayParts(SymbolDisplayFormat.CSharpErrorMessageFormat), "C.M(ref int)",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            Verify(methodSymbol.ToDisplayParts(SymbolDisplayFormat.CSharpErrorMessageNoParameterNamesFormat), "C.M(ref int)",
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation);

            Verify(parameterSymbol.ToDisplayParts(format), "ref int p",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName);

            Verify(parameterSymbol.ToDisplayParts(SymbolDisplayFormat.CSharpErrorMessageFormat), "ref int p",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName);

            Verify(parameterSymbol.ToDisplayParts(SymbolDisplayFormat.CSharpErrorMessageNoParameterNamesFormat), "ref int",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword);
        }

        [Fact]
        public void TestRequiredProperty()
        {
            var source = @"
class C
{
    required int Prop { get; set; }
}
";

            var comp = CreateCompilation(source);
            var propertySymbol = comp.GetMember<PropertySymbol>("C.Prop").GetPublicSymbol();

            Verify(propertySymbol.ToDisplayParts(s_memberSignatureDisplayFormat), "required int C.Prop { get; set; }",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
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
        public void RefFields()
        {
            var source =
@"#pragma warning disable 169
ref struct S<T>
{
    ref T F1;
    ref readonly T F2;
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics(
                // (4,11): error CS9064: Target runtime doesn't support ref fields.
                //     ref T F1;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportRefFields, "F1").WithLocation(4, 11),
                // (5,20): error CS9064: Target runtime doesn't support ref fields.
                //     ref readonly T F2;
                Diagnostic(ErrorCode.ERR_RuntimeDoesNotSupportRefFields, "F2").WithLocation(5, 20)
                );

            Verify(comp.GetMember<FieldSymbol>("S.F1").ToDisplayParts(SymbolDisplayFormat.TestFormat),
                "ref T S<T>.F1",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName);

            Verify(comp.GetMember<FieldSymbol>("S.F2").ToDisplayParts(SymbolDisplayFormat.TestFormat),
                "ref readonly T S<T>.F2",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.TypeParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.FieldName);
        }

        [Fact]
        public void ScopedParameter_01()
        {
            var source =
@"ref struct R { }
class Program
{
    static void F(scoped R r1, scoped ref R r2, scoped in R r3, scoped out R r4) { }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var method = comp.GetMember<MethodSymbol>("Program.F");

            var formatTypeOnly = SymbolDisplayFormat.TestFormat.WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName);
            var formatTypeRefAndScoped = formatTypeOnly.AddParameterOptions(SymbolDisplayParameterOptions.IncludeParamsRefOut);

            Verify(method.ToDisplayParts(formatTypeOnly),
                "void Program.F(R r1, R r2, R r3, R r4)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            Verify(method.ToDisplayParts(formatTypeRefAndScoped),
                "void Program.F(scoped R r1, scoped ref R r2, scoped in R r3, out R r4)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ClassName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }

        [Fact]
        public void ScopedParameter_02()
        {
            var source =
@"ref struct R { }
delegate void D(scoped R r1, scoped ref R r2, scoped in R r3, scoped out R r4);
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var delegateType = comp.GetMember<NamedTypeSymbol>("D");

            var formatTypeOnly = s_fullDelegateFormat.WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName);
            var formatTypeRefAndScoped = formatTypeOnly.AddParameterOptions(SymbolDisplayParameterOptions.IncludeParamsRefOut);

            Verify(delegateType.ToDisplayParts(formatTypeOnly),
                "delegate void D(R r1, R r2, R r3, R r4)");

            Verify(delegateType.ToDisplayParts(formatTypeRefAndScoped),
                "delegate void D(scoped R r1, scoped ref R r2, scoped in R r3, out R r4)");
        }

        [Fact]
        public void ScopedParameter_03()
        {
            var source =
@"#pragma warning disable 169
ref struct R { }
unsafe class Program
{
    delegate*<scoped R, scoped in R, scoped ref R, scoped out R, void> D;
}
";

            var comp = CreateCompilation(source, options: TestOptions.UnsafeReleaseDll);
            comp.VerifyDiagnostics(
                // (5,15): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //     delegate*<scoped R, scoped in R, scoped ref R, scoped out R, void> D;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(5, 15),
                // (5,25): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //     delegate*<scoped R, scoped in R, scoped ref R, scoped out R, void> D;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(5, 25),
                // (5,38): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //     delegate*<scoped R, scoped in R, scoped ref R, scoped out R, void> D;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(5, 38),
                // (5,52): error CS8755: 'scoped' cannot be used as a modifier on a function pointer parameter.
                //     delegate*<scoped R, scoped in R, scoped ref R, scoped out R, void> D;
                Diagnostic(ErrorCode.ERR_BadFuncPointerParamModifier, "scoped").WithArguments("scoped").WithLocation(5, 52));

            var type = comp.GetMember<FieldSymbol>("Program.D").Type;

            var formatMinimal = new SymbolDisplayFormat();
            var formatTypeRefAndScoped = s_fullDelegateFormat.
                WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut);

            Verify(type.ToDisplayParts(formatMinimal),
                "delegate*<R, in R, ref R, out R, Void>");

            Verify(type.ToDisplayParts(formatTypeRefAndScoped),
                "delegate*<R, in R, ref R, out R, void>");
        }

        [Fact]
        public void ScopedParameter_04()
        {
            var source =
@"using System.Diagnostics.CodeAnalysis;
ref struct R { }
class Program
{
    static void F1(out int i1, [UnscopedRef] out int i2) => throw null;
    static void F2(ref R r1, ref R r2) => throw null;
}";

            var comp = CreateCompilation(new[] { source, UnscopedRefAttributeDefinition });
            comp.VerifyDiagnostics();

            var format = SymbolDisplayFormat.TestFormat.
                WithParameterOptions(SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeParamsRefOut);

            Verify(comp.GetMember<MethodSymbol>("Program.F1").ToDisplayParts(format),
                "void Program.F1(out System.Int32 i1, out System.Int32 i2)");

            Verify(comp.GetMember<MethodSymbol>("Program.F2").ToDisplayParts(format),
                "void Program.F2(ref R r1, ref R r2)");
        }

        [Fact]
        public void ScopedLocal()
        {
            var source =
@"ref struct R { }
class Program
{
    static void M(R r0)
    {
        scoped R r1 = r0;
        scoped ref readonly R r3 = ref r0;
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var decls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().ToArray();
            var locals = decls.Select(d => model.GetDeclaredSymbol(d)).ToArray();

            var formatTypeOnly = SymbolDisplayFormat.TestFormat.WithLocalOptions(SymbolDisplayLocalOptions.IncludeType);
            var formatTypeRefAndScoped = formatTypeOnly.AddLocalOptions(SymbolDisplayLocalOptions.IncludeRef);

            Verify(locals[0].ToDisplayParts(formatTypeOnly),
                "R r1",
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName);

            Verify(locals[0].ToDisplayParts(formatTypeRefAndScoped),
                "scoped R r1",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName);

            Verify(locals[1].ToDisplayParts(formatTypeOnly),
                "R r3",
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName);

            Verify(locals[1].ToDisplayParts(formatTypeRefAndScoped),
                "scoped ref readonly R r3",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.LocalName);
        }

        [Fact, WorkItem(38783, "https://github.com/dotnet/roslyn/issues/38783")]
        public void Operator1()
        {
            var source = """
                class Program
                {
                    void M()
                    {
                        _ = 1 == 1;
                    }
                }
                """;

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();
            var tree = comp.SyntaxTrees[0];
            var model = comp.GetSemanticModel(tree);
            var binaryExpression = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
            var op = model.GetSymbolInfo(binaryExpression).Symbol;

            // When asking for metadata names, this should show up as a method-name.
            Verify(op.ToDisplayParts(SymbolDisplayFormat.TestFormat),
                "System.Boolean System.Int32.op_Equality(System.Int32 left, System.Int32 right)",
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.MethodName, // Should be MethodName because of 'op_Equality'
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.NamespaceName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.StructName,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);

            var ideFormat = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeRef |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeContainingType,
                kindOptions:
                    SymbolDisplayKindOptions.IncludeMemberKeyword,
                propertyStyle:
                    SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeName |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeDefaultValue |
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets,
                localOptions:
                    SymbolDisplayLocalOptions.IncludeRef |
                    SymbolDisplayLocalOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                    SymbolDisplayMiscellaneousOptions.AllowDefaultLiteral |
                    SymbolDisplayMiscellaneousOptions.CollapseTupleTypes);

            // When not asking for metadata names, this should show up as an operator.
            Verify(op.ToDisplayParts(ideFormat),
                "bool int.operator ==(int left, int right)",
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Operator, // Should be MethodName because of '=='
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.Keyword,
                SymbolDisplayPartKind.Space,
                SymbolDisplayPartKind.ParameterName,
                SymbolDisplayPartKind.Punctuation);
        }
    }
}
