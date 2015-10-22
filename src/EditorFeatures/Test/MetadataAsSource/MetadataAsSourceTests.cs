// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestClass()
        {
            var metadataSource = "public class C {}";
            var symbolName = "C";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|C|]
    Public Sub New()
End Class");
        }

        [WorkItem(546241)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestInterface()
        {
            var metadataSource = "public interface I {}";
            var symbolName = "I";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public interface [|I|]
{{
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Interface [|I|]
End Interface");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestConstructor()
        {
            var metadataSource = "public class C {}";
            var symbolName = "C..ctor";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public [|C|]();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub [|New|]()
End Class");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestMethod()
        {
            var metadataSource = "public class C { public void Foo() {} }";
            var symbolName = "C.Foo";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public void [|Foo|]();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Public Sub [|Foo|]()
End Class");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestField()
        {
            var metadataSource = "public class C { public string S; }";
            var symbolName = "C.S";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public string [|S|];

    public C();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public [|S|] As String

    Public Sub New()
End Class");
        }

        [WorkItem(546240)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestProperty()
        {
            var metadataSource = "public class C { public string S { get; protected set; } }";
            var symbolName = "C.S";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public string [|S|] {{ get; protected set; }}
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEvent()
        {
            var metadataSource = "using System; public class C { public event Action E; }";
            var symbolName = "C.E";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using System;

public class C
{{
    public C();

    public event Action [|E|];
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports System

Public Class C
    Public Sub New()

    Public Event [|E|] As Action
End Class");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestNestedType()
        {
            var metadataSource = "public class C { protected class D { } }";
            var symbolName = "C+D";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEnum()
        {
            var metadataSource = "public enum E { A, B, C }";
            var symbolName = "E";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum [|E|]
{{
    A = 0,
    B = 1,
    C = 2
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEnumFromField()
        {
            var metadataSource = "public enum E { A, B, C }";
            var symbolName = "E.C";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E
{{
    A = 0,
    B = 1,
    [|C|] = 2
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEnumWithUnderlyingType()
        {
            var metadataSource = "public enum E : short { A = 0, B = 1, C = 2 }";
            var symbolName = "E.C";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : short
{{
    A = 0,
    B = 1,
    [|C|] = 2
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEnumWithOverflowingUnderlyingType()
        {
            var metadataSource = "public enum E : ulong { A = 9223372036854775808 }";
            var symbolName = "E.A";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : ulong
{{
    [|A|] = 9223372036854775808
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Enum E As ULong
    [|A|] = 9223372036854775808UL
End Enum");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEnumWithDifferentValues()
        {
            var metadataSource = "public enum E : short { A = 1, B = 2, C = 3 }";
            var symbolName = "E.C";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public enum E : short
{{
    A = 1,
    B = 2,
    [|C|] = 3
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestTypeInNamespace()
        {
            var metadataSource = "namespace N { public class C {} }";
            var symbolName = "N.C";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestInlineConstant()
        {
            var metadataSource = @"public class C { public const string S = ""Hello mas""; }";
            var symbolName = "C.S";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public const string [|S|] = ""Hello mas"";

    public C();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Const [|S|] As String = ""Hello mas""

    Public Sub New()
End Class");
        }

        [WorkItem(546221)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestInlineTypeOf()
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

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

[MyType(typeof(string))]
public class [|C|]
{{
    public C();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

<MyType(GetType(String))>
Public Class [|C|]
    Public Sub New()
End Class");
        }

        [WorkItem(546231)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestNoDefaultConstructorInStructs()
        {
            var metadataSource = "public struct S {}";
            var symbolName = "S";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public struct [|S|]
{{
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Structure [|S|]
End Structure");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestReferenceDefinedType()
        {
            var metadataSource = "public class C { public static C Create() { return new C(); } }";
            var symbolName = "C";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|C|]
{{
    public C();

    public static C Create();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|C|]
    Public Sub New()

    Public Shared Function Create() As C
End Class");
        }

        [WorkItem(546227)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestGenericType()
        {
            var metadataSource = "public class G<SomeType> { public SomeType S; }";
            var symbolName = "G`1";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class [|G|]<SomeType>
{{
    public SomeType S;

    public G();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class [|G|](Of SomeType)
    Public S As SomeType

    Public Sub New()
End Class");
        }

        [WorkItem(546227)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestGenericDelegate()
        {
            var metadataSource = "public class C { public delegate void D<SomeType>(SomeType s); }";
            var symbolName = "C+D`1";

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

public class C
{{
    public C();

    public delegate void [|D|]<SomeType>(SomeType s);
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Public Class C
    Public Sub New()

    Public Delegate Sub [|D|](Of SomeType)(s As SomeType)
End Class");
        }

        [WorkItem(546200)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestAttribute()
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

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
#region {FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// {CodeAnalysisResources.InMemoryAssembly}
#endregion

using N;

[Working(true)]
public class [|C|]
{{
    public C();
}}");
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null""
' {CodeAnalysisResources.InMemoryAssembly}
#End Region

Imports N

<Working(True)>
Public Class [|C|]
    Public Sub New()
End Class");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestSymbolIdMatchesMetadata()
        {
            TestSymbolIdMatchesMetadata(LanguageNames.CSharp);
            TestSymbolIdMatchesMetadata(LanguageNames.VisualBasic);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestNotReusedOnAssemblyDiffers()
        {
            TestNotReusedOnAssemblyDiffers(LanguageNames.CSharp);
            TestNotReusedOnAssemblyDiffers(LanguageNames.VisualBasic);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestThrowsOnGenerateNamespace()
        {
            var namespaceSymbol = CodeGenerationSymbolFactory.CreateNamespaceSymbol("Outerspace");

            using (var context = new TestContext())
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestReuseGenerateMemberOfGeneratedType()
        {
            var metadataSource = "public class C { public bool Is; }";

            using (var context = new TestContext(LanguageNames.CSharp, SpecializedCollections.SingletonEnumerable(metadataSource)))
            {
                var a = context.GenerateSource("C");
                var b = context.GenerateSource("C.Is");
                context.VerifyDocumentReused(a, b);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestReuseRepeatGeneration()
        {
            using (var context = new TestContext())
            {
                var a = context.GenerateSource();
                var b = context.GenerateSource();
                context.VerifyDocumentReused(a, b);
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestReuseGenerateFromDifferentProject()
        {
            using (var context = new TestContext())
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestNotReusedGeneratingForDifferentLanguage()
        {
            using (var context = new TestContext(LanguageNames.CSharp))
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void FormatMetadataAsSource()
        {
            using (var context = new TestContext(LanguageNames.CSharp))
            {
                var file = context.GenerateSource("System.Console", project: context.DefaultProject);
                var document = context.GetDocument(file);
                Microsoft.CodeAnalysis.Formatting.Formatter.FormatAsync(document).Wait();
            }
        }

        [WorkItem(530829)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void IndexedProperty()
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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, expected);
        }

        [WorkItem(566688)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void AttributeReferencingInternalNestedType()
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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, expected);
        }

        [WorkItem(530978)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestAttributesOnMembers()
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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, expectedCS);

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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, expectedVB);
        }

        [WorkItem(530923)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEmptyLineBetweenMembers()
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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, expectedCS, compareTokens: false);

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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, expectedVB, compareTokens: false);
        }

        [WorkItem(728644)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestEmptyLineBetweenMembers2()
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
            GenerateAndVerifySource(source, symbolName, LanguageNames.CSharp, expectedCS, compareTokens: false, includeXmlDocComments: true);

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
            GenerateAndVerifySource(source, symbolName, LanguageNames.VisualBasic, expectedVB, compareTokens: false, includeXmlDocComments: true);
        }

        [WorkItem(679114), WorkItem(715013)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestDefaultValueEnum()
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
            GenerateAndVerifySource(source, symbolName, LanguageNames.CSharp, expectedCS);

            var expectedVB = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" 
' {CodeAnalysisResources.InMemoryAssembly}
#End Region
Imports System.IO 

Public Class [|Test|] 
    Public Sub New() 
    Public Sub foo(Optional options As FileOptions = FileOptions.None) 
End Class";
            GenerateAndVerifySource(source, symbolName, LanguageNames.VisualBasic, expectedVB);
        }

        [WorkItem(651261)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestNullAttribute()
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
            GenerateAndVerifySource(source, symbolName, LanguageNames.CSharp, expectedCS);

            var expectedVB = $@"#Region ""{FeaturesResources.Assembly} ReferencedAssembly, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"" 
' {CodeAnalysisResources.InMemoryAssembly}
#End Region
Imports System 

<Test(Nothing)>
Public Class [|TestAttribute|]
    Inherits Attribute 
    
    Public Sub New(i() As Integer) 
End Class";
            GenerateAndVerifySource(source, symbolName, LanguageNames.VisualBasic, expectedVB);
        }

        [WorkItem(897006)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestNavigationViaReducedExtensionMethodCS()
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

            using (var context = new TestContext(
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
        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestNavigationViaReducedExtensionMethodVB()
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

            using (var context = new TestContext(
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

        [WpfFact, Trait(Traits.Feature, Traits.Features.MetadataAsSource)]
        public void TestIndexersAndOperators()
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

            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.CSharp, $@"
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
            GenerateAndVerifySource(metadataSource, symbolName, LanguageNames.VisualBasic, $@"
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
