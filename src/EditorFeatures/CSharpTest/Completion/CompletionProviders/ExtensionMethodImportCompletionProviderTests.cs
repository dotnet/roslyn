// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [UseExportProvider]
    public class ExtensionMethodImportCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        public ExtensionMethodImportCompletionProviderTests(CSharpTestWorkspaceFixture workspaceFixture) : base(workspaceFixture)
        {
        }

        private bool? ShowImportCompletionItemsOptionValue { get; set; } = true;

        // -1 would disable timebox, whereas 0 means always timeout.
        private int TimeoutInMilliseconds { get; set; } = -1;

        protected override void SetWorkspaceOptions(TestWorkspace workspace)
        {
            workspace.Options = workspace.Options
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, ShowImportCompletionItemsOptionValue)
                .WithChangedOption(CompletionServiceOptions.TimeoutInMillisecondsForImportCompletion, TimeoutInMilliseconds);
        }

        protected override ExportProvider GetExportProvider()
        {
            return ExportProviderCache
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(TestExperimentationService)))
                .CreateExportProvider();
        }

        internal override CompletionProvider CreateCompletionProvider()
        {
            return new ExtensionMethodImportCompletionProvider();
        }

        public enum ReferenceType
        {
            None,
            Project,
            Metadata
        }

        private static IEnumerable<object[]> CombineWithReferenceTypeData(IEnumerable<List<object>> data)
        {
            foreach (var refKind in Enum.GetValues(typeof(ReferenceType)))
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

        private static IEnumerable<List<object>> BuiltInTypes
        {
            get
            {
                var predefinedTypes = new List<List<string>>
                {
                    new List<string>()
                    { "int", "Int32", "System.Int32" },
                    new List<string>()
                    { "float", "Single", "System.Single" },
                    new List<string>()
                    { "uint", "UInt32", "System.UInt32" },
                    new List<string>()
                    { "bool", "Boolean", "System.Boolean"},
                    new List<string>()
                    { "string", "String", "System.String"},
                    new List<string>()
                    { "object", "Object", "System.Object"},
                };

                var arraySuffixes = new[] { "", "[]", "[,]" };

                foreach (var group in predefinedTypes)
                {
                    foreach (var type1 in group)
                    {
                        foreach (var type2 in group)
                        {
                            foreach (var suffix in arraySuffixes)
                            {
                                yield return new List<object>() { type1 + suffix, type2 + suffix };
                            }
                        }
                    }
                }
            }
        }

        private static string GetMarkup(string current, string referenced, ReferenceType refType,
                                        string currentLanguage = LanguageNames.CSharp,
                                        string referencedLanguage = LanguageNames.CSharp)
            => refType switch
            {
                ReferenceType.None => CreateMarkupForSingleProject(current, referenced, currentLanguage),
                ReferenceType.Project => GetMarkupWithReference(current, referenced, currentLanguage, referencedLanguage, true),
                ReferenceType.Metadata => GetMarkupWithReference(current, referenced, currentLanguage, referencedLanguage, false),
                _ => null,
            };

        public static IEnumerable<object[]> BuiltInTypesWithReferenceTypeData
            => CombineWithReferenceTypeData(BuiltInTypes);

        [MemberData(nameof(BuiltInTypesWithReferenceTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestPredefinedType(string type1, string type2, ReferenceType refType)
        {
            var file1 = $@"
using System;

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this {type1} x)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M({type2} x)
        {{
            x.$$
        }}
    }}
}}";

            var markup = GetMarkup(file2, file1, refType);

            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        [MemberData(nameof(ReferenceTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingAliasInDeclaration(ReferenceType refType)
        {
            var file1 = $@"
using System;
using MyInt = System.Int32;

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this MyInt x)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(int x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);

            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        [MemberData(nameof(ReferenceTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UsingAliasInUsage(ReferenceType refType)
        {
            var file1 = $@"
using System;

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this int x)
            => true;
    }}
}}";
            var file2 = $@"
using System;
using MyInt = System.Int32;

namespace Baz
{{
    public class Bat
    {{
        public void M(MyInt x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);

            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        [MemberData(nameof(AllTypeKindsWithReferenceTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task RegularType(string typeKind, ReferenceType refType)
        {
            var file1 = $@"
using System;

public {typeKind} MyType {{ }}

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this MyType t)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(MyType x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        [MemberData(nameof(AllTypeKindsWithReferenceTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ObjectType(string typeKind, ReferenceType refType)
        {
            var file1 = $@"
using System;

public {typeKind} MyType {{ }}

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this object t)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(MyType x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        public static IEnumerable<object[]> TupleWithRefTypeData => CombineWithReferenceTypeData(
            (new[]
            {
                "(int, int)",
                "(int, (int, int))",
                "(string a, string b)"
            }).Select(tuple => new List<object>() { tuple }));

        [MemberData(nameof(TupleWithRefTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ValueTupleType(string tupleType, ReferenceType refType)
        {
            var file1 = $@"
using System;

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this {tupleType} t)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M({tupleType} x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        public static IEnumerable<object[]> DerivableTypeKindsWithReferenceTypeData
            => CombineWithReferenceTypeData((new[] { "class", "interface", "abstract class" }).Select(kind => new List<object>() { kind }));

        [MemberData(nameof(DerivableTypeKindsWithReferenceTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task RegularTypeAsBase(string baseType, ReferenceType refType)
        {
            var file1 = $@"
using System;

public {baseType} MyBase {{ }}

public class MyType : MyBase {{ }}

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this MyBase t)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(MyType x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        public static IEnumerable<object[]> BounedGenericTypeWithRefTypeData => CombineWithReferenceTypeData(
            (new[]
            {
                "IEnumerable<string>",
                "List<string>",
                "string[]"
            }).Select(tuple => new List<object>() { tuple }));

        [MemberData(nameof(BounedGenericTypeWithRefTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task BoundedGenericType(string type, ReferenceType refType)
        {
            var file1 = $@"
using System;
using System.Collections.Generic;

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this IEnumerable<string> t)
            => true;
    }}
}}";
            var file2 = $@"
using System;
using System.Collections.Generic;

namespace Baz
{{
    public class Bat
    {{
        public void M({type} x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
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

        [MemberData(nameof(TypeParameterWithRefTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MatchingTypeParameter(string type, ReferenceType refType)
        {
            var file1 = $@"
using System;

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod<T>(this T t)
            => true;
    }}
}}";
            var file2 = $@"
using System;
using System.Collections.Generic;

namespace Baz
{{
    public interface Bar {{}}

    public class Bat
    {{
        public void M({type} x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 displayTextSuffix: "<>",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        [InlineData(ReferenceType.Project)]
        [InlineData(ReferenceType.Metadata)]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInternalExtensionMethods_NoIVT_InReference(ReferenceType refType)
        {
            var file1 = $@"
using System;

namespace Foo
{{
    internal static class ExtensionClass
    {{
        public static bool ExtentionMethod(this int x)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(int x)
        {{
            x.$$
        }}
    }}
}}";

            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemIsAbsentAsync(
                 markup,
                 "ExtentionMethod",
                 inlineDescription: "Foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInternalExtensionMethods_NoIVT_InSameProject()
        {
            var file1 = $@"
using System;

namespace Foo
{{
    internal static class ExtensionClass
    {{
        internal static bool ExtentionMethod(this int x)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(int x)
        {{
            x.$$
        }}
    }}
}}";

            var markup = GetMarkup(file2, file1, ReferenceType.None);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodInternal,     // This is based on declared accessibility
                 inlineDescription: "Foo");
        }

        [InlineData(ReferenceType.None)]
        [InlineData(ReferenceType.Project)]
        [InlineData(ReferenceType.Metadata, Skip = "SymbolTreeInfo explicitly ignores non-public types from metadata(likely for perf reasons). Need to decide if this is the design we'd like.")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task TestInternalExtensionMethods_WithIVT(ReferenceType refType)
        {
            var file1 = $@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Project1"")]

namespace Foo
{{
    internal static class ExtensionClass
    {{
        internal static bool ExtentionMethod(this int x)
            => true;
    }}
}}";
            var file2 = $@"
namespace Baz
{{
    public class Bat
    {{
        public void M(int x)
        {{
            x.$$
        }}
    }}
}}";

            var markup = GetMarkup(file2, file1, refType);
            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodInternal,
                 inlineDescription: "Foo");
        }

        [MemberData(nameof(ReferenceTypeData))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task UserDefinedGenericType(ReferenceType refType)
        {
            var file1 = $@"
using System;

public class MyGeneric<T>
{{
}}

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this MyGeneric<int> x)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(MyGeneric<int> x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, refType);

            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Foo");
        }

        [InlineData("(1 + 1)")]
        [InlineData("(new int())")]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task MethodSymbolReceiver(string expression)
        {
            var file1 = $@"
using System;

namespace Foo
{{
    public static class ExtensionClass
    {{
        public static bool ExtentionMethod(this int x)
            => true;
    }}
}}";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M()
        {{
            {expression}.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, ReferenceType.None);

            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
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

        [MemberData(nameof(VBBuiltInTypes))]
        [Theory, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodDelcaredInVBSource(string vbType, string csType)
        {
            var file1 = $@"
Imports System
Imports System.Runtime.CompilerServices

Namespace NS
    Public Module Foo
        <Extension>
        public Function ExtentionMethod(x As {vbType}) As Boolean
            Return True
        End Function
    End Module
End Namespace";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M({csType} x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = GetMarkup(file2, file1, ReferenceType.Project, currentLanguage: LanguageNames.CSharp, referencedLanguage: LanguageNames.VisualBasic);

            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "NS");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodDelcaredInRootNamespaceVBSource()
        {
            var file1 = $@"
Imports System
Imports System.Runtime.CompilerServices

Public Module Foo
    <Extension>
    public Function ExtentionMethod(x As Integer) As Boolean
        Return True
    End Function
End Module";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(int x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp, rootNamespace: "Root");

            await VerifyTypeImportItemExistsAsync(
                 markup,
                 "ExtentionMethod",
                 glyph: (int)Glyph.ExtensionMethodPublic,
                 inlineDescription: "Root");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public async Task ExtensionMethodDelcaredInGlobalNamespaceVBSource()
        {
            var file1 = $@"
Imports System
Imports System.Runtime.CompilerServices

Public Module Foo
    <Extension>
    public Function ExtentionMethod(x As Integer) As Boolean
        Return True
    End Function
End Module";
            var file2 = $@"
using System;

namespace Baz
{{
    public class Bat
    {{
        public void M(int x)
        {{
            x.$$
        }}
    }}
}}";
            var markup = CreateMarkupForProjecWithVBProjectReference(file2, file1, sourceLanguage: LanguageNames.CSharp);

            await VerifyTypeImportItemIsAbsentAsync(
                 markup,
                 "ExtentionMethod",
                 inlineDescription: "");
        }

        private Task VerifyTypeImportItemExistsAsync(string markup, string expectedItem, int glyph, string inlineDescription, string displayTextSuffix = null, string expectedDescriptionOrNull = null)
        {
            return VerifyItemExistsAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, glyph: glyph, inlineDescription: inlineDescription, expectedDescriptionOrNull: expectedDescriptionOrNull);
        }

        private Task VerifyTypeImportItemIsAbsentAsync(string markup, string expectedItem, string inlineDescription, string displayTextSuffix = null)
        {
            return VerifyItemIsAbsentAsync(markup, expectedItem, displayTextSuffix: displayTextSuffix, inlineDescription: inlineDescription);
        }
    }
}
