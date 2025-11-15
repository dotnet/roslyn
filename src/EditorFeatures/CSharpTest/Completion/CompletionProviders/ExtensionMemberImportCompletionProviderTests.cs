// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Completion)]
public sealed class ExtensionMemberImportCompletionProviderTests : AbstractCSharpCompletionProviderTests
{
    public ExtensionMemberImportCompletionProviderTests()
    {
        ShowImportCompletionItemsOptionValue = true;
        ForceExpandedCompletionIndexCreation = true;
    }

    internal override Type GetCompletionProviderType()
        => typeof(ExtensionMemberImportCompletionProvider);

    public enum ReferenceType
    {
        None,
        Project,
        Metadata
    }

    private static IEnumerable<object[]> CombineWithReferenceTypeData(IEnumerable<List<object>> data)
    {
        foreach (var refKind in Enum.GetValues<ReferenceType>())
        {
            foreach (var d in data)
            {
                d.Add(refKind);
                yield return d.ToArray();
            }
        }
    }

    public static IEnumerable<object[]> ReferenceTypeData
        => (new[] { ReferenceType.None, ReferenceType.Project, ReferenceType.Metadata }).Select(refType => new[] { (object)refType });

    public static IEnumerable<object[]> AllTypeKindsWithReferenceTypeData
        => CombineWithReferenceTypeData((new[] { "class", "struct", "interface", "enum", "abstract class" }).Select(kind => new List<object>() { kind }));

    private static IEnumerable<List<object>> BuiltInTypes(bool includeArrays)
    {
        var predefinedTypes = new List<string>() { "string", "String", "System.String" };
        var arraySuffixes = includeArrays ? new[] { "", "[]", "[,]" } : new[] { "" };

        foreach (var type1 in predefinedTypes)
        {
            foreach (var type2 in predefinedTypes)
            {
                foreach (var suffix in arraySuffixes)
                    yield return new List<object>() { type1 + suffix, type2 + suffix };
            }
        }
    }

    private static string GetMarkup(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string current,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string referenced,
        ReferenceType refType,
        string currentLanguage = LanguageNames.CSharp,
        string referencedLanguage = LanguageNames.CSharp)
        => refType switch
        {
            ReferenceType.None => CreateMarkupForSingleProject(current, referenced, currentLanguage),
            ReferenceType.Project => GetMarkupWithReference(current, referenced, currentLanguage, referencedLanguage, true),
            ReferenceType.Metadata => GetMarkupWithReference(current, referenced, currentLanguage, referencedLanguage, false),
            _ => null,
        };

    public static IEnumerable<object[]> BuiltInTypesIncludingArraysWithReferenceTypeData
        => CombineWithReferenceTypeData(BuiltInTypes(includeArrays: true));

    public static IEnumerable<object[]> BuiltInTypesWithReferenceTypeData
        => CombineWithReferenceTypeData(BuiltInTypes(includeArrays: false));

    private Task VerifyImportItemExistsAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string expectedItem, string inlineDescription, Glyph? glyph = null, string displayTextSuffix = null, string expectedDescriptionOrNull = null, List<CompletionFilter> expectedFilters = null)
        => VerifyItemExistsAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, glyph: glyph, inlineDescription: inlineDescription, expectedDescriptionOrNull: expectedDescriptionOrNull, isComplexTextEdit: true, matchingFilters: expectedFilters);

    private Task VerifyImportItemIsAbsentAsync(
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup, string expectedItem, string inlineDescription, string displayTextSuffix = null)
        => VerifyItemIsAbsentAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription);

    [Theory, MemberData(nameof(BuiltInTypesIncludingArraysWithReferenceTypeData))]
    public async Task TestPredefinedType(string type1, string type2, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this {{type1}} x)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{type2}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UsingAliasInDeclaration(ReferenceType refType)
    {
        var file1 = """
            using System;
            using MyInt = System.Int32;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this MyInt x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UsingAliasInDeclaration_PrimitiveType(ReferenceType refType)
    {
        var file1 = """
            using System;
            using MyInt = System.Int32;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this MyInt x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UsingAliasInDeclaration_RegularType(ReferenceType refType)
    {
        var file1 = """
            using System;
            using MyAlias = System.Exception;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this MyAlias x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(Exception x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UsingAliasInDeclaration_GenericType(ReferenceType refType)
    {
        var file1 = """
            using System;
            using MyAlias = System.Collections.Generic.List<int>;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this MyAlias x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(System.Collections.Generic.List<int> x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UsingAliasInDeclaration_RegularTypeWithSameSimpleName(ReferenceType refType)
    {
        var file1 = """
            using DataTime = System.Exception;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this System.DateTime x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(DateTime x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UsingAliasInDeclaration_Namespace(ReferenceType refType)
    {
        var file1 = """
            using System;
            using GenericCollection = System.Collections.Generic;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod<T>(this GenericCollection.List<T> x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(System.Collections.Generic.List<int> x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             displayTextSuffix: "<>",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UsingAliasInUsage(ReferenceType refType)
    {
        var file1 = """
            using System;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this int x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;
            using MyInt = System.Int32;

            namespace Baz
            {
                public class Bat
                {
                    public void M(MyInt x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(AllTypeKindsWithReferenceTypeData))]
    public async Task RegularType(string typeKind, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            public {{typeKind}} MyType { }

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this MyType t)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(MyType x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(AllTypeKindsWithReferenceTypeData))]
    public async Task ObjectType(string typeKind, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            public {{typeKind}} MyType { }

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this object t)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(MyType x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    public static IEnumerable<object[]> TupleWithRefTypeData => CombineWithReferenceTypeData(
        (new[]
        {
            "(int, int)",
            "(int, (int, int))",
            "(string a, string b)"
        }).Select(tuple => new List<object>() { tuple }));

    [Theory, MemberData(nameof(TupleWithRefTypeData))]
    public async Task ValueTupleType(string tupleType, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this {{tupleType}} t)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{tupleType}} x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    public static IEnumerable<object[]> DerivableTypeKindsWithReferenceTypeData
        => CombineWithReferenceTypeData((new[] { "class", "interface", "abstract class" }).Select(kind => new List<object>() { kind }));

    [Theory, MemberData(nameof(DerivableTypeKindsWithReferenceTypeData))]
    public async Task RegularTypeAsBase(string baseType, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            public {{baseType}} MyBase { }

            public class MyType : MyBase { }

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this MyBase t)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(MyType x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    public static IEnumerable<object[]> BounedGenericTypeWithRefTypeData => CombineWithReferenceTypeData(
        (new[]
        {
            "IEnumerable<string>",
            "List<string>",
            "string[]"
        }).Select(tuple => new List<object>() { tuple }));

    [Theory, MemberData(nameof(BounedGenericTypeWithRefTypeData))]
    public async Task BoundedGenericType(string type, ReferenceType refType)
    {
        var file1 = """
            using System;
            using System.Collections.Generic;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this IEnumerable<string> t)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;
            using System.Collections.Generic;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{type}} x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }
    public static IEnumerable<object[]> TypeParameterWithRefTypeData => CombineWithReferenceTypeData(
        (new[]
        {
            "IEnumerable<string>",
            "int",
            "Bat",
            "Bat"
        }).Select(tuple => new List<object>() { tuple }));

    [Theory, MemberData(nameof(TypeParameterWithRefTypeData))]
    public async Task MatchingTypeParameter(string type, ReferenceType refType)
    {
        var file1 = """
            using System;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod<T>(this T t)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;
            using System.Collections.Generic;

            namespace Baz
            {
                public interface Bar {}

                public class Bat
                {
                    public void M({{type}} x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             displayTextSuffix: "<>",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory]
    [InlineData(ReferenceType.Project)]
    [InlineData(ReferenceType.Metadata)]
    public async Task TestInternalExtensionMethods_NoIVT_InReference(ReferenceType refType)
    {
        var file1 = """
            using System;

            namespace Foo
            {
                internal static class ExtensionClass
                {
                    public static bool ExtensionMethod(this int x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemIsAbsentAsync(
             markup,
             "ExtensionMethod",
             inlineDescription: "Foo");
    }

    [Fact]
    public async Task TestInternalExtensionMethods_NoIVT_InSameProject()
    {
        var file1 = """
            using System;

            namespace Foo
            {
                internal static class ExtensionClass
                {
                    internal static bool ExtensionMethod(this int x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, ReferenceType.None);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodInternal,     // This is based on declared accessibility
             inlineDescription: "Foo");
    }

    // SymbolTreeInfo explicitly ignores non-public types from metadata(likely for perf reasons). So we don't need to test internals in PE reference
    [Theory]
    [InlineData(ReferenceType.None)]
    [InlineData(ReferenceType.Project)]
    public async Task TestInternalExtensionMethods_WithIVT(ReferenceType refType)
    {
        var file1 = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project1")]

            namespace Foo
            {
                internal static class ExtensionClass
                {
                    internal static bool ExtensionMethod(this int x)
                        => true;
                }
            }
            """;
        var file2 = """
            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodInternal,
             inlineDescription: "Foo");
    }

    [Theory, MemberData(nameof(ReferenceTypeData))]
    public async Task UserDefinedGenericType(ReferenceType refType)
    {
        var file1 = """
            using System;

            public class MyGeneric<T>
            {
            }

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this MyGeneric<int> x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(MyGeneric<int> x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [InlineData("(1 + 1)")]
    [InlineData("(new int())")]
    [Theory]
    public async Task MethodSymbolReceiver(string expression)
    {
        var file1 = """
            using System;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod(this int x)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M()
                    {
                        {{expression}}.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, ReferenceType.None);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    public static IEnumerable<object[]> VBBuiltInTypes
    {
        get
        {
            var predefinedTypes = new List<(string vbType, string csType)>
            {
                ( "Boolean", "bool" ),
                ( "Byte", "byte" ),
                ( "Char", "char" ),
                ( "Date", "DateTime" ),
                ( "Integer", "int" ),
                ( "String", "string" ),
                ( "Object", "object" ),
                ( "Short", "short" ),

            };

            var arraySuffixes = new (string vbSuffix, string csSuffix)[] { ("", ""), ("()", "[]"), ("(,)", "[,]") };

            foreach (var type in predefinedTypes)
            {
                foreach (var suffix in arraySuffixes)
                {
                    yield return new object[] { type.vbType + suffix.vbSuffix, type.csType + suffix.csSuffix };
                }
            }
        }
    }

    [Theory, MemberData(nameof(VBBuiltInTypes))]
    public async Task ExtensionMethodDelcaredInVBSource(string vbType, string csType)
    {
        var file1 = $"""
            Imports System
            Imports System.Runtime.CompilerServices

            Namespace NS
                Public Module Foo
                    <Extension>
                    public Function ExtensionMethod(x As {vbType}) As Boolean
                        Return True
                    End Function
                End Module
            End Namespace
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{csType}} x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, ReferenceType.Project, currentLanguage: LanguageNames.CSharp, referencedLanguage: LanguageNames.VisualBasic);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "NS");
    }

    [Fact]
    public async Task ExtensionMethodDelcaredInRootNamespaceVBSource()
    {
        var file1 = """
            Imports System
            Imports System.Runtime.CompilerServices

            Public Module Foo
                <Extension>
                public Function ExtensionMethod(x As Integer) As Boolean
                    Return True
                End Function
            End Module
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "Root");

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Root");
    }

    [Fact]
    public async Task ExtensionMethodDelcaredInGlobalNamespaceVBSource()
    {
        var file1 = """
            Imports System
            Imports System.Runtime.CompilerServices

            Public Module Foo
                <Extension>
                public Function ExtensionMethod(x As Integer) As Boolean
                    Return True
                End Function
            End Module
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;
        var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp);

        await VerifyImportItemIsAbsentAsync(
             markup,
             "ExtensionMethod",
             inlineDescription: "");
    }

    [Fact]
    public async Task TestTriggerLocation()
    {
        var file1 = """
            using System;

            namespace Foo
            {
                internal static class ExtensionClass
                {
                    internal static bool ExtensionMethod(this int x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                        var z = 10;
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, ReferenceType.None);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodInternal,     // This is based on declared accessibility
             inlineDescription: "Foo");
    }

    [Theory]
    [InlineData("int", "Int32Method", "Foo")]
    [InlineData("string", "StringMethod", "Bar")]
    public async Task TestIdenticalAliases(string type, string expectedMethodname, string expectedNamespace)
    {
        var file1 = """
            using X = System.String;

            namespace Foo
            {
                using X = System.Int32;

                internal static class ExtensionClass
                {
                    internal static bool Int32Method(this X x)
                        => true;
                }
            }

            namespace Bar
            {
                internal static class ExtensionClass
                {
                    internal static bool StringMethod(this X x)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{type}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, ReferenceType.None);
        await VerifyImportItemExistsAsync(
             markup,
             expectedMethodname,
             glyph: Glyph.ExtensionMethodInternal,
             inlineDescription: expectedNamespace);
    }

    [Theory]
    [InlineData("int")]
    [InlineData("Exception")]
    public async Task TestIdenticalMethodName(string type)
    {
        var file1 = """
            using System;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtMethod(this int x)
                        => true;

                    public static bool ExtMethod(this Exception x)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{type}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, ReferenceType.None);
        await VerifyImportItemExistsAsync(
             markup,
             "ExtMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Fact]
    public async Task DoNotTriggerOnType()
    {
        var file1 = """
            using System;

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtMethod(this string x)
                        => true;
                }
            }
            """;
        var file2 = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M()
                    {
                        string.$$
                    }
                }
            }
            """;
        var markup = GetMarkup(file2, file1, ReferenceType.None);
        await VerifyImportItemIsAbsentAsync(
             markup,
             "ExtMethod",
             inlineDescription: "Foo");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42325")]
    public async Task TestExtensionMethodInPartialClass()
    {
        var file1 = """
            using System;

            namespace Foo
            {
                public static partial class ExtensionClass
                {
                    public static bool ExtensionMethod1(this string x)
                        => true;
                }
            }
            """;
        var currentFile = """
            using System;

            namespace Foo
            {
                public static partial class ExtensionClass
                {
                    public static bool ExtensionMethod2(this string x)
                        => true;
                }
            }

            namespace Baz
            {
                public class Bat
                {
                    public void M(string x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = CreateMarkupForSingleProject(currentFile, file1, LanguageNames.CSharp);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod1",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod2",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [InlineData(ReferenceType.Project, "public")]
    [InlineData(ReferenceType.Project, "internal")]
    [InlineData(ReferenceType.Metadata, "public")]  // We don't support internal extension method from non-source references.
    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/42325")]
    public async Task TestExtensionMethodsInConflictingTypes(ReferenceType refType, string accessibility)
    {
        var refDoc = $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project1")]

            namespace Foo
            {
                {{accessibility}} static class ExtensionClass
                {
                    public static bool ExtensionMethod1(this int x)
                        => true;
                }
            }
            """;
        var srcDoc = """
            using System;

            namespace Foo
            {
                internal static class ExtensionClass
                {
                    public static bool ExtensionMethod2(this int x)
                        => true;
                }
            }

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod1",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod2",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/42325")]
    public async Task TestExtensionMethodsInConflictingTypesFromReferencedProjects()
    {
        var refDoc1 = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project1")]

            namespace Foo
            {
                internal static class ExtensionClass
                {
                    public static bool ExtensionMethod1(this int x)
                        => true;
                }
            }
            """;
        var refDoc2 = """
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project1")]

            namespace Foo
            {
                internal static class ExtensionClass
                {
                    public static bool ExtensionMethod2(this int x)
                        => true;
                }
            }
            """;
        var srcDoc = """
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = CreateMarkupForProjectWithMultupleProjectReferences(srcDoc, LanguageNames.CSharp, LanguageNames.CSharp, [refDoc1, refDoc2]);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod1",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod2",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory]
    [InlineData("", "", false)]
    [InlineData("", "public", true)]
    [InlineData("public", "", false)]
    public async Task TestCSharpDefaultAccessibility(string containerAccessibility, string methodAccessibility, bool isAvailable)
    {
        var file1 = $$"""
            using System;

            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project1")]

            namespace Foo
            {
                {{containerAccessibility}} static class ExtensionClass
                {
                    {{methodAccessibility}} static bool ExtensionMethod(this int x)
                        => true;
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkupWithReference(file2, file1, LanguageNames.CSharp, LanguageNames.CSharp, isProjectReference: true);

        if (isAvailable)
        {
            await VerifyImportItemExistsAsync(
                 markup,
                 "ExtensionMethod",
                 glyph: Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }
        else
        {
            await VerifyImportItemIsAbsentAsync(
                 markup,
                 "ExtensionMethod",
                 inlineDescription: "Foo");
        }
    }

    [Theory]
    [InlineData(ReferenceType.Project, "[]", "ExtensionMethod2")]
    [InlineData(ReferenceType.Project, "[][]", "ExtensionMethod3")]
    [InlineData(ReferenceType.Project, "[,]", "ExtensionMethod4")]
    [InlineData(ReferenceType.Project, "[][,]", "ExtensionMethod5")]
    [InlineData(ReferenceType.Metadata, "[]", "ExtensionMethod2")]
    [InlineData(ReferenceType.Metadata, "[][]", "ExtensionMethod3")]
    [InlineData(ReferenceType.Metadata, "[,]", "ExtensionMethod4")]
    [InlineData(ReferenceType.Metadata, "[][,]", "ExtensionMethod5")]
    public async Task TestExtensionMethodsForSimpleArrayType(ReferenceType refType, string rank, string expectedName)
    {
        var refDoc = $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project1")]

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod1(this int x)
                        => true;

                    public static bool ExtensionMethod2(this int[] x)
                        => true;

                    public static bool ExtensionMethod3(this int[][] x)
                        => true;

                    public static bool ExtensionMethod4(this int[,] x)
                        => true;

                    public static bool ExtensionMethod5(this int[][,] x)
                        => true;
                }
            }
            """;
        var srcDoc = $$"""
            namespace Baz
            {
                public class Bat
                {
                    public void M(int{{rank}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
             markup,
             expectedName,
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory]
    [InlineData(ReferenceType.Project, "[]", "ExtensionMethod2")]
    [InlineData(ReferenceType.Project, "[][]", "ExtensionMethod3")]
    [InlineData(ReferenceType.Project, "[,]", "ExtensionMethod4")]
    [InlineData(ReferenceType.Project, "[][,]", "ExtensionMethod5")]
    [InlineData(ReferenceType.Metadata, "[]", "ExtensionMethod2")]
    [InlineData(ReferenceType.Metadata, "[][]", "ExtensionMethod3")]
    [InlineData(ReferenceType.Metadata, "[,]", "ExtensionMethod4")]
    [InlineData(ReferenceType.Metadata, "[][,]", "ExtensionMethod5")]
    public async Task TestExtensionMethodsForGenericArrayType(ReferenceType refType, string rank, string expectedName)
    {
        var refDoc = $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project1")]

            namespace Foo
            {
                public static class ExtensionClass
                {
                    public static bool ExtensionMethod1<T>(this T x)
                        => true;

                    public static bool ExtensionMethod2<T>(this T[] x)
                        => true;

                    public static bool ExtensionMethod3<T>(this T[][] x)
                        => true;

                    public static bool ExtensionMethod4<T>(this T[,] x)
                        => true;

                    public static bool ExtensionMethod5<T>(this T[][,] x)
                        => true;
                }
            }
            """;
        var srcDoc = $$"""
            namespace Baz
            {
                public class Bat
                {
                    public void M(int{{rank}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
             markup,
             expectedName,
             displayTextSuffix: "<>",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Foo");
    }

    [Theory]
    [InlineData(ReferenceType.Project)]
    [InlineData(ReferenceType.Metadata)]
    public async Task TestGenericReceiverTypeWithConstraint(ReferenceType refType)
    {
        var refDoc = """
            using System;

            namespace NS1
            {
                public class C1 {}
            }

            namespace NS2
            {
                public static class Extensions
                {
                    public static bool ExtensionMethod(this NS1.C1 c) => false;
                }
            }
            """;
        var srcDoc = """
            namespace NS1
            {
                public class C2
                {
                    public void M<T>(T x) where T : C1
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
            markup,
            "ExtensionMethod",
            glyph: Glyph.ExtensionMethodPublic,
            inlineDescription: "NS2");
    }

    [Theory]
    [InlineData(ReferenceType.Project, "(int,int)")]
    [InlineData(ReferenceType.Project, "(int,int,int,int,int,int,int,int,int,int)")]    // more than 8 tuple elements
    [InlineData(ReferenceType.Metadata, "(int,int)")]
    [InlineData(ReferenceType.Metadata, "(int,int,int,int,int,int,int,int,int,int)")]   // more than 8 tuple elements
    public async Task TestTupleArray(ReferenceType refType, string tupleType)
    {
        var refDoc = $$"""
            using System;

            namespace NS2
            {
                public static class Extensions
                {
                    public static bool ExtensionMethod(this {{tupleType}}[] x) => false;
                }
            }
            """;
        var srcDoc = $$"""
            namespace NS1
            {
                public class C
                {
                    public void M({{tupleType}}[] x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
            markup,
            "ExtensionMethod",
            glyph: Glyph.ExtensionMethodPublic,
            inlineDescription: "NS2");
    }

    [Theory]
    [InlineData(ReferenceType.Project, "(int[],int[])")]
    [InlineData(ReferenceType.Project, "(int[],int[],int[],int[],int[],int[],int[],int[],int[],int[])")] // more than 8 tuple elements
    [InlineData(ReferenceType.Metadata, "(int[],int[])")]
    [InlineData(ReferenceType.Metadata, "(int[],int[],int[],int[],int[],int[],int[],int[],int[],int[])")] // more than 8 tuple elements
    public async Task TestArrayTuple(ReferenceType refType, string tupleType)
    {
        var refDoc = $$"""
            using System;

            namespace NS2
            {
                public static class Extensions
                {
                    public static bool ExtensionMethod(this {{tupleType}} x) => false;
                }
            }
            """;
        var srcDoc = $$"""
            namespace NS1
            {
                public class C
                {
                    public void M({{tupleType}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
            markup,
            "ExtensionMethod",
            glyph: Glyph.ExtensionMethodPublic,
            inlineDescription: "NS2");
    }

    [Theory]
    [InlineData(ReferenceType.Project)]
    [InlineData(ReferenceType.Metadata)]
    public async Task TestDescriptionOfGenericReceiverType(ReferenceType refType)
    {
        var refDoc = """
            using System;

            namespace NS2
            {
                public static class Extensions
                {
                    public static bool ExtensionMethod<T>(this T t) => false;
                }
            }
            """;
        var srcDoc = """
            namespace NS1
            {
                public class C
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
            markup,
            "ExtensionMethod",
            displayTextSuffix: "<>",
            glyph: Glyph.ExtensionMethodPublic,
            inlineDescription: "NS2",
            expectedDescriptionOrNull: $"({CSharpFeaturesResources.extension}) bool int.ExtensionMethod<int>()");
    }

    [Theory]
    [InlineData(ReferenceType.Project)]
    [InlineData(ReferenceType.Metadata)]
    public async Task TestDescriptionOfOverloads(ReferenceType refType)
    {
        var refDoc = """
            using System;

            namespace NS2
            {
                public static class Extensions
                {
                    public static bool ExtensionMethod(this int t) => false;
                    public static bool ExtensionMethod(this int t, int a) => false;
                    public static bool ExtensionMethod(this int t, int a, int b) => false;
                    public static bool ExtensionMethod<T>(this int t, T a) => false;
                    public static bool ExtensionMethod<T>(this int t, T a, T b) => false;
                    public static bool ExtensionMethod<T1, T2>(this int t, T1 a, T2 b) => false;
                }
            }
            """;
        var srcDoc = """
            namespace NS1
            {
                public class C
                {
                    public void M(int x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
            markup,
            "ExtensionMethod",
            glyph: Glyph.ExtensionMethodPublic,
            inlineDescription: "NS2",
            expectedDescriptionOrNull: $"({CSharpFeaturesResources.extension}) bool int.ExtensionMethod() (+{NonBreakingSpaceString}2{NonBreakingSpaceString}{FeaturesResources.overloads_})");

        await VerifyImportItemExistsAsync(
            markup,
            "ExtensionMethod",
            displayTextSuffix: "<>",
            glyph: Glyph.ExtensionMethodPublic,
            inlineDescription: "NS2",
            expectedDescriptionOrNull: $"({CSharpFeaturesResources.extension}) bool int.ExtensionMethod<T>(T a) (+{NonBreakingSpaceString}2{NonBreakingSpaceString}{FeaturesResources.generic_overloads})");
    }

    [InlineData(ReferenceType.Project)]
    [InlineData(ReferenceType.Metadata)]
    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47551")]
    public async Task TestBrowsableAlways(ReferenceType refType)
    {
        var srcDoc = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var refDoc = """
            public class Goo
            {
            }

            namespace Foo
            {
                public static class GooExtensions
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Always)]
                    public static void Bar(this Goo goo, int x)
                    {
                    }
                }
            }
            """;

        var markup = refType switch
        {
            ReferenceType.Project => CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            ReferenceType.Metadata => CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp),
            _ => null,
        };

        await VerifyImportItemExistsAsync(
                markup,
                "Bar",
                glyph: Glyph.ExtensionMethodPublic,
                inlineDescription: "Foo");
    }

    [InlineData(ReferenceType.Project)]
    [InlineData(ReferenceType.Metadata)]
    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47551")]
    public async Task TestBrowsableNever(ReferenceType refType)
    {
        var srcDoc = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var refDoc = """
            public class Goo
            {
            }

            namespace Foo
            {
                public static class GooExtensions
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
                    public static void Bar(this Goo goo, int x)
                    {
                    }
                }
            }
            """;

        var (markup, shouldContainItem) = refType switch
        {
            ReferenceType.Project => (CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), true),
            ReferenceType.Metadata => (CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), false),
            _ => throw ExceptionUtilities.Unreachable(),
        };

        if (shouldContainItem)
        {
            await VerifyImportItemExistsAsync(
                    markup,
                    "Bar",
                    glyph: Glyph.ExtensionMethodPublic,
                    inlineDescription: "Foo");
        }
        else
        {
            await VerifyImportItemIsAbsentAsync(
                    markup,
                    "Bar",
                    inlineDescription: "Foo");
        }
    }

    [InlineData(ReferenceType.Project, true)]
    [InlineData(ReferenceType.Project, false)]
    [InlineData(ReferenceType.Metadata, true)]
    [InlineData(ReferenceType.Metadata, false)]
    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/47551")]
    public async Task TestBrowsableAdvanced(ReferenceType refType, bool hideAdvanced)
    {
        HideAdvancedMembers = hideAdvanced;

        var srcDoc = """
            class Program
            {
                void M()
                {
                    new Goo().$$
                }
            }
            """;

        var refDoc = """
            public class Goo
            {
            }

            namespace Foo
            {
                public static class GooExtensions
                {
                    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
                    public static void Bar(this Goo goo, int x)
                    {
                    }
                }
            }
            """;

        var (markup, shouldContainItem) = (refType, hideAdvanced) switch
        {
            (ReferenceType.Project, _) => (CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), true),
            (ReferenceType.Metadata, true) => (CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), false),
            (ReferenceType.Metadata, false) => (CreateMarkupForProjectWithMetadataReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp), true),
            _ => throw ExceptionUtilities.Unreachable(),
        };

        if (shouldContainItem)
        {
            await VerifyImportItemExistsAsync(
                    markup,
                    "Bar",
                    glyph: Glyph.ExtensionMethodPublic,
                    inlineDescription: "Foo");
        }
        else
        {
            await VerifyImportItemIsAbsentAsync(
                    markup,
                    "Bar",
                    inlineDescription: "Foo");
        }
    }

    [Theory]
    [InlineData('.')]
    [InlineData(';')]
    public Task TestCommitWithCustomizedCharForMethod(char commitChar)
        => VerifyProviderCommitAsync("""
            public class C
            {
            }
            namespace AA
            {
                public static class Ext
                {
                    public static int ToInt(this C c)
                        => 1;
                }
            }

            namespace BB
            {
                public class B
                {
                    public void M()
                    {
                        var c = new C();
                        c.$$
                    }
                }
            }
            """, "ToInt", $$"""
        using AA;

        public class C
        {
        }
        namespace AA
        {
            public static class Ext
            {
                public static int ToInt(this C c)
                    => 1;
            }
        }

        namespace BB
        {
            public class B
            {
                public void M()
                {
                    var c = new C();
                    c.ToInt(){{commitChar}}
                }
            }
        }
        """, commitChar: commitChar, sourceCodeKind: SourceCodeKind.Regular);

    [Theory]
    [InlineData("int", true, "int a")]
    [InlineData("int[]", true, "int a, int b")]
    [InlineData("bool", false, null)]
    public async Task TestTargetTypedCompletion(string targetType, bool matchTargetType, string expectedParameterList)
    {
        var refDoc = """
            using System;

            namespace NS2
            {
                public static class Extensions
                {
                    public static int ExtensionMethod(this int t, int a) => 0;
                    public static int[] ExtensionMethod(this int t, int a, int b) => null;
                    public static string ExtensionMethod(this int t, int a, int b, int c) => false;
                }
            }
            """;
        var srcDoc = $$"""
            namespace NS1
            {
                public class C
                {
                    public void M(int x)
                    {
                        {{targetType}} y = x.$$
                    }
                }
            }
            """;

        ShowTargetTypedCompletionFilter = true;
        var markup = CreateMarkupForProjectWithProjectReference(srcDoc, refDoc, LanguageNames.CSharp, LanguageNames.CSharp);

        string expectedDescription = null;
        var expectedFilters = new List<CompletionFilter>()
        {
            FilterSet.ExtensionMethodFilter
        };

        if (matchTargetType)
        {
            expectedFilters.Add(FilterSet.TargetTypedFilter);
            expectedDescription = $"({CSharpFeaturesResources.extension}) {targetType} int.ExtensionMethod({expectedParameterList}) (+{NonBreakingSpaceString}2{NonBreakingSpaceString}{FeaturesResources.overloads_})";
        }

        await VerifyImportItemExistsAsync(
            markup,
            "ExtensionMethod",
            expectedFilters: expectedFilters,
            inlineDescription: "NS2",
            expectedDescriptionOrNull: expectedDescription);
    }

    [Fact]
    public Task TestComplexConstraint_NotShownOnObject()
        => VerifyItemIsAbsentAsync(
             """
             interface I
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T>(this T t) where T : I
                     {
                     }
                 }
             }

             void M(object i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestComplexConstraint_ShownOnExactInterfaceConstraintMatch()
        => VerifyItemExistsAsync(
             """
             interface I
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T>(this T t) where T : I
                     {
                     }
                 }
             }

             void M(I i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestComplexConstraint_ShownOnInterfaceMatchThroughChainedTypeParameter()
        => VerifyItemExistsAsync(
             """
             interface I
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T, U>(this T t) where T : U where U : I
                     {
                     }
                 }
             }

             void M(I i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestComplexConstraint_ShownOnInterfaceMatchThroughChainedTypeParameter_BaseInterface()
        => VerifyItemExistsAsync(
             """
             interface I1
             {
             }

             interface I2 : I1
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T, U>(this T t) where T : U where U : I1
                     {
                     }
                 }
             }

             void M(I2 i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestComplexConstraint_ShownOnExactBaseTypeConstraintMatch()
        => VerifyItemExistsAsync(
             """
             class C
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T>(this T t) where T : C
                     {
                     }
                 }
             }

             void M(C i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestComplexConstraint_ShownOnBaseTypeMatchThroughChainedTypeParameter()
        => VerifyItemExistsAsync(
             """
             class C
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T, U>(this T t) where T : U where U : C
                     {
                     }
                 }
             }

             void M(C i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestComplexConstraint_ShownOnBaseTypeMatchThroughChainedTypeParameter_InheritedType()
        => VerifyItemExistsAsync(
             """
             class C1
             {
             }

             class C2 : C1
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T, U>(this T t) where T : U where U : C1
                     {
                     }
                 }
             }

             void M(C2 i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact]
    public Task TestComplexConstraint_ShownOnBaseTypeMatchThroughChainedTypeParameter_InheritedBaseTypeAndInterface()
        => VerifyItemExistsAsync(
             """
             interface I1
             {
             }

             interface I2 : I1
             {
             }

             class C1 : I2
             {
             }

             namespace N
             {
                 static class Extensions
                 {
                     public static void M<T, U>(this T t) where T : U where U : I1
                     {
                     }
                 }
             }

             void M(C1 i)
             {
                 i.$$
             }
             """,
             "M",
             displayTextSuffix: "<>",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79096")]
    public Task TestTopLevelExpressionConfusedAsQualifiedType()
        => VerifyItemExistsAsync(
             """
             using Microsoft.Extensions.Hosting;

             var builder = new Builder();

             builder.Services.$$
             var host = builder.Build();

             class Builder
             {
                 public C Services { get; }
             }

             class C
             {
             }

             namespace N
             {
                 static class SC
                 {
                     public static void M(this C c)
                     {
                     }
                 }
             }
             """,
             "M",
             displayTextSuffix: "",
             inlineDescription: "N",
             sourceCodeKind: SourceCodeKind.Regular);

    [Theory, MemberData(nameof(BuiltInTypesIncludingArraysWithReferenceTypeData))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80561")]
    public async Task TestPredefinedType_ModernExtensionMethod(string type1, string type2, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Goo
            {
                public static class ExtensionClass
                {
                    extension({{type1}} x)
                    {
                        public bool ExtensionMethod()
                            => true;
                    }
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{type2}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Goo");
    }

    [Theory, MemberData(nameof(BuiltInTypesWithReferenceTypeData))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80561")]
    public async Task TestPredefinedType_ModernExtensionMethod_Static(string type1, string type2, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Goo
            {
                public static class ExtensionClass
                {
                    extension({{type1}})
                    {
                        public static bool ExtensionMethod()
                            => true;
                    }
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M()
                    {
                        {{type2}}.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Goo");
    }

    [Theory, MemberData(nameof(BuiltInTypesIncludingArraysWithReferenceTypeData))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80561")]
    public async Task TestPredefinedType_ModernExtensionMethod_Static_Alias(string type1, string type2, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Goo
            {
                public static class ExtensionClass
                {
                    extension({{type1}})
                    {
                        public static bool ExtensionMethod()
                            => true;
                    }
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                using T = {{type2}};
                public class Bat
                {
                    public void M()
                    {
                        T.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionMethod",
             glyph: Glyph.ExtensionMethodPublic,
             inlineDescription: "Goo");
    }

    [Theory, MemberData(nameof(BuiltInTypesIncludingArraysWithReferenceTypeData))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80561")]
    public async Task TestPredefinedType_ModernExtensionProperty(string type1, string type2, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Goo
            {
                public static class ExtensionClass
                {
                    extension({{type1}} x)
                    {
                        public bool ExtensionProp => true;
                    }
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M({{type2}} x)
                    {
                        x.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionProp",
             glyph: Glyph.PropertyPublic,
             inlineDescription: "Goo");
    }

    [Theory, MemberData(nameof(BuiltInTypesWithReferenceTypeData))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80561")]
    public async Task TestPredefinedType_ModernExtensionProperty_Static(string type1, string type2, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Goo
            {
                public static class ExtensionClass
                {
                    extension({{type1}})
                    {
                        public static bool ExtensionProp => true;
                    }
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                public class Bat
                {
                    public void M()
                    {
                        {{type2}}.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionProp",
             glyph: Glyph.PropertyPublic,
             inlineDescription: "Goo");
    }

    [Theory, MemberData(nameof(BuiltInTypesIncludingArraysWithReferenceTypeData))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/80561")]
    public async Task TestPredefinedType_ModernExtensionProperty_Static_Alias(string type1, string type2, ReferenceType refType)
    {
        var file1 = $$"""
            using System;

            namespace Goo
            {
                public static class ExtensionClass
                {
                    extension({{type1}})
                    {
                        public static bool ExtensionProp => true;
                    }
                }
            }
            """;
        var file2 = $$"""
            using System;

            namespace Baz
            {
                using T = {{type2}};
                public class Bat
                {
                    public void M()
                    {
                        T.$$
                    }
                }
            }
            """;

        var markup = GetMarkup(file2, file1, refType);

        await VerifyImportItemExistsAsync(
             markup,
             "ExtensionProp",
             glyph: Glyph.PropertyPublic,
             inlineDescription: "Goo");
    }
}
