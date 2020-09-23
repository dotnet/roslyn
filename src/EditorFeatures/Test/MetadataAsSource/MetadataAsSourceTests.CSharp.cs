// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public partial class MetadataAsSourceTests
    {
        [UseExportProvider]
        public class CSharp
        {
            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public void ExtractXMLFromDocComment()
            {
                var docCommentText = @"/// <summary>
/// I am the very model of a modern major general.
/// </summary>";

                var expectedXMLFragment = @" <summary>
 I am the very model of a modern major general.
 </summary>";

                var extractedXMLFragment = DocumentationCommentUtilities.ExtractXMLFragment(docCommentText, "///");

                Assert.Equal(expectedXMLFragment, extractedXMLFragment);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            [WorkItem(42986, "https://github.com/dotnet/roslyn/issues/42986")]
            public async Task TestNativeInteger()
            {
                var metadataSource = "public class C { public nint i; public nuint i2; }";
                var symbolName = "C";

                await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview",
                    expected: $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public nint i;
    public nuint i2;

    public C();
}}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public async Task TestInitOnlyProperty()
            {
                var metadataSource = @"public class C { public int Property { get; init; } }
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit { }
}
";
                var symbolName = "C";

                await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview",
                    expected: $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public int Property {{ get; init; }}
}}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public async Task TestTupleWithNames()
            {
                var metadataSource = "public class C { public (int a, int b) t; }";
                var symbolName = "C";

                await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp,
                    expected: $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Runtime.CompilerServices;

public class [|C|]
{{
    [TupleElementNames(new[] {{ ""a"", ""b"" }})]
    public (int a, int b) t;

    public C();
}}");
            }

            [Fact, WorkItem(26605, "https://github.com/dotnet/roslyn/issues/26605")]
            public async Task TestValueTuple()
            {
                using var context = TestContext.Create(LanguageNames.CSharp);
                await context.GenerateAndVerifySourceAsync("System.ValueTuple",
@$"#region {FeaturesResources.Assembly} System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
// System.ValueTuple.dll
#endregion

using System.Collections;

namespace System
{{
    public struct [|ValueTuple|] : IEquatable<ValueTuple>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<ValueTuple>, ITupleInternal
    {{
        public static ValueTuple Create();
        public static ValueTuple<T1> Create<T1>(T1 item1);
        public static (T1, T2) Create<T1, T2>(T1 item1, T2 item2);
        public static (T1, T2, T3) Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3);
        public static (T1, T2, T3, T4) Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4);
        public static (T1, T2, T3, T4, T5) Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5);
        public static (T1, T2, T3, T4, T5, T6) Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6);
        public static (T1, T2, T3, T4, T5, T6, T7) Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7);
        public static (T1, T2, T3, T4, T5, T6, T7, T8) Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8);
        public int CompareTo(ValueTuple other);
        public override bool Equals(object obj);
        public bool Equals(ValueTuple other);
        public override int GetHashCode();
        public override string ToString();
    }}
}}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
            public async Task TestExtendedPartialMethod1()
            {
                var metadataSource = "public partial class C { public partial void F(); public partial void F() { } }";
                var symbolName = "C";

                await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview",
                    expected: $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public void F();
}}");
            }
        }
    }
}
