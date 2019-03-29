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
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceName()
            => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceKeyword()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_MultipleDeclarations()
            => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_WithVariousSymbols()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_NestedNamespace()
        => TestMoveToNamespaceViaCommandAsync(
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
        => TestMoveToNamespaceViaCommandAsync(
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
        public Task MoveToNamespace_MoveType_Single()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_SingleTop()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_TopWithReference()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Bottom()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_BottomReference()
        => TestMoveToNamespaceViaCommandAsync(
@"namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass[||]
    {
    }
}",
expectedMarkup: @"namespace A
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Middle()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedMarkup: @"namespace A
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference2()
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "B");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_NestedInNamespace()
        => TestMoveToNamespaceViaCommandAsync(
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
        => TestMoveToNamespaceViaCommandAsync(
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
expectedNamespace: "My.New.Namespace");
    }
}
