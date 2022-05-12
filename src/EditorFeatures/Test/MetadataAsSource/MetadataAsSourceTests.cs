// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editor.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using System.Threading;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public partial class MetadataAsSourceTests : AbstractMetadataAsSourceTests
    {
        public enum OriginatingProjectLanguage
        {
            CSharp,
            VisualBasic,
        }

        private static string ToLanguageName(OriginatingProjectLanguage language)
            => language switch
            {
                OriginatingProjectLanguage.CSharp => LanguageNames.CSharp,
                OriginatingProjectLanguage.VisualBasic => LanguageNames.VisualBasic,
                _ => throw ExceptionUtilities.UnexpectedValue(language),
            };

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestClass(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C {}";
            var symbolName = "C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|C|]
    Public Sub New()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546241, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546241")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInterface(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public interface I {}";
            var symbolName = "I";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public interface [|I|]
{{
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Interface [|I|]
End Interface",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public interface [|I|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public interface [|I|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestConstructor(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C {}";
            var symbolName = "C..ctor";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public [|C|]();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub [|New|]()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestMethod(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C { public void Goo() {} }";
            var symbolName = "C.Goo";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public void [|Goo|]();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Public Sub [|Goo|]()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public void [|Goo|]()
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public void [|Goo|]()
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestField(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C { public string S; }";
            var symbolName = "C.S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public string [|S|];

    public C();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public [|S|] As String

    Public Sub New()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public string [|S|];
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public string [|S|];
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546240, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546240")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestProperty(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C { public string S { get; protected set; } }";
            var symbolName = "C.S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public string [|S|] {{ get; protected set; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Public Property [|S|] As String
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public string [|S|] {{ get; protected set; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public string [|S|] {{ get; protected set; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546194, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546194")]
        [WorkItem(546291, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546291")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEvent(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "using System; public class C { public event Action E; }";
            var symbolName = "C.E";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

public class C
{{
    public C();

    public event Action [|E|];
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

Public Class C
    Public Sub New()

    Public Event [|E|] As Action
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

public class C
{{
    public event Action [|E|];
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

public class C
{{
    public event Action [|E|];
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNestedType(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C { protected class D { } }";
            var symbolName = "C+D";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    protected class [|D|]
    {{
        public D();
    }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Protected Class [|D|]
        Public Sub New()
    End Class
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    protected class [|D|]
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    protected class [|D|]
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546195"), WorkItem(546269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546269")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnum(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public enum E { A, B, C }";
            var symbolName = "E";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum [|E|]
{{
    A = 0,
    B = 1,
    C = 2
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum [|E|]
    A = 0
    B = 1
    C = 2
End Enum",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum [|E|]
{{
    A,
    B,
    C
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum [|E|]
{{
    A,
    B,
    C
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546195"), WorkItem(546269, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546269")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumFromField(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public enum E { A, B, C }";
            var symbolName = "E.C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E
{{
    A = 0,
    B = 1,
    [|C|] = 2
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E
    A = 0
    B = 1
    [|C|] = 2
End Enum",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E
{{
    A,
    B,
    [|C|]
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E
{{
    A,
    B,
    [|C|]
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546273, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546273")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumWithUnderlyingType(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public enum E : short { A = 0, B = 1, C = 2 }";
            var symbolName = "E.C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : short
{{
    A = 0,
    B = 1,
    [|C|] = 2
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E As Short
    A = 0
    B = 1
    [|C|] = 2
End Enum",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E : short
{{
    A,
    B,
    [|C|]
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E : short
{{
    A,
    B,
    [|C|]
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(650741, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/650741")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumWithOverflowingUnderlyingType(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public enum E : ulong { A = 9223372036854775808 }";
            var symbolName = "E.A";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : ulong
{{
    [|A|] = 9223372036854775808
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E As ULong
    [|A|] = 9223372036854775808UL
End Enum",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E : ulong
{{
    [|A|] = 9223372036854775808uL
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E : ulong
{{
    [|A|] = 9223372036854775808uL
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumWithDifferentValues(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public enum E : short { A = 1, B = 2, C = 3 }";
            var symbolName = "E.C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : short
{{
    A = 1,
    B = 2,
    [|C|] = 3
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E As Short
    A = 1
    B = 2
    [|C|] = 3
End Enum",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E : short
{{
    A = 1,
    B,
    [|C|]
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public enum E : short
{{
    A = 1,
    B,
    [|C|]
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546198")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestTypeInNamespace(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "namespace N { public class C {} }";
            var symbolName = "N.C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

namespace N
{{
    public class [|C|]
    {{
        public C();
    }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Namespace N
    Public Class [|C|]
        Public Sub New()
    End Class
End Namespace",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

namespace N
{{
    public class [|C|]
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

namespace N
{{
    public class [|C|]
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546198")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestTypeInFileScopedNamespace1()
        {
            var metadataSource = "namespace N { public class C {} }";

            using var context = TestContext.Create(
                LanguageNames.CSharp, SpecializedCollections.SingletonEnumerable(metadataSource), languageVersion: "10");

            context.Workspace.SetOptions(context.Workspace.Options.WithChangedOption(
                CSharpCodeStyleOptions.NamespaceDeclarations,
                new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.FileScoped, NotificationOption2.Silent)));

            await context.GenerateAndVerifySourceAsync("N.C",
                $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

namespace N;

public class [|C|]
{{
    public C();
}}");
        }

        [WorkItem(546198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546198")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestTypeInFileScopedNamespace2()
        {
            var metadataSource = "namespace N { public class C {} }";

            using var context = TestContext.Create(
                LanguageNames.CSharp, SpecializedCollections.SingletonEnumerable(metadataSource), languageVersion: "9");

            context.Workspace.SetOptions(context.Workspace.Options.WithChangedOption(
                CSharpCodeStyleOptions.NamespaceDeclarations,
                new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.FileScoped, NotificationOption2.Silent)));

            await context.GenerateAndVerifySourceAsync("N.C",
                $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

namespace N
{{
    public class [|C|]
    {{
        public C();
    }}
}}");
        }

        [WorkItem(546198, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546198")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestTypeInFileScopedNamespace3()
        {
            var metadataSource = "namespace N { public class C {} }";

            using var context = TestContext.Create(
                LanguageNames.CSharp, SpecializedCollections.SingletonEnumerable(metadataSource), languageVersion: "10");

            context.Workspace.SetOptions(context.Workspace.Options.WithChangedOption(
                CSharpCodeStyleOptions.NamespaceDeclarations,
                new CodeStyleOption2<NamespaceDeclarationPreference>(NamespaceDeclarationPreference.BlockScoped, NotificationOption2.Silent)));

            await context.GenerateAndVerifySourceAsync("N.C",
                $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

namespace N
{{
    public class [|C|]
    {{
        public C();
    }}
}}");
        }

        [WorkItem(546223, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546223")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInlineConstant(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"public class C { public const string S = ""Hello mas""; }";
            var symbolName = "C.S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public const string [|S|] = ""Hello mas"";

    public C();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Const [|S|] As String = ""Hello mas""

    Public Sub New()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public const string [|S|] = ""Hello mas"";
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public const string [|S|] = ""Hello mas"";
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546221, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546221")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInlineTypeOf(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
using System;

public class MyTypeAttribute : Attribute
{
    public MyTypeAttribute(Type type) {}
}

[MyType(typeof(string))]
public class C {}";

            var symbolName = "C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

[MyType(typeof(string))]
public class [|C|]
{{
    public C();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

<MyType(GetType(String))>
Public Class [|C|]
    Public Sub New()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

[MyType(typeof(string))]
public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

[MyType(typeof(string))]
public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546231, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546231")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNoDefaultConstructorInStructs(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public struct S {}";
            var symbolName = "S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct [|S|]
{{
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure [|S|]
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct [|S|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct [|S|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReferenceDefinedType(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C { public static C Create() { return new C(); } }";
            var symbolName = "C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public static C Create();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|C|]
    Public Sub New()

    Public Shared Function Create() As C
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
    public static C Create()
    {{
        return new C();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]
{{
    public static C Create()
    {{
        return new C();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546227")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestGenericType(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class G<SomeType> { public SomeType S; }";
            var symbolName = "G`1";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|G|]<SomeType>
{{
    public SomeType S;

    public G();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|G|](Of SomeType)
    Public S As SomeType

    Public Sub New()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|G|]<SomeType>
{{
    public SomeType S;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|G|]<SomeType>
{{
    public SomeType S;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        [WorkItem(38916, "https://github.com/dotnet/roslyn/issues/38916")]
        public async Task TestParameterAttributes(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public class C<[My] T>
{
    public void Method([My] T x, [My] T y) { }
}

internal class MyAttribute : System.Attribute { }
";
            var symbolName = "C`1";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]<[MyAttribute] T>
{{
    public C();

    public void Method([MyAttribute] T x, [MyAttribute] T y);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|C|](Of T)
    Public Sub New()

    Public Sub Method(<MyAttribute> x As T, <MyAttribute> y As T)
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]<[My] T>
{{
    public void Method([My] T x, [My] T y)
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|C|]<[My] T>
{{
    public void Method([My] T x, [My] T y)
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        [WorkItem(38916, "https://github.com/dotnet/roslyn/issues/38916")]
        public async Task TestGenericWithNullableReferenceTypes(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
#nullable enable
public interface C<T>
{
    bool Equals([AllowNull] T other);
}

internal class AllowNullAttribute : System.Attribute { }
";
            var symbolName = "C`1";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public interface [|C|]<T>
{{
    bool Equals([AllowNullAttribute] T other);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region


<NullableContextAttribute(1)>
Public Interface [|C|](Of T)
    Function Equals(<AllowNullAttribute> other As T) As Boolean
End Interface",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public interface [|C|]<T>
{{
    bool Equals([AllowNull] T other);
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public interface [|C|]<T>
{{
    bool Equals([AllowNull] T other);
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546227, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546227")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestGenericDelegate(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = "public class C { public delegate void D<SomeType>(SomeType s); }";
            var symbolName = "C+D`1";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public delegate void [|D|]<SomeType>(SomeType s);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()
    Public Delegate Sub [|D|](Of SomeType)(s As SomeType)
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public delegate void [|D|]<SomeType>(SomeType s);
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class C
{{
    public delegate void [|D|]<SomeType>(SomeType s);
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(546200, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546200")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestAttribute(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
using System;

namespace N
{
    public class WorkingAttribute : Attribute
    {
        public WorkingAttribute(bool working) {}
    }
}

[N.Working(true)]
public class C {}";

            var symbolName = "C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using N;

[Working(true)]
public class [|C|]
{{
    public C();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports N

<Working(True)>
Public Class [|C|]
    Public Sub New()
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using N;

[Working(true)]
public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using N;

[Working(true)]
public class [|C|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestSymbolIdMatchesMetadata()
        {
            await TestSymbolIdMatchesMetadataAsync(LanguageNames.CSharp);
            await TestSymbolIdMatchesMetadataAsync(LanguageNames.VisualBasic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNotReusedOnAssemblyDiffers()
        {
            await TestNotReusedOnAssemblyDiffersAsync(LanguageNames.CSharp);
            await TestNotReusedOnAssemblyDiffersAsync(LanguageNames.VisualBasic);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestThrowsOnGenerateNamespace()
        {
            var namespaceSymbol = CodeGenerationSymbolFactory.CreateNamespaceSymbol("Outerspace");

            using var context = TestContext.Create();
            await Assert.ThrowsAsync<ArgumentException>(async () =>
{
    await context.GenerateSourceAsync(namespaceSymbol);
});
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReuseGenerateMemberOfGeneratedType()
        {
            var metadataSource = "public class C { public bool Is; }";

            using var context = TestContext.Create(LanguageNames.CSharp, SpecializedCollections.SingletonEnumerable(metadataSource));
            var a = await context.GenerateSourceAsync("C");
            var b = await context.GenerateSourceAsync("C.Is");
            TestContext.VerifyDocumentReused(a, b);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReuseRepeatGeneration()
        {
            using var context = TestContext.Create();
            var a = await context.GenerateSourceAsync();
            var b = await context.GenerateSourceAsync();
            TestContext.VerifyDocumentReused(a, b);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestWorkspaceContextHasReasonableProjectName()
        {
            using var context = TestContext.Create();
            var compilation = await context.DefaultProject.GetCompilationAsync();
            var result = await context.GenerateSourceAsync(compilation.ObjectType);
            var openedDocument = context.GetDocument(result);

            Assert.Equal("mscorlib", openedDocument.Project.AssemblyName);
            Assert.Equal("mscorlib", openedDocument.Project.Name);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReuseGenerateFromDifferentProject()
        {
            using var context = TestContext.Create();
            var projectId = ProjectId.CreateNewId();
            var project = context.CurrentSolution.AddProject(projectId, "ProjectB", "ProjectB", LanguageNames.CSharp).GetProject(projectId)
                .WithMetadataReferences(context.DefaultProject.MetadataReferences)
                .WithCompilationOptions(new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var a = await context.GenerateSourceAsync(project: context.DefaultProject);
            var b = await context.GenerateSourceAsync(project: project);
            TestContext.VerifyDocumentReused(a, b);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNotReusedGeneratingForDifferentLanguage()
        {
            using var context = TestContext.Create(LanguageNames.CSharp);
            var projectId = ProjectId.CreateNewId();
            var project = context.CurrentSolution.AddProject(projectId, "ProjectB", "ProjectB", LanguageNames.VisualBasic).GetProject(projectId)
                .WithMetadataReferences(context.DefaultProject.MetadataReferences)
                .WithCompilationOptions(new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var a = await context.GenerateSourceAsync(project: context.DefaultProject);
            var b = await context.GenerateSourceAsync(project: project);
            TestContext.VerifyDocumentNotReused(a, b);
        }

        [WorkItem(546311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546311")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task FormatMetadataAsSource()
        {
            using var context = TestContext.Create(LanguageNames.CSharp);
            var file = await context.GenerateSourceAsync("System.Console", project: context.DefaultProject);
            var document = context.GetDocument(file);
            await Formatter.FormatAsync(document, CSharpSyntaxFormattingOptions.Default, CancellationToken.None);
        }

        [WorkItem(530829, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530829")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task IndexedProperty()
        {
            var metadataSource = @"
Public Class C
    Public Property IndexProp(ByVal p1 As Integer) As String
        Get
            Return Nothing
        End Get
        Set(ByVal value As String)

        End Set
    End Property
End Class";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public string [|get_IndexProp|](int p1);
    public void set_IndexProp(int p1, string value);
}}";
            var symbolName = "C.get_IndexProp";
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, expected);
        }

        [WorkItem(566688, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/566688")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task AttributeReferencingInternalNestedType()
        {
            var metadataSource = @"using System;
[My(typeof(D))]
public class C
{
    public C() { }

    internal class D { }
}

public class MyAttribute : Attribute
{
    public MyAttribute(Type t) { }
}";

            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

[My(typeof(D))]
public class [|C|]
{{
    public C();
}}";
            var symbolName = "C";
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, expected);
        }

        [WorkItem(530978, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530978")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestAttributesOnMembers(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"using System;

[Obsolete]
public class C
{
    [Obsolete]
    [ThreadStatic]
    public int field1;

    [Obsolete]
    public int prop1 { get; set; }

    [Obsolete]
    public int prop2 { get { return 10; } set {} }

    [Obsolete]
    public void method1() {}

    [Obsolete]
    public C() {}

    [Obsolete]
    ~C() {}

    [Obsolete]
    public int this[int x] { get { return 10; } set {} }

    [Obsolete]
    public event Action event1;

    [Obsolete]
    public event Action event2 { add {} remove {}}

    public void method2([System.Runtime.CompilerServices.CallerMemberName] string name = """") {}

    [Obsolete]
    public static C operator + (C c1, C c2) { return new C(); }
}
";
            var symbolName = "C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[DefaultMember(""Item"")]
[Obsolete]
public class [|C|]
{{
    [Obsolete]
    [ThreadStatic]
    public int field1;

    [Obsolete]
    public C();

    [Obsolete]
    ~C();

    [Obsolete]
    public int this[int x] {{ get; set; }}

    [Obsolete]
    public int prop1 {{ get; set; }}
    [Obsolete]
    public int prop2 {{ get; set; }}

    [Obsolete]
    public event Action event1;
    [Obsolete]
    public event Action event2;

    [Obsolete]
    public void method1();
    public void method2([CallerMemberName] string name = """");

    [Obsolete]
    public static C operator +(C c1, C c2);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices

<DefaultMember(""Item"")> <Obsolete>
Public Class [|C|]
    <Obsolete> <ThreadStatic>
    Public field1 As Integer

    <Obsolete>
    Public Sub New()

    <Obsolete>
    Public Property prop1 As Integer
    <Obsolete>
    Public Property prop2 As Integer
    <Obsolete>
    Default Public Property Item(x As Integer) As Integer

    <Obsolete>
    Public Event event1 As Action
    <Obsolete>
    Public Event event2 As Action

    <Obsolete>
    Public Sub method1()
    Public Sub method2(<CallerMemberName> Optional name As String = """")
    <Obsolete>
    Protected Overrides Sub Finalize()

    <Obsolete>
    Public Shared Operator +(c1 As C, c2 As C) As C
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.CompilerServices;

[Obsolete]
public class [|C|]
{{
    [Obsolete]
    [ThreadStatic]
    public int field1;

    [Obsolete]
    public int prop1 {{ get; set; }}

    [Obsolete]
    public int prop2
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    [Obsolete]
    public int this[int x]
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    [Obsolete]
    public event Action event1;

    [Obsolete]
    public event Action event2
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}

    [Obsolete]
    public void method1()
    {{
    }}

    [Obsolete]
    public C()
    {{
    }}

    [Obsolete]
    ~C()
    {{
    }}

    public void method2([CallerMemberName] string name = """")
    {{
    }}

    [Obsolete]
    public static C operator +(C c1, C c2)
    {{
        return new C();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.CompilerServices;

[Obsolete]
public class [|C|]
{{
    [Obsolete]
    [ThreadStatic]
    public int field1;

    [Obsolete]
    public int prop1 {{ get; set; }}

    [Obsolete]
    public int prop2
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    [Obsolete]
    public int this[int x]
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    [Obsolete]
    public event Action event1;

    [Obsolete]
    public event Action event2
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}

    [Obsolete]
    public void method1()
    {{
    }}

    [Obsolete]
    public C()
    {{
    }}

    [Obsolete]
    ~C()
    {{
    }}

    public void method2([CallerMemberName] string name = """")
    {{
    }}

    [Obsolete]
    public static C operator +(C c1, C c2)
    {{
        return new C();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(530923, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530923")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEmptyLineBetweenMembers(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"using System;

public class C
{
    public int field1;
    public int prop1 { get; set; }
    public int field2;
    public int prop2 { get { return 10; } set {} }
    public void method1() {}
    public C() {}
    public void method2([System.Runtime.CompilerServices.CallerMemberName] string name = """") {}
    ~C() {}
    public int this[int x] { get { return 10; } set {} }
    public event Action event1;
    public static C operator + (C c1, C c2) { return new C(); }
    public event Action event2 { add {} remove {}}
    public static C operator - (C c1, C c2) { return new C(); }
}
";
            var symbolName = "C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[DefaultMember(""Item"")]
public class [|C|]
{{
    public int field1;
    public int field2;

    public C();

    ~C();

    public int this[int x] {{ get; set; }}

    public int prop1 {{ get; set; }}
    public int prop2 {{ get; set; }}

    public event Action event1;
    public event Action event2;

    public void method1();
    public void method2([CallerMemberName] string name = """");

    public static C operator +(C c1, C c2);
    public static C operator -(C c1, C c2);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices

<DefaultMember(""Item"")>
Public Class [|C|]
    Public field1 As Integer
    Public field2 As Integer

    Public Sub New()

    Public Property prop1 As Integer
    Public Property prop2 As Integer
    Default Public Property Item(x As Integer) As Integer

    Public Event event1 As Action
    Public Event event2 As Action

    Public Sub method1()
    Public Sub method2(<CallerMemberName> Optional name As String = """")
    Protected Overrides Sub Finalize()

    Public Shared Operator +(c1 As C, c2 As C) As C
    Public Shared Operator -(c1 As C, c2 As C) As C
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.CompilerServices;

public class [|C|]
{{
    public int field1;

    public int field2;

    public int prop1 {{ get; set; }}

    public int prop2
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    public int this[int x]
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    public event Action event1;

    public event Action event2
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}

    public void method1()
    {{
    }}

    public void method2([CallerMemberName] string name = """")
    {{
    }}

    ~C()
    {{
    }}

    public static C operator +(C c1, C c2)
    {{
        return new C();
    }}

    public static C operator -(C c1, C c2)
    {{
        return new C();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.CompilerServices;

public class [|C|]
{{
    public int field1;

    public int field2;

    public int prop1 {{ get; set; }}

    public int prop2
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    public int this[int x]
    {{
        get
        {{
            return 10;
        }}
        set
        {{
        }}
    }}

    public event Action event1;

    public event Action event2
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}

    public void method1()
    {{
    }}

    public void method2([CallerMemberName] string name = """")
    {{
    }}

    ~C()
    {{
    }}

    public static C operator +(C c1, C c2)
    {{
        return new C();
    }}

    public static C operator -(C c1, C c2)
    {{
        return new C();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(728644, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/728644")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEmptyLineBetweenMembers2(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var source = @"
using System;

/// <summary>T:IGoo</summary>
public interface IGoo
{
    /// <summary>P:IGoo.Prop1</summary>
    Uri Prop1 { get; set; }
    /// <summary>M:IGoo.Method1</summary>
    Uri Method1();
}
";
            var symbolName = "IGoo";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

//
// {FeaturesResources.Summary_colon}
//     T:IGoo
public interface [|IGoo|]
{{
    //
    // {FeaturesResources.Summary_colon}
    //     P:IGoo.Prop1
    Uri Prop1 {{ get; set; }}

    //
    // {FeaturesResources.Summary_colon}
    //     M:IGoo.Method1
    Uri Method1();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

'
' {FeaturesResources.Summary_colon}
'     T:IGoo
Public Interface [|IGoo|]
    '
    ' {FeaturesResources.Summary_colon}
    '     P:IGoo.Prop1
    Property Prop1 As Uri

    '
    ' {FeaturesResources.Summary_colon}
    '     M:IGoo.Method1
    Function Method1() As Uri
End Interface",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

public interface [|IGoo|]
{{
    Uri Prop1 {{ get; set; }}

    Uri Method1();
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "System.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

public interface [|IGoo|]
{{
    Uri Prop1 {{ get; set; }}

    Uri Method1();
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "System.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(source, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly, includeXmlDocComments: true);
        }

        [WorkItem(679114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/679114"), WorkItem(715013, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/715013")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestDefaultValueEnum(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var source = @"
using System.IO;

public class Test
{
    public void goo(FileOptions options = 0) {}
}
";
            var symbolName = "Test";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.IO;

public class [|Test|]
{{
    public Test();

    public void goo(FileOptions options = FileOptions.None);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.IO

Public Class [|Test|]
    Public Sub New()

    Public Sub goo(Optional options As FileOptions = FileOptions.None)
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.IO;

public class [|Test|]
{{
    public void goo(FileOptions options = FileOptions.None)
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.IO;

public class [|Test|]
{{
    public void goo(FileOptions options = FileOptions.None)
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(source, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(651261, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/651261")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullAttribute(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var source = @"
using System;

[Test(null)]
public class TestAttribute : Attribute
{
    public TestAttribute(int[] i)
    {
    }
}";
            var symbolName = "TestAttribute";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

[Test(null)]
public class [|TestAttribute|] : Attribute
{{
    public TestAttribute(int[] i);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

<Test(Nothing)>
Public Class [|TestAttribute|]
    Inherits Attribute

    Public Sub New(i() As Integer)
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

[Test(null)]
public class [|TestAttribute|] : Attribute
{{
    public TestAttribute(int[] i)
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

[Test(null)]
public class [|TestAttribute|] : Attribute
{{
    public TestAttribute(int[] i)
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(source, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(897006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/897006")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNavigationViaReducedExtensionMethodCS()
        {
            var metadata = @"using System;
public static class ObjectExtensions
{
    public static void M(this object o, int x) { }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        new object().[|M|](5);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public static class ObjectExtensions
{{
    public static void [|M|](this object o, int x);
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                sourceWithSymbolReference: sourceWithSymbolReference);
            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [WorkItem(897006, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/897006")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNavigationViaReducedExtensionMethodVB()
        {
            var metadata = @"Imports System.Runtime.CompilerServices
Namespace NS
    Public Module StringExtensions
        <Extension()>
        Public Sub M(ByVal o As String, x As Integer)
        End Sub
    End Module
End Namespace";
            var sourceWithSymbolReference = @"
Imports NS.StringExtensions
Public Module C
    Sub M()
        Dim s = ""Yay""
        s.[|M|](1)
    End Sub
End Module";
            var expected = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.Runtime.CompilerServices

Namespace NS
    <Extension>
    Public Module StringExtensions <Extension>
        Public Sub [|M|](o As String, x As Integer)
    End Module
End Namespace";

            using var context = TestContext.Create(
                LanguageNames.VisualBasic,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                sourceWithSymbolReference: sourceWithSymbolReference);
            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestIndexersAndOperators(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"public class Program
{
    public int this[int x]
    {
        get
        {
            return 0;
        }
        set
        {

        }
    }

    public static  Program operator + (Program p1, Program p2)
    {
        return new Program();
    }
}";
            var symbolName = "Program";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Reflection;

[DefaultMember(""Item"")]
public class [|Program|]
{{
    public Program();

    public int this[int x] {{ get; set; }}

    public static Program operator +(Program p1, Program p2);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.Reflection

<DefaultMember(""Item"")>
Public Class [|Program|]
    Public Sub New()

    Default Public Property Item(x As Integer) As Integer

    Public Shared Operator +(p1 As Program, p2 As Program) As Program
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|Program|]
{{
    public int this[int x]
    {{
        get
        {{
            return 0;
        }}
        set
        {{
        }}
    }}

    public static Program operator +(Program p1, Program p2)
    {{
        return new Program();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public class [|Program|]
{{
    public int this[int x]
    {{
        get
        {{
            return 0;
        }}
        set
        {{
        }}
    }}

    public static Program operator +(Program p1, Program p2)
    {{
        return new Program();
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(15387, "https://github.com/dotnet/roslyn/issues/15387")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestComImport1(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
using System.Runtime.InteropServices;

[ComImport]
[Guid(""666A175D-2448-447A-B786-CCC82CBEF156"")]
public interface IComImport
{
    void MOverload();
    void X();
    void MOverload(int i);
    int Prop { get; }
}";
            var symbolName = "IComImport";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Runtime.InteropServices;

[Guid(""666A175D-2448-447A-B786-CCC82CBEF156"")]
public interface [|IComImport|]
{{
    void MOverload();
    void X();
    void MOverload(int i);

    int Prop {{ get; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.Runtime.InteropServices

<Guid(""666A175D-2448-447A-B786-CCC82CBEF156"")>
Public Interface [|IComImport|]
    ReadOnly Property Prop As Integer

    Sub MOverload()
    Sub X()
    Sub MOverload(i As Integer)
End Interface",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[ComImport]
[Guid(""666A175D-2448-447A-B786-CCC82CBEF156"")]
public interface [|IComImport|]
{{
    int Prop {{ get; }}

    void MOverload();

    void X();

    void MOverload(int i);
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[ComImport]
[Guid(""666A175D-2448-447A-B786-CCC82CBEF156"")]
public interface [|IComImport|]
{{
    int Prop {{ get; }}

    void MOverload();

    void X();

    void MOverload(int i);
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestOptionalParameterWithDefaultLiteral(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
using System.Threading;

public class C {
    public void M(CancellationToken cancellationToken = default(CancellationToken)) { }
}";
            var symbolName = "C";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Threading;

public class [|C|]
{{
    public C();

    public void M(CancellationToken cancellationToken = default);
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.Threading

Public Class [|C|]
    Public Sub New()

    Public Sub M(Optional cancellationToken As CancellationToken = Nothing)
End Class",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Threading;

public class [|C|]
{{
    public void M(CancellationToken cancellationToken = default(CancellationToken))
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Threading;

public class [|C|]
{{
    public void M(CancellationToken cancellationToken = default(CancellationToken))
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            var languageVersion = language switch
            {
                OriginatingProjectLanguage.CSharp => "7.1",
                OriginatingProjectLanguage.VisualBasic => "15.5",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly, languageVersion: languageVersion);
        }

        [WorkItem(446567, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=446567")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestDocCommentsWithUnixNewLine(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var source = @"
using System;

/// <summary>T:IGoo" + "\n/// ABCDE\n" + @"/// FGHIJK</summary>
public interface IGoo
{
    /// <summary>P:IGoo.Prop1" + "\n/// ABCDE\n" + @"/// FGHIJK</summary>
    Uri Prop1 { get; set; }
    /// <summary>M:IGoo.Method1" + "\n/// ABCDE\n" + @"/// FGHIJK</summary>
    Uri Method1();
}
";
            var symbolName = "IGoo";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

//
// {FeaturesResources.Summary_colon}
//     T:IGoo ABCDE FGHIJK
public interface [|IGoo|]
{{
    //
    // {FeaturesResources.Summary_colon}
    //     P:IGoo.Prop1 ABCDE FGHIJK
    Uri Prop1 {{ get; set; }}

    //
    // {FeaturesResources.Summary_colon}
    //     M:IGoo.Method1 ABCDE FGHIJK
    Uri Method1();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

'
' {FeaturesResources.Summary_colon}
'     T:IGoo ABCDE FGHIJK
Public Interface [|IGoo|]
    '
    ' {FeaturesResources.Summary_colon}
    '     P:IGoo.Prop1 ABCDE FGHIJK
    Property Prop1 As Uri

    '
    ' {FeaturesResources.Summary_colon}
    '     M:IGoo.Method1 ABCDE FGHIJK
    Function Method1() As Uri
End Interface",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

public interface [|IGoo|]
{{
    Uri Prop1 {{ get; set; }}

    Uri Method1();
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "System.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;

public interface [|IGoo|]
{{
    Uri Prop1 {{ get; set; }}

    Uri Method1();
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "System.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(source, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly, includeXmlDocComments: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestUnmanagedCSharpConstraint_Type()
        {
            var metadata = @"using System;
public class TestType<T> where T : unmanaged
{
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new [|TestType|]&lt;int&gt;();
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|TestType|]<T> where T : unmanaged
{{
    public TestType();
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "7.3",
                sourceWithSymbolReference: sourceWithSymbolReference);
            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestUnmanagedCSharpConstraint_Method()
        {
            var metadata = @"using System;
public class TestType
{
    public void M<T>() where T : unmanaged
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M|]&lt;int&gt;();
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class TestType
{{
    public TestType();

    public void [|M|]<T>() where T : unmanaged;
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "7.3",
                sourceWithSymbolReference: sourceWithSymbolReference);
            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestUnmanagedCSharpConstraint_Delegate()
        {
            var metadata = @"using System;
public delegate void D<T>() where T : unmanaged;";
            var sourceWithSymbolReference = @"
class C
{
    void M([|D|]&lt;int&gt; lambda)
    {
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public delegate void [|D|]<T>() where T : unmanaged;";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "7.3",
                sourceWithSymbolReference: sourceWithSymbolReference);
            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestSByteMinValue()
        {
            var source = @"
class C
{
    sbyte Goo = sbyte.[|MinValue|];
}";

            var expected = "public const SByte MinValue = -128;";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.CSharp, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestSByteMinValueVB()
        {
            var source = @"
Class C
    Public Goo = SByte.[|MinValue|]
End Class";

            var expected = "Public Const MinValue As [SByte] = -128";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.VisualBasic, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInt16MinValue()
        {
            var source = @"
class C
{
    short Goo = short.[|MinValue|];
}";

            var expected = $"public const Int16 MinValue = -32768;";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.CSharp, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInt16MinValueVB()
        {
            var source = @"
Class C
    Public Goo = Short.[|MinValue|]
End Class";

            var expected = $"Public Const MinValue As Int16 = -32768";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.VisualBasic, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInt32MinValue()
        {
            var source = @"
class C
{
    int Goo = int.[|MinValue|];
}";

            var expected = $"public const Int32 MinValue = -2147483648;";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.CSharp, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInt32MinValueVB()
        {
            var source = @"
Class C
    Public Goo = Integer.[|MinValue|]
End Class";

            var expected = $"Public Const MinValue As Int32 = -2147483648";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.VisualBasic, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInt64MinValue()
        {
            var source = @"
class C
{
    long Goo = long.[|MinValue|];
}";

            var expected = $"public const Int64 MinValue = -9223372036854775808;";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.CSharp, expected);
        }

        [WorkItem(29786, "https://github.com/dotnet/roslyn/issues/29786")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInt64MinValueVB()
        {
            var source = @"
Class C
    Public Goo = Long.[|MinValue|]
End Class";

            var expected = $"Public Const MinValue As Int64 = -9223372036854775808";

            await GenerateAndVerifySourceLineAsync(source, LanguageNames.VisualBasic, expected);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReadOnlyStruct_ReadOnlyField(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public readonly struct S
{
    public readonly int i;
}
";
            var symbolName = "S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public readonly struct [|S|]
{{
    public readonly int i;
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region


<IsReadOnlyAttribute>
Public Structure [|S|]
    Public ReadOnly i As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public readonly struct [|S|]
{{
    public readonly int i;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public readonly struct [|S|]
{{
    public readonly int i;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStruct_ReadOnlyField(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public readonly int i;
}
";
            var symbolName = "S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct [|S|]
{{
    public readonly int i;
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure [|S|]
    Public ReadOnly i As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct [|S|]
{{
    public readonly int i;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct [|S|]
{{
    public readonly int i;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestRefStruct(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public ref struct S
{
}
";
            var symbolName = "S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public ref struct [|S|]
{{
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

<IsByRefLikeAttribute> <Obsolete(""Types with embedded references are not supported in this version of your compiler."", True)>
Public Structure [|S|]
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public ref struct [|S|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public ref struct [|S|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReadOnlyRefStruct(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public readonly ref struct S
{
}
";
            var symbolName = "S";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public readonly ref struct [|S|]
{{
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

<IsByRefLikeAttribute> <IsReadOnlyAttribute> <Obsolete(""Types with embedded references are not supported in this version of your compiler."", True)>
Public Structure [|S|]
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly ref struct [|S|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly ref struct [|S|]
{{
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReadOnlyMethod(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public readonly void M() {}
}
";
            var symbolName = "S.M";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct S
{{
    public readonly void [|M|]();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region


Public Structure S <IsReadOnlyAttribute>
    Public Sub [|M|]()
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly void [|M|]()
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly void [|M|]()
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReadOnlyMethod_InReadOnlyStruct(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public readonly struct S
{
    public void M() {}
}
";
            var symbolName = "S.M";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public readonly struct S
{{
    public void [|M|]();
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region


<IsReadOnlyAttribute>
Public Structure S
    Public Sub [|M|]()
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct S
{{
    public void [|M|]()
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct S
{{
    public void [|M|]()
    {{
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructProperty_ReadOnly(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public int P { get; }
}
";
            var symbolName = "S.P";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct S
{{
    public readonly int [|P|] {{ get; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure S
    Public ReadOnly Property [|P|] As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructProperty_ReadOnly_CSharp7_3(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public int P { get; }
}
";
            var symbolName = "S.P";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct S
{{
    public int [|P|] {{ get; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure S
    Public ReadOnly Property [|P|] As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            var metadataLanguageVersion = language switch
            {
                OriginatingProjectLanguage.CSharp => "7.3",
                OriginatingProjectLanguage.VisualBasic => "Preview",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly, metadataLanguageVersion: metadataLanguageVersion);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructProperty_ReadOnlyGet(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public readonly int P { get; }
}
";
            var symbolName = "S.P";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct S
{{
    public readonly int [|P|] {{ get; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure S
    Public ReadOnly Property [|P|] As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReadOnlyStructProperty_ReadOnlyGet(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public readonly struct S
{
    public readonly int P { get; }
}
";
            var symbolName = "S.P";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public readonly struct S
{{
    public int [|P|] {{ get; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region


<IsReadOnlyAttribute>
Public Structure S
    Public ReadOnly Property [|P|] As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public readonly struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

public readonly struct S
{{
    public int [|P|] {{ get; }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructProperty_ReadOnlyGet_Set(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public int P { readonly get => 123; set {} }
}
";
            var symbolName = "S.P";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct S
{{
    public int [|P|] {{ readonly get; set; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure S
    Public Property [|P|] As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public int [|P|]
    {{
        readonly get
        {{
            return 123;
        }}
        set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public int [|P|]
    {{
        readonly get
        {{
            return 123;
        }}
        set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructProperty_Get_ReadOnlySet(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public int P { get => 123; readonly set {} }
}
";
            var symbolName = "S.P";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct S
{{
    public int [|P|] {{ get; readonly set; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure S
    Public Property [|P|] As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public int [|P|]
    {{
        get
        {{
            return 123;
        }}
        readonly set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public int [|P|]
    {{
        get
        {{
            return 123;
        }}
        readonly set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructProperty_ReadOnlyGet_ReadOnlySet(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public readonly int P { get => 123; set {} }
}
";
            var symbolName = "S.P";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct S
{{
    public readonly int [|P|] {{ get; set; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure S
    Public Property [|P|] As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly int [|P|]
    {{
        get
        {{
            return 123;
        }}
        set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly int [|P|]
    {{
        get
        {{
            return 123;
        }}
        set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructIndexer_ReadOnlyGet(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public readonly int this[int i] => i;
}
";
            var symbolName = "S.Item";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Reflection;

[DefaultMember(""Item"")]
public struct S
{{
    public readonly int [|this|][int i] {{ get; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.Reflection

<DefaultMember(""Item"")>
Public Structure S
    Default Public ReadOnly Property [|Item|](i As Integer) As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly int [|this|][int i] => i;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly int [|this|][int i] => i;
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStructIndexer_ReadOnlyGet_Set(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public int this[int i] { readonly get => i; set {} }
}
";
            var symbolName = "S.Item";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Reflection;

[DefaultMember(""Item"")]
public struct S
{{
    public int [|this|][int i] {{ readonly get; set; }}
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.Reflection

<DefaultMember(""Item"")>
Public Structure S
    Default Public Property [|Item|](i As Integer) As Integer
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public int [|this|][int i]
    {{
        readonly get
        {{
            return i;
        }}
        set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public int [|this|][int i]
    {{
        readonly get
        {{
            return i;
        }}
        set
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestStruct_ReadOnlyEvent(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public struct S
{
    public readonly event System.Action E { add {} remove {} }
}
";
            var symbolName = "S.E";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

public struct S
{{
    public readonly event Action [|E|];
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

Public Structure S
    Public Event [|E|] As Action
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly event Action [|E|]
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct S
{{
    public readonly event Action [|E|]
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [WorkItem(34650, "https://github.com/dotnet/roslyn/issues/34650")]
        [Theory, CombinatorialData, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReadOnlyStruct_ReadOnlyEvent(OriginatingProjectLanguage language, bool signaturesOnly)
        {
            var metadataSource = @"
public readonly struct S
{
    public event System.Action E { add {} remove {} }
}
";
            var symbolName = "S.E";

            var expected = (language, signaturesOnly) switch
            {
                (OriginatingProjectLanguage.CSharp, true) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

public readonly struct S
{{
    public event Action [|E|];
}}",
                (OriginatingProjectLanguage.VisualBasic, true) => $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

<IsReadOnlyAttribute>
Public Structure S
    Public Event [|E|] As Action
End Structure",
                (OriginatingProjectLanguage.CSharp, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct S
{{
    public event Action [|E|]
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 6)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                (OriginatingProjectLanguage.VisualBasic, false) => $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {FeaturesResources.location_unknown}
// Decompiled with ICSharpCode.Decompiler {ICSharpCodeDecompilerVersion}
#endregion

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct S
{{
    public event Action [|E|]
    {{
        add
        {{
        }}
        remove
        {{
        }}
    }}
}}
#if false // {CSharpEditorResources.Decompilation_log}
{string.Format(CSharpEditorResources._0_items_in_cache, 9)}
------------------
{string.Format(CSharpEditorResources.Resolve_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Found_single_assembly_0, "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")}
{string.Format(CSharpEditorResources.Load_from_0, "mscorlib.v4_6_1038_0.dll")}
#endif",
                _ => throw ExceptionUtilities.Unreachable,
            };

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, ToLanguageName(language), expected, signaturesOnly: signaturesOnly);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNotNullCSharpConstraint_Type()
        {
            var metadata = @"using System;
public class TestType<T> where T : notnull
{
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new [|TestType|]&lt;int&gt;();
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|TestType|]<T> where T : notnull
{{
    public TestType();
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNotNullCSharpConstraint_Method()
        {
            var metadata = @"using System;
public class TestType
{
    public void M<T>() where T : notnull
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M|]&lt;int&gt;();
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class TestType
{{
    public TestType();

    public void [|M|]<T>() where T : notnull;
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNotNullCSharpConstraint_Delegate()
        {
            var metadata = @"using System;
public delegate void D<T>() where T : notnull;";
            var sourceWithSymbolReference = @"
class C
{
    void M([|D|]&lt;int&gt; lambda)
    {
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public delegate void [|D|]<T>() where T : notnull;";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable1()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1(string s)
    {
    }

#nullable disable

    public void M2(string s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

public class TestType
{{
    public TestType();

    public void [|M1|](string s);
#nullable disable
    public void M2(string s);

#nullable enable
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable2()
        {
            var metadata = @"
using System;

public class TestType
{
    public void M1(string s)
    {
    }

#nullable enable

    public void M2(string s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

public class TestType
{{
    public TestType();

#nullable disable
    public void [|M1|](string s);
#nullable enable
    public void M2(string s);
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable3()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1(string s)
    {
    }

#nullable disable

    public void M2(string s)
    {
    }

    public void M3(string s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

public class TestType
{{
    public TestType();

    public void [|M1|](string s);
#nullable disable
    public void M2(string s);
    public void M3(string s);

#nullable enable
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable4()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1(ICloneable s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

using System;

public class TestType
{{
    public TestType();

    public void [|M1|](ICloneable s);
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable5()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1(ICloneable s)
    {
#nullable disable
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

using System;

public class TestType
{{
    public TestType();

    public void [|M1|](ICloneable s);
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable6()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1<T>(T? s) where T : class
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|]("""");
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

public class TestType
{{
    public TestType();

    public void [|M1|]<T>(T? s) where T : class;
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable7()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1<T>(T s) where T : class
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|]("""");
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

public class TestType
{{
    public TestType();

    public void [|M1|]<T>(T s) where T : class;
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable8()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1<T>(T? s) where T : struct
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|]((int?)0);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class TestType
{{
    public TestType();

    public void [|M1|]<T>(T? s) where T : struct;
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable9()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1<T>(T s) where T : struct
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](0);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class TestType
{{
    public TestType();

    public void [|M1|]<T>(T s) where T : struct;
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable10()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1<T>(T s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|]("""");
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class TestType
{{
    public TestType();

    public void [|M1|]<T>(T s);
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable11()
        {
            var metadata = @"
using System;

public class TestType
{
    public void M1<T>(T s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|]("""");
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class TestType
{{
    public TestType();

    public void [|M1|]<T>(T s);
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable12()
        {
            var metadata = @"
#nullable enable

using System;

namespace N
{
    public class TestType
    {
        public void M1(string s)
        {
        }

    #nullable disable

        public void M2(string s)
        {
        }
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new N.TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

namespace N
{{
    public class TestType
    {{
        public TestType();

        public void [|M1|](string s);
#nullable disable
        public void M2(string s);

#nullable enable
    }}
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullableEnableDisable13()
        {
            var metadata = @"
#nullable enable

using System;

public class TestType
{
    public void M1(string s)
    {
    }

#nullable disable

    public class Nested
    {
        public void NestedM(string s)
        {
        }
    }

#nullable enable

    public void M2(string s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

#nullable enable

public class TestType
{{
    public TestType();

    public void [|M1|](string s);
    public void M2(string s);

    public class Nested
    {{
        public Nested();

#nullable disable
        public void NestedM(string s);

#nullable enable
    }}
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestDynamic1()
        {
            var metadata = @"
using System;

public class TestType
{
    public void M1(dynamic s)
    {
    }
}";
            var sourceWithSymbolReference = @"
class C
{
    void M()
    {
        var obj = new TestType().[|M1|](null);
    }
}";
            var expected = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class TestType
{{
    public TestType();

    public void [|M1|](dynamic s);
}}";

            using var context = TestContext.Create(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                languageVersion: "8",
                sourceWithSymbolReference: sourceWithSymbolReference,
                metadataLanguageVersion: "8");

            var navigationSymbol = await context.GetNavigationSymbolAsync();
            var metadataAsSourceFile = await context.GenerateSourceAsync(navigationSymbol);
            TestContext.VerifyResult(metadataAsSourceFile, expected);
        }

        [WorkItem(22431, "https://github.com/dotnet/roslyn/issues/22431")]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestCDATAComment()
        {
            var source = @"
public enum BinaryOperatorKind
{
    /// <summary>
    /// Represents the <![CDATA['<<']]> operator.
    /// </summary>
    LeftShift = 0x8,
}
";
            var symbolName = "BinaryOperatorKind.LeftShift";
            var expectedCS = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum BinaryOperatorKind
{{
    //
    // {FeaturesResources.Summary_colon}
    //     Represents the '<<' operator.
    [|LeftShift|] = 8
}}";
            await GenerateAndVerifySourceAsync(source, symbolName, LanguageNames.CSharp, expectedCS, includeXmlDocComments: true);
        }
    }
}
