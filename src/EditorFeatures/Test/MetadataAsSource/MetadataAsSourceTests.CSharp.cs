// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource;

public partial class MetadataAsSourceTests
{
    [Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
    public class CSharp : AbstractMetadataAsSourceTests
    {
        [Fact]
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

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/42986")]
        public async Task TestNativeInteger(bool signaturesOnly)
        {
            var metadataSource = "public class C { public nint i; public nuint i2; }";
            var symbolName = "C";

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public nint i;
    public nuint i2;

    public C();
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
    public nint i;

    public nuint i2;
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview", expected: expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData]
        public async Task TestInitOnlyProperty(bool signaturesOnly)
        {
            var metadataSource = @"public class C { public int Property { get; init; } }
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit { }
}
";
            var symbolName = "C";

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public int Property {{ get; init; }}
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
    public int Property {{ get; init; }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview", expected: expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData]
        public async Task TestTupleWithNames(bool signaturesOnly)
        {
            var metadataSource = "public class C { public (int a, int b) t; }";
            var symbolName = "C";

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Runtime.CompilerServices;

public class [|C|]
{{
    [TupleElementNames(new[] {{ ""a"", ""b"" }})]
    public (int a, int b) t;

    public C();
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
    public (int a, int b) t;
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(FeaturesResources.Resolve_0, "System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")}
{string.Format(FeaturesResources.Found_single_assembly_0, "System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51")}
{string.Format(FeaturesResources.Load_from_0, "System.ValueTuple.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, expected: expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/26605")]
        public async Task TestValueTuple(bool signaturesOnly)
        {
            using var context = TestContext.Create(LanguageNames.CSharp);

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
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
}}",
                false => $@"#region {FeaturesResources.Assembly} System.ValueTuple, Version=4.0.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
// System.ValueTuple.dll
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Collections;
using System.Runtime.InteropServices;

namespace System;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct [|ValueTuple|] : IEquatable<ValueTuple>, IStructuralEquatable, IStructuralComparable, IComparable, IComparable<ValueTuple>, ITupleInternal
{{
    int ITupleInternal.Size => 0;

    public override bool Equals(object obj)
    {{
        return obj is ValueTuple;
    }}

    public bool Equals(ValueTuple other)
    {{
        return true;
    }}

    bool IStructuralEquatable.Equals(object other, IEqualityComparer comparer)
    {{
        return other is ValueTuple;
    }}

    int IComparable.CompareTo(object other)
    {{
        if (other == null)
        {{
            return 1;
        }}

        if (!(other is ValueTuple))
        {{
            throw new ArgumentException(System.SR.ArgumentException_ValueTupleIncorrectType, ""other"");
        }}

        return 0;
    }}

    public int CompareTo(ValueTuple other)
    {{
        return 0;
    }}

    int IStructuralComparable.CompareTo(object other, IComparer comparer)
    {{
        if (other == null)
        {{
            return 1;
        }}

        if (!(other is ValueTuple))
        {{
            throw new ArgumentException(System.SR.ArgumentException_ValueTupleIncorrectType, ""other"");
        }}

        return 0;
    }}

    public override int GetHashCode()
    {{
        return 0;
    }}

    int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
    {{
        return 0;
    }}

    int ITupleInternal.GetHashCode(IEqualityComparer comparer)
    {{
        return 0;
    }}

    public override string ToString()
    {{
        return ""()"";
    }}

    string ITupleInternal.ToStringEnd()
    {{
        return "")"";
    }}

    public static ValueTuple Create()
    {{
        return default(ValueTuple);
    }}

    public static ValueTuple<T1> Create<T1>(T1 item1)
    {{
        return new ValueTuple<T1>(item1);
    }}

    public static (T1, T2) Create<T1, T2>(T1 item1, T2 item2)
    {{
        return (item1, item2);
    }}

    public static (T1, T2, T3) Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3)
    {{
        return (item1, item2, item3);
    }}

    public static (T1, T2, T3, T4) Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4)
    {{
        return (item1, item2, item3, item4);
    }}

    public static (T1, T2, T3, T4, T5) Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
    {{
        return (item1, item2, item3, item4, item5);
    }}

    public static (T1, T2, T3, T4, T5, T6) Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
    {{
        return (item1, item2, item3, item4, item5, item6);
    }}

    public static (T1, T2, T3, T4, T5, T6, T7) Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
    {{
        return (item1, item2, item3, item4, item5, item6, item7);
    }}

    public static (T1, T2, T3, T4, T5, T6, T7, T8) Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
    {{
        return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>(item1, item2, item3, item4, item5, item6, item7, Create(item8));
    }}

    internal static int CombineHashCodes(int h1, int h2)
    {{
        return ((h1 << 5) + h1) ^ h2;
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3)
    {{
        return CombineHashCodes(CombineHashCodes(h1, h2), h3);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4)
    {{
        return CombineHashCodes(CombineHashCodes(h1, h2), CombineHashCodes(h3, h4));
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
    {{
        return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), h5);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6)
    {{
        return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6));
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7)
    {{
        return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7));
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8)
    {{
        return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), CombineHashCodes(h5, h6, h7, h8));
    }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
{string.Format(FeaturesResources.Found_single_assembly_0, "System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
{string.Format(FeaturesResources.WARN_Version_mismatch_Expected_0_Got_1, "4.0.0.0", "4.0.20.0")}
{string.Format(FeaturesResources.Load_from_0, "System.Runtime.dll")}
------------------
{string.Format(FeaturesResources.Resolve_0, "System.Resources.ResourceManager, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
{string.Format(FeaturesResources.Could_not_find_by_name_0, "System.Resources.ResourceManager, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
------------------
{string.Format(FeaturesResources.Resolve_0, "System.Collections, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
{string.Format(FeaturesResources.Could_not_find_by_name_0, "System.Collections, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(FeaturesResources.Resolve_0, "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "System.Core.v4_0_30319_17929.dll")}
------------------
{string.Format(FeaturesResources.Resolve_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "System.v4_6_1038_0.dll")}
------------------
{string.Format(FeaturesResources.Resolve_0, "System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Could_not_find_by_name_0, "System.ComponentModel.Composition, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
#endif",
            };

            await context.GenerateAndVerifySourceAsync("System.ValueTuple", expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData]
        public async Task TestExtendedPartialMethod1(bool signaturesOnly)
        {
            var metadataSource = "public partial class C { public partial void F(); public partial void F() { } }";
            var symbolName = "C";

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public void F();
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
    public void F()
    {{
    }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview", expected: expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/44566")]
        public async Task TestRecordType(bool signaturesOnly)
        {
            var metadataSource = "public record R;";
            var symbolName = "R";

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Text;

public record [|R|] : IEquatable<R>
{{
    public R();
    [CompilerGenerated]
    protected R(R original);

    [CompilerGenerated]
    protected virtual Type EqualityContract {{ get; }}

    [CompilerGenerated]
    public virtual R <Clone>$();
    [CompilerGenerated]
    public override bool Equals(object? obj);
    [CompilerGenerated]
    public virtual bool Equals(R? other);
    [CompilerGenerated]
    public override int GetHashCode();
    [CompilerGenerated]
    public override string ToString();
    [CompilerGenerated]
    protected virtual bool PrintMembers(StringBuilder builder);

    [CompilerGenerated]
    public static bool operator ==(R? left, R? right);
    [CompilerGenerated]
    public static bool operator !=(R? left, R? right);
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public record [|R|];
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, expected: expected, signaturesOnly: signaturesOnly);
        }

        /// <summary>
        /// This test must be updated when we switch to a new version of the decompiler that supports checked ops.
        /// </summary>
        [Theory, CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/42986")]
        public async Task TestCheckedOperators(bool signaturesOnly)
        {
            var metadataSource = @"
public class C
{
    public static explicit operator string(C x) => throw new System.Exception();

    public static explicit operator checked string(C x) => throw new System.Exception();

    public static C operator -(C x) => throw new System.Exception();

    public static C operator checked -(C x) => throw new System.Exception();

    public static C operator +(C x, C y) => throw new System.Exception();

    public static C operator checked +(C x, C y) => throw new System.Exception();
}";
            var symbolName = "C";

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public static C operator +(C x, C y);
    public static C operator checked +(C x, C y);
    public static C operator -(C x);
    public static C operator checked -(C x);

    public static explicit operator string(C x);
    public static explicit operator checked string(C x);
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

public class [|C|]
{{
    public static explicit operator string(C x)
    {{
        throw new Exception();
    }}

    public static explicit operator checked string(C x)
    {{
        throw new Exception();
    }}

    public static C operator -(C x)
    {{
        throw new Exception();
    }}

    public static C operator checked -(C x)
    {{
        throw new Exception();
    }}

    public static C operator +(C x, C y)
    {{
        throw new Exception();
    }}

    public static C operator checked +(C x, C y)
    {{
        throw new Exception();
    }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview", expected: expected, signaturesOnly: signaturesOnly);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60567")]
        public async Task TestStaticInterfaceMembers()
        {
            var metadataSource = @"
interface I<T> where T : I<T>
{
    static abstract T P { get; set; }
    static abstract event System.Action E;
    static abstract void M();
    static void NonAbstract() { }
    static abstract T operator +(T l, T r);
    static abstract bool operator ==(T l, T r);
    static abstract bool operator !=(T l, T r);
    static abstract implicit operator T(string s);
    static abstract explicit operator string(T t);
}";
            var symbolName = "I`1.M";

            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

internal interface I<T> where T : I<T>
{{
    static abstract T P {{ get; set; }}

    static abstract event Action E;

    static abstract void [|M|]();
    static void NonAbstract();

    static abstract T operator +(T l, T r);
    static abstract bool operator ==(T l, T r);
    static abstract bool operator !=(T l, T r);

    static abstract implicit operator T(string s);
    static abstract explicit operator string(T t);
}}";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview", expected: expected, signaturesOnly: true, metadataCommonReferences: "CommonReferencesNet6");
        }

        [Theory, CombinatorialData]
        public async Task UnsignedRightShift(bool signaturesOnly)
        {
            var metadataSource = "public class C { public static C operator >>>(C x, int y) => x; }";
            var symbolName = "C.op_UnsignedRightShift";

            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public static C operator [|>>>|](C x, int y);
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public static C operator [|>>>|](C x, int y)
    {{
        return x;
    }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, expected: expected, signaturesOnly: signaturesOnly, languageVersion: "Preview", metadataLanguageVersion: "Preview");
        }

        [Theory, CombinatorialData]
        public async Task TestRequiredProperty(bool signaturesOnly)
        {
            var metadataSource = """
                public class C
                {
                    public required int Property { get; set; }
                    public required int Field;
                }
                namespace System.Runtime.CompilerServices
                {
                    public sealed class RequiredMemberAttribute : Attribute { }
                    public sealed class CompilerFeatureRequiredAttribute : Attribute
                    {
                        public CompilerFeatureRequiredAttribute(string featureName)
                        {
                            FeatureName = featureName;
                        }
                        public string FeatureName { get; }
                        public bool IsOptional { get; set; }
                    }
                }

                """;
            var symbolName = "C";

            // ICSharpDecompiler does not yet support decoding required members nicely
            var expected = signaturesOnly switch
            {
                true => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public required int Field;

    public C();

    public required int Property {{ get; set; }}
}}",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
    public required int Field;

    public required int Property {{ get; set; }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 6)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, languageVersion: "Preview", metadataLanguageVersion: "Preview", expected: expected, signaturesOnly: signaturesOnly);
        }
    }
}
