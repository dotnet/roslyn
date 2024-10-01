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
    public class VisualBasic : AbstractMetadataAsSourceTests
    {
        [Theory, CombinatorialData, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530123")]
        public async Task TestGenerateTypeInModule(bool signaturesOnly)
        {
            var metadataSource = @"
Module M
    Public Class D
    End Class
End Module";

            var expected = signaturesOnly switch
            {
                true => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Friend Module M
    Public Class [|D|]
        Public Sub New()
    End Class
End Module",
                false => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using Microsoft.VisualBasic.CompilerServices;

[StandardModule]
internal sealed class M
{{
    public class [|D|]
    {{
    }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 9)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(FeaturesResources.Resolve_0, "Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
{string.Format(FeaturesResources.Found_single_assembly_0, "Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")}
{string.Format(FeaturesResources.Load_from_0, "Microsoft.VisualBasic (net461)")}
#endif",
            };

            await GenerateAndVerifySourceAsync(metadataSource, "M+D", LanguageNames.VisualBasic, expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/60253")]
        public async Task TestReferenceAssembly(bool signaturesOnly)
        {
            var metadataSource = @"
<Assembly: System.Runtime.CompilerServices.ReferenceAssembly>
Module M
    Public Class D
    End Class
End Module";

            var expected = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Friend Module M
    Public Class [|D|]
        Public Sub New()
    End Class
End Module";

            await GenerateAndVerifySourceAsync(metadataSource, "M+D", LanguageNames.VisualBasic, expected, signaturesOnly: signaturesOnly);
        }

        // This test depends on the version of mscorlib used by the TestWorkspace and may 
        // change in the future
        [Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530526")]
        [InlineData(false, Skip = "https://github.com/dotnet/roslyn/issues/52415")]
        [InlineData(true)]
        public async Task BracketedIdentifierSimplificationTest(bool signaturesOnly)
        {
            var expected = signaturesOnly switch
            {
                true => $@"#Region ""{FeaturesResources.Assembly} mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089""
' mscorlib.v4_6_1038_0.dll
#End Region

Imports System.Runtime.InteropServices

Namespace System
    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Struct Or AttributeTargets.Enum Or AttributeTargets.Constructor Or AttributeTargets.Method Or AttributeTargets.Property Or AttributeTargets.Field Or AttributeTargets.Event Or AttributeTargets.Interface Or AttributeTargets.Delegate, Inherited:=False)> <ComVisible(True)>
    Public NotInheritable Class [|ObsoleteAttribute|]
        Inherits Attribute

        Public Sub New()
        Public Sub New(message As String)
        Public Sub New(message As String, [error] As Boolean)

        Public ReadOnly Property Message As String
        Public ReadOnly Property IsError As Boolean
    End Class
End Namespace",
                false => $@"#region {FeaturesResources.Assembly} mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

namespace System;

[Serializable]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, Inherited = false)]
[ComVisible(true)]
public sealed class [|ObsoleteAttribute|] : Attribute
{{
    public string Message
    {{
        get
        {{
            /*Error: Empty body found. Decompiled assembly might be a reference assembly.*/
            ;
        }}
    }}

    public bool IsError
    {{
        get
        {{
            /*Error: Empty body found. Decompiled assembly might be a reference assembly.*/
            ;
        }}
    }}

    public ObsoleteAttribute()
    {{
        /*Error: Empty body found. Decompiled assembly might be a reference assembly.*/
        ;
    }}

    public ObsoleteAttribute(string message)
    {{
        /*Error: Empty body found. Decompiled assembly might be a reference assembly.*/
        ;
    }}

    public ObsoleteAttribute(string message, bool error)
    {{
        /*Error: Empty body found. Decompiled assembly might be a reference assembly.*/
        ;
    }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 9)}
#endif",
            };

            using var context = TestContext.Create(LanguageNames.VisualBasic);
            await context.GenerateAndVerifySourceAsync("System.ObsoleteAttribute", expected, signaturesOnly: signaturesOnly);
        }

        [Fact]
        public void ExtractXMLFromDocComment()
        {
            var docCommentText = @"''' <summary>
''' I am the very model of a modern major general.
''' </summary>";

            var expectedXMLFragment = @" <summary>
 I am the very model of a modern major general.
 </summary>";

            var extractedXMLFragment = DocumentationCommentUtilities.ExtractXMLFragment(docCommentText, "'''");

            Assert.Equal(expectedXMLFragment, extractedXMLFragment);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/26605")]
        public async Task TestValueTuple(bool signaturesOnly)
        {
            using var context = TestContext.Create(LanguageNames.VisualBasic);

            var expected = signaturesOnly switch
            {
                true => $@"#Region ""{FeaturesResources.Assembly} System.ValueTuple, Version=4.0.3.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51""
' System.ValueTuple (net461)
#End Region

Imports System.Collections

Namespace System
    Public Structure [|ValueTuple|]
        Implements IEquatable(Of ValueTuple), IStructuralEquatable, IStructuralComparable, IComparable, IComparable(Of ValueTuple), ITupleInternal

        Public Shared Function Create() As ValueTuple
        Public Shared Function Create(Of T1)(item1 As T1) As ValueTuple(Of T1)
        Public Shared Function Create(Of T1, T2)(item1 As T1, item2 As T2) As (T1, T2)
        Public Shared Function Create(Of T1, T2, T3)(item1 As T1, item2 As T2, item3 As T3) As (T1, T2, T3)
        Public Shared Function Create(Of T1, T2, T3, T4)(item1 As T1, item2 As T2, item3 As T3, item4 As T4) As (T1, T2, T3, T4)
        Public Shared Function Create(Of T1, T2, T3, T4, T5)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5) As (T1, T2, T3, T4, T5)
        Public Shared Function Create(Of T1, T2, T3, T4, T5, T6)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6) As (T1, T2, T3, T4, T5, T6)
        Public Shared Function Create(Of T1, T2, T3, T4, T5, T6, T7)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7) As (T1, T2, T3, T4, T5, T6, T7)
        Public Shared Function Create(Of T1, T2, T3, T4, T5, T6, T7, T8)(item1 As T1, item2 As T2, item3 As T3, item4 As T4, item5 As T5, item6 As T6, item7 As T7, item8 As T8) As (T1, T2, T3, T4, T5, T6, T7, T8)
        Public Overrides Function Equals(obj As Object) As Boolean
        Public Function Equals(other As ValueTuple) As Boolean
        Public Function CompareTo(other As ValueTuple) As Integer
        Public Overrides Function GetHashCode() As Integer
        Public Overrides Function ToString() As String
    End Structure
End Namespace",
                false => $@"#region {FeaturesResources.Assembly} System.ValueTuple, Version=4.0.3.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Collections;
using System.Numerics.Hashing;
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
        return HashHelpers.Combine(HashHelpers.Combine(HashHelpers.RandomSeed, h1), h2);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3)
    {{
        return HashHelpers.Combine(CombineHashCodes(h1, h2), h3);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4)
    {{
        return HashHelpers.Combine(CombineHashCodes(h1, h2, h3), h4);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
    {{
        return HashHelpers.Combine(CombineHashCodes(h1, h2, h3, h4), h5);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6)
    {{
        return HashHelpers.Combine(CombineHashCodes(h1, h2, h3, h4, h5), h6);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7)
    {{
        return HashHelpers.Combine(CombineHashCodes(h1, h2, h3, h4, h5, h6), h7);
    }}

    internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8)
    {{
        return HashHelpers.Combine(CombineHashCodes(h1, h2, h3, h4, h5, h6, h7), h8);
    }}
}}
#if false // {FeaturesResources.Decompilation_log}
{string.Format(FeaturesResources._0_items_in_cache, 9)}
------------------
{string.Format(FeaturesResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(FeaturesResources.Resolve_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Found_single_assembly_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(FeaturesResources.Load_from_0, "System.v4_6_1038_0.dll")}
#endif",
            };

            await context.GenerateAndVerifySourceAsync("System.ValueTuple", expected, signaturesOnly: signaturesOnly);
        }
    }
}
