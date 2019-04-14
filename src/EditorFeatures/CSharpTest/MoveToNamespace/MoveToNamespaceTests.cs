// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveToNamespace
{
    [UseExportProvider]
    public class MoveToNamespaceTests : AbstractMoveToNamespaceTests
    {
        private static readonly IExportProviderFactory CSharpExportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                    .WithPart(typeof(TestMoveToNamespaceOptionsService)));

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions, exportProvider: CSharpExportProviderFactory.CreateExportProvider());

        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected override string GetLanguage() => LanguageNames.CSharp;

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretAboveNamespace()
            => TestMoveToNamespaceAsync(
@"using System;
[||]
namespace A
{
    class MyClass
    {
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretAboveNamespace2()
            => TestMoveToNamespaceAsync(
@"using System;[||]

namespace A
{
    class MyClass
    {
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_WeirdNamespace()
            => TestMoveToNamespaceAsync(
@"namespace A  [||].    B   .   C
{
    class MyClass
    {
    }
}",
expectedMarkup: @"namespace {|Warning:A|}
{
    class MyClass
    {
    }
}",
targetNamespace: "A");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceName()
            => TestMoveToNamespaceAsync(
@"namespace A[||] 
{
    class MyClass
    {
        void Method() { }
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    class MyClass
    {
        void Method() { }
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceName2()
            => TestMoveToNamespaceAsync(
@"namespace A[||].B.C
{
    class MyClass
    {
        void Method() { }
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    class MyClass
    {
        void Method() { }
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceKeyword()
        => TestMoveToNamespaceAsync(
@"namespace[||] A
{
    class MyClass
    {
        void Method() { }
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    class MyClass
    {
        void Method() { }
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceKeyword2()
        => TestMoveToNamespaceAsync(
@"[||]namespace A
{
    class MyClass
    {
        void Method() { }
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    class MyClass
    {
        void Method() { }
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceBrace()
        => TestMoveToNamespaceAsync(
@"namespace A
[||]{
    class MyClass
    {
        void Method() { }
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceBrace2()
        => TestMoveToNamespaceAsync(
@"namespace A
{[||]
    class MyClass
    {
        void Method() { }
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_MultipleDeclarations()
            => TestMoveToNamespaceAsync(
@"namespace A[||] 
{
    class MyClass
    {
        void Method() { }
    }

    class MyOtherClass
    {
        void Method() { }
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    class MyClass
    {
        void Method() { }
    }

    class MyOtherClass
    {
        void Method() { }
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_WithVariousSymbols()
        => TestMoveToNamespaceAsync(
@"namespace A[||] 
{
    public delegate void MyDelegate();

    public enum MyEnum
    {
        One,
        Two,
        Three
    }

    public struct MyStruct
    { }

    public interface MyInterface
    { }

    class MyClass
    {
        void Method() { }
    }

    class MyOtherClass
    {
        void Method() { }
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    public delegate void MyDelegate();

    public enum MyEnum
    {
        One,
        Two,
        Three
    }

    public struct MyStruct
    { }

    public interface MyInterface
    { }

    class MyClass
    {
        void Method() { }
    }

    class MyOtherClass
    {
        void Method() { }
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_NestedNamespace()
        => TestMoveToNamespaceAsync(
@"namespace A[||]
{
    namespace C 
    {
        class MyClass
        {
            void Method() { }
        }
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_NestedNamespace2()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    namespace C[||]
    {
        class MyClass
        {
            void Method() { }
        }
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Nested()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
        class NestedClass[||]
        {
        }
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Single()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass[||]
    {
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    class MyClass
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_SingleTop()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass[||]
    {
    }

    class MyClass2
    {
    }
}",
expectedMarkup: @"namespace {|Warning:B|}
{
    class MyClass
    {
    }
}

namespace A
{
    class MyClass2
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_TopWithReference()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass[||] : IMyClass
    {
    }

    interface IMyClass
    {
    }
}",
expectedMarkup: @"using A;

namespace {|Warning:B|}
{
    class MyClass : IMyClass
    {
    }
}

namespace A
{
    interface IMyClass
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Bottom()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
    }

    class MyClass2[||]
    {
    }
}",
expectedMarkup: @"namespace A
{
    class MyClass
    {
    }
}

namespace {|Warning:B|}
{
    class MyClass2
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_BottomReference()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass[||]
    {
    }
}",
expectedMarkup: @"using B;

namespace A
{
    class MyClass : IMyClass
    {
    }
}

namespace {|Warning:B|}
{
    interface IMyClass
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Middle()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
    }

    class MyClass2[||]
    {
    }

    class MyClass3
    {
    }
}",
expectedMarkup: @"namespace A
{
    class MyClass
    {
    }
}

namespace {|Warning:B|}
{
    class MyClass2
    {
    }
}

namespace A
{
    class MyClass3
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Middle_CaretBeforeClass()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
    }

    [||]class MyClass2
    {
    }

    class MyClass3
    {
    }
}",
expectedMarkup: @"namespace A
{
    class MyClass
    {
    }
}

namespace {|Warning:B|}
{
    class MyClass2
    {
    }
}

namespace A
{
    class MyClass3
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Middle_CaretAfterClass()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
    }

    class[||] MyClass2
    {
    }

    class MyClass3
    {
    }
}",
expectedMarkup: @"namespace A
{
    class MyClass
    {
    }
}

namespace {|Warning:B|}
{
    class MyClass2
    {
    }
}

namespace A
{
    class MyClass3
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Middle_CaretBeforeClassName()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
    }

    class [||]MyClass2
    {
    }

    class MyClass3
    {
    }
}",
expectedMarkup: @"namespace A
{
    class MyClass
    {
    }
}

namespace {|Warning:B|}
{
    class MyClass2
    {
    }
}

namespace A
{
    class MyClass3
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_CaretInMethod()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
        public string [||]MyMethod
        {
            return "";
        }
    }

}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass[||]
    {
    }

    class MyClass3 : IMyClass
    {
    }
}",
expectedMarkup: @"using B;

namespace A
{
    class MyClass : IMyClass
    {
    }
}

namespace {|Warning:B|}
{
    interface IMyClass
    {
    }
}

namespace A
{
    class MyClass3 : IMyClass
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference2()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }

    class [||]MyClass3 : IMyClass
    {
    }

    class MyClass4
    {
    }
}",
expectedMarkup: @"using A;

namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }
}

namespace {|Warning:B|}
{
    class MyClass3 : IMyClass
    {
    }
}

namespace A
{
    class MyClass4
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_NestedInNamespace()
        => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass
    {
    }

    namespace B
    {
        interface [||]IMyClass
        {
        }
    }

    class MyClass2 : B.IMyClass
    {
    }
}",
expectedSuccess: false);

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Cancelled()
            => TestCancelledOption(
@"namespace A
{
    class MyClass
    {
    }

    interface [||]IMyClass
    {
    }

    class MyClass2 : B.IMyClass
    {
    }
}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_Cancelled()
            => TestCancelledOption(
@"namespace A[||]
{
    class MyClass
    {
    }

    interface IMyClass
    {
    }

    class MyClass2 : B.IMyClass
    {
    }
}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference_ComplexName()
        => TestMoveToNamespaceAsync(
@"namespace A.B.C
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }

    class [||]MyClass3 : IMyClass
    {
    }

    class MyClass4
    {
    }
}",
expectedMarkup: @"using A.B.C;

namespace A.B.C
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }
}

namespace {|Warning:My.New.Namespace|}
{
    class MyClass3 : IMyClass
    {
    }
}

namespace A.B.C
{
    class MyClass4
    {
    }
}",
targetNamespace: "My.New.Namespace");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference_ComplexName2()
       => TestMoveToNamespaceAsync(
@"namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }

    class [||]MyClass3 : IMyClass
    {
    }

    class MyClass4
    {
    }
}",
expectedMarkup: @"using A;

namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }
}

namespace {|Warning:My.New.Namespace|}
{
    class MyClass3 : IMyClass
    {
    }
}

namespace A
{
    class MyClass4
    {
    }
}",
targetNamespace: "My.New.Namespace");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference_ComplexName3()
       => TestMoveToNamespaceAsync(
@"namespace A.B.C
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }

    class [||]MyClass3 : IMyClass
    {
    }

    class MyClass4
    {
    }
}",
expectedMarkup: @"using A.B.C;

namespace A.B.C
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }
}

namespace {|Warning:B|}
{
    class MyClass3 : IMyClass
    {
    }
}

namespace A.B.C
{
    class MyClass4
    {
    }
}",
targetNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveItems_ComplexNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace [||]A.Complex.Namespace
{
    class MyClass
    {
    }
}",
expectedNamespaceName: "A.Complex.Namespace");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveType_ComplexNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace A.Complex.Namespace
{
    class [||]MyClass
    {
    }
}",
expectedNamespaceName: "A.Complex.Namespace");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveItems_WeirdNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace A  [||].    B   .   C
{
    class MyClass
    {
    }
}",
expectedNamespaceName: "A  .    B   .   C");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveType_WeirdNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace A  .    B   .   C
{
    class MyClass[||]
    {
    }
}",
expectedNamespaceName: "A  .    B   .   C");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(34736, "https://github.com/dotnet/roslyn/issues/34736")]
        public Task MoveToNamespace_MoveType_Usings()
            => TestMoveToNamespaceAsync(
@"namespace One
{
    using Two;
    class C1
    {
        private C2 c2;
    }
}

namespace [||]Two
{
    class C2
    {

    }
}",
expectedMarkup: @"namespace One
{
    using Three;
    class C1
    {
        private C2 c2;
    }
}

namespace {|Warning:Three|}
{
    class C2
    {

    }
}",
targetNamespace: "Three");
    }
}
