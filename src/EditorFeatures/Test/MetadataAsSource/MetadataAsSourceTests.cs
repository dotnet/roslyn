// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.MetadataAsSource
{
    public partial class MetadataAsSourceTests : AbstractMetadataAsSourceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestClass()
        {
            var metadataSource = "public class C {}";
            var symbolName = "C";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|C|]
    Public Sub New()
End Class");
        }

        [WorkItem(546241)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInterface()
        {
            var metadataSource = "public interface I {}";
            var symbolName = "I";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public interface [|I|]
{{
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Interface [|I|]
End Interface");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestConstructor()
        {
            var metadataSource = "public class C {}";
            var symbolName = "C..ctor";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public [|C|]();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub [|New|]()
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestMethod()
        {
            var metadataSource = "public class C { public void Foo() {} }";
            var symbolName = "C.Foo";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public void [|Foo|]();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Public Sub [|Foo|]()
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestField()
        {
            var metadataSource = "public class C { public string S; }";
            var symbolName = "C.S";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public string [|S|];

    public C();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public [|S|] As String

    Public Sub New()
End Class");
        }

        [WorkItem(546240)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestProperty()
        {
            var metadataSource = "public class C { public string S { get; protected set; } }";
            var symbolName = "C.S";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public string [|S|] {{ get; protected set; }}
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Public Property [|S|] As String
End Class");
        }

        [WorkItem(546194)]
        [WorkItem(546291)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEvent()
        {
            var metadataSource = "using System; public class C { public event Action E; }";
            var symbolName = "C.E";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

public class C
{{
    public C();

    public event Action [|E|];
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

Public Class C
    Public Sub New()

    Public Event [|E|] As Action
End Class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNestedType()
        {
            var metadataSource = "public class C { protected class D { } }";
            var symbolName = "C+D";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    protected class [|D|]
    {{
        public D();
    }}
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Protected Class [|D|]
        Public Sub New()
    End Class
End Class");
        }

        [WorkItem(546195), WorkItem(546269)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnum()
        {
            var metadataSource = "public enum E { A, B, C }";
            var symbolName = "E";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum [|E|]
{{
    A = 0,
    B = 1,
    C = 2
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum [|E|]
    A = 0
    B = 1
    C = 2
End Enum");
        }

        [WorkItem(546195), WorkItem(546269)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumFromField()
        {
            var metadataSource = "public enum E { A, B, C }";
            var symbolName = "E.C";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E
{{
    A = 0,
    B = 1,
    [|C|] = 2
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E
    A = 0
    B = 1
    [|C|] = 2
End Enum");
        }

        [WorkItem(546273)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumWithUnderlyingType()
        {
            var metadataSource = "public enum E : short { A = 0, B = 1, C = 2 }";
            var symbolName = "E.C";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : short
{{
    A = 0,
    B = 1,
    [|C|] = 2
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E As Short
    A = 0
    B = 1
    [|C|] = 2 
End Enum");
        }

        [WorkItem(650741)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumWithOverflowingUnderlyingType()
        {
            var metadataSource = "public enum E : ulong { A = 9223372036854775808 }";
            var symbolName = "E.A";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : ulong
{{
    [|A|] = 9223372036854775808
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E As ULong
    [|A|] = 9223372036854775808UL
End Enum");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEnumWithDifferentValues()
        {
            var metadataSource = "public enum E : short { A = 1, B = 2, C = 3 }";
            var symbolName = "E.C";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : short
{{
    A = 1,
    B = 2,
    [|C|] = 3
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E As Short
    A = 1
    B = 2
    [|C|] = 3
End Enum");
        }

        [WorkItem(546198)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestTypeInNamespace()
        {
            var metadataSource = "namespace N { public class C {} }";
            var symbolName = "N.C";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

namespace N
{{
    public class [|C|]
    {{
        public C();
    }}
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Namespace N
    Public Class [|C|]
        Public Sub New()
    End Class
End Namespace");
        }

        [WorkItem(546223)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInlineConstant()
        {
            var metadataSource = @"public class C { public const string S = ""Hello mas""; }";
            var symbolName = "C.S";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public const string [|S|] = ""Hello mas"";

    public C();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Const [|S|] As String = ""Hello mas""

    Public Sub New()
End Class");
        }

        [WorkItem(546221)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestInlineTypeOf()
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

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

[MyType(typeof(string))]
public class [|C|]
{{
    public C();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

<MyType(GetType(String))>
Public Class [|C|]
    Public Sub New()
End Class");
        }

        [WorkItem(546231)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNoDefaultConstructorInStructs()
        {
            var metadataSource = "public struct S {}";
            var symbolName = "S";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct [|S|]
{{
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure [|S|]
End Structure");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReferenceDefinedType()
        {
            var metadataSource = "public class C { public static C Create() { return new C(); } }";
            var symbolName = "C";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public static C Create();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|C|]
    Public Sub New()

    Public Shared Function Create() As C
End Class");
        }

        [WorkItem(546227)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestGenericType()
        {
            var metadataSource = "public class G<SomeType> { public SomeType S; }";
            var symbolName = "G`1";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|G|]<SomeType>
{{
    public SomeType S;

    public G();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|G|](Of SomeType)
    Public S As SomeType

    Public Sub New()
End Class");
        }

        [WorkItem(546227)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestGenericDelegate()
        {
            var metadataSource = "public class C { public delegate void D<SomeType>(SomeType s); }";
            var symbolName = "C+D`1";

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public delegate void [|D|]<SomeType>(SomeType s);
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Public Delegate Sub [|D|](Of SomeType)(s As SomeType)
End Class");
        }

        [WorkItem(546200)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestAttribute()
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

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using N;

[Working(true)]
public class [|C|]
{{
    public C();
}}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports N

<Working(True)>
Public Class [|C|]
    Public Sub New()
End Class");
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

            using (var context = await TestContext.CreateAsync())
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    try
                    {
                        context.GenerateSource(namespaceSymbol);
                    }
                    catch (AggregateException ae)
                    {
                        throw ae.InnerException;
                    }
                });
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReuseGenerateMemberOfGeneratedType()
        {
            var metadataSource = "public class C { public bool Is; }";

            using (var context = await TestContext.CreateAsync(LanguageNames.CSharp, SpecializedCollections.SingletonEnumerable(metadataSource)))
            {
                var a = context.GenerateSource("C");
                var b = context.GenerateSource("C.Is");
                context.VerifyDocumentReused(a, b);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReuseRepeatGeneration()
        {
            using (var context = await TestContext.CreateAsync())
            {
                var a = context.GenerateSource();
                var b = context.GenerateSource();
                context.VerifyDocumentReused(a, b);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestReuseGenerateFromDifferentProject()
        {
            using (var context = await TestContext.CreateAsync())
            {
                var projectId = ProjectId.CreateNewId();
                var project = context.CurrentSolution.AddProject(projectId, "ProjectB", "ProjectB", LanguageNames.CSharp).GetProject(projectId)
                    .WithMetadataReferences(context.DefaultProject.MetadataReferences)
                    .WithCompilationOptions(new CS.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                var a = context.GenerateSource(project: context.DefaultProject);
                var b = context.GenerateSource(project: project);
                context.VerifyDocumentReused(a, b);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNotReusedGeneratingForDifferentLanguage()
        {
            using (var context = await TestContext.CreateAsync(LanguageNames.CSharp))
            {
                var projectId = ProjectId.CreateNewId();
                var project = context.CurrentSolution.AddProject(projectId, "ProjectB", "ProjectB", LanguageNames.VisualBasic).GetProject(projectId)
                    .WithMetadataReferences(context.DefaultProject.MetadataReferences)
                    .WithCompilationOptions(new VB.VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                var a = context.GenerateSource(project: context.DefaultProject);
                var b = context.GenerateSource(project: project);
                context.VerifyDocumentNotReused(a, b);
            }
        }

        [WorkItem(546311)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task FormatMetadataAsSource()
        {
            using (var context = await TestContext.CreateAsync(LanguageNames.CSharp))
            {
                var file = context.GenerateSource("System.Console", project: context.DefaultProject);
                var document = context.GetDocument(file);
                Microsoft.CodeAnalysis.Formatting.Formatter.FormatAsync(document).Wait();
            }
        }

        [WorkItem(530829)]
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
            var expected = $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
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

        [WorkItem(566688)]
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

            var expected = $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
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

        [WorkItem(530978)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestAttributesOnMembers()
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
            var expectedCS = $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
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
}}
";
            var symbolName = "C";
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, expectedCS);

            var expectedVB = $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region
Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices

<DefaultMember(""Item"")>
<Obsolete>
Public Class [|C|]
    <Obsolete>
    <ThreadStatic>
    Public field1 As Integer
    <Obsolete>
    Public Sub New()
    <Obsolete>
    Default Public Property Item(x As Integer) As Integer
    <Obsolete>
    Public Property prop1 As Integer
    <Obsolete>
    Public Property prop2 As Integer
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
End Class
";
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, expectedVB);
        }

        [WorkItem(530923)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEmptyLineBetweenMembers()
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
            var expectedCS = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
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
}}
";
            var symbolName = "C";
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, expectedCS, compareTokens: false);

            var expectedVB = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
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

    Default Public Property Item(x As Integer) As Integer
    Public Property prop1 As Integer
    Public Property prop2 As Integer

    Public Event event1 As Action
    Public Event event2 As Action

    Public Sub method1()
    Public Sub method2(<CallerMemberName> Optional name As String = """")
    Protected Overrides Sub Finalize()

    Public Shared Operator +(c1 As C, c2 As C) As C
    Public Shared Operator -(c1 As C, c2 As C) As C
End Class";
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, expectedVB, compareTokens: false);
        }

        [WorkItem(728644)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestEmptyLineBetweenMembers2()
        {
            var source = @"
using System;

/// <summary>T:IFoo</summary>
public interface IFoo
{
    /// <summary>P:IFoo.Prop1</summary>
    Uri Prop1 { get; set; }
    /// <summary>M:IFoo.Method1</summary>
    Uri Method1();
}
";
            var symbolName = "IFoo";
            var expectedCS = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

//
// {FeaturesResources.Summary}
//     T:IFoo
public interface [|IFoo|]
{{
    //
    // {FeaturesResources.Summary}
    //     P:IFoo.Prop1
    Uri Prop1 {{ get; set; }}

    //
    // {FeaturesResources.Summary}
    //     M:IFoo.Method1
    Uri Method1();
}}
";
            await GenerateAndVerifySourceAsync(source, symbolName, LanguageNames.CSharp, expectedCS, compareTokens: false, includeXmlDocComments: true);

            var expectedVB = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

'
' {FeaturesResources.Summary}
'     T:IFoo
Public Interface [|IFoo|]
    '
    ' {FeaturesResources.Summary}
    '     P:IFoo.Prop1
    Property Prop1 As Uri

    '
    ' {FeaturesResources.Summary}
    '     M:IFoo.Method1
    Function Method1() As Uri
End Interface
";
            await GenerateAndVerifySourceAsync(source, symbolName, LanguageNames.VisualBasic, expectedVB, compareTokens: false, includeXmlDocComments: true);
        }

        [WorkItem(679114), WorkItem(715013)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestDefaultValueEnum()
        {
            var source = @"
using System.IO;

public class Test
{
    public void foo(FileOptions options = 0) {}
}
";
            var symbolName = "Test";
            var expectedCS = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.IO;

public class [|Test|] 
{{
    public Test(); 
    public void foo(FileOptions options = FileOptions.None);
}}
";
            await GenerateAndVerifySourceAsync(source, symbolName, LanguageNames.CSharp, expectedCS);

            var expectedVB = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" 
' {CodeAnalysisResources.InMemoryAssembly}
#End Region
Imports System.IO 

Public Class [|Test|] 
    Public Sub New() 
    Public Sub foo(Optional options As FileOptions = FileOptions.None) 
End Class";
            await GenerateAndVerifySourceAsync(source, symbolName, LanguageNames.VisualBasic, expectedVB);
        }

        [WorkItem(651261)]
        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestNullAttribute()
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
            var expectedCS = $@"#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;
 
[Test(null)]
public class [|TestAttribute|] : Attribute
{{
    public TestAttribute(int[] i);
}}
";
            await GenerateAndVerifySourceAsync(source, symbolName, LanguageNames.CSharp, expectedCS);

            var expectedVB = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" 
' {CodeAnalysisResources.InMemoryAssembly}
#End Region
Imports System 

<Test(Nothing)>
Public Class [|TestAttribute|]
    Inherits Attribute 
    
    Public Sub New(i() As Integer) 
End Class";
            await GenerateAndVerifySourceAsync(source, symbolName, LanguageNames.VisualBasic, expectedVB);
        }

        [WorkItem(897006)]
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
}}
";

            using (var context = await TestContext.CreateAsync(
                LanguageNames.CSharp,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                sourceWithSymbolReference: sourceWithSymbolReference))
            {
                var navigationSymbol = context.GetNavigationSymbol();
                var metadataAsSourceFile = context.GenerateSource(navigationSymbol);
                context.VerifyResult(metadataAsSourceFile, expected);
            }
        }

        [WorkItem(897006)]
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
    Public Module StringExtensions
        <Extension> Public Sub [|M|](o As String, x As Integer)
    End Module
End Namespace";

            using (var context = await TestContext.CreateAsync(
                LanguageNames.VisualBasic,
                SpecializedCollections.SingletonEnumerable(metadata),
                includeXmlDocComments: false,
                sourceWithSymbolReference: sourceWithSymbolReference))
            {
                var navigationSymbol = context.GetNavigationSymbol();
                var metadataAsSourceFile = context.GenerateSource(navigationSymbol);
                context.VerifyResult(metadataAsSourceFile, expected);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public async Task TestIndexersAndOperators()
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

            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System.Reflection;

[DefaultMember(""Item"")]
public class [|Program|]
        {{
            public Program();

            public int this[int x] {{ get; set; }}

            public static Program operator +(Program p1, Program p2);
        }}");
            await GenerateAndVerifySourceAsync(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System.Reflection

<DefaultMember(""Item"")>
Public Class [|Program|]
    Public Sub New()

    Default Public Property Item(x As Integer) As Integer

    Public Shared Operator +(p1 As Program, p2 As Program) As Program
End Class");
        }
    }
}