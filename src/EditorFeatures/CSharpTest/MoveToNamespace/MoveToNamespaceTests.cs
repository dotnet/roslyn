// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveToNamespace
{
    public class MoveToNamespaceTests : AbstractCSharpCodeActionTest
    {
        private static readonly IExportProviderFactory CSharpExportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic
                    .WithPart(typeof(TestMoveToNamespaceOptionsService)));

        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new MoveToNamespaceCodeActionProvider();

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions, exportProvider: CSharpExportProviderFactory.CreateExportProvider());

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceName()
            => TestInRegularAndScriptAsync(
@"namespace A[||] 
{
    class MyClass
    {
        void Method() { }
    }
}",
@$"namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass
    {{
        void Method() {{ }}
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceKeyword()
        => TestInRegularAndScriptAsync(
@"namespace[||] A
{
    class MyClass
    {
        void Method() { }
    }
}",
@$"namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass
    {{
        void Method() {{ }}
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_MultipleDeclarations()
            => TestInRegularAndScriptAsync(
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
@$"namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass
    {{
        void Method() {{ }}
    }}

    class MyOtherClass
    {{
        void Method() {{ }}
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_WithVariousSymbols()
        => TestInRegularAndScriptAsync(
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
@$"namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    public delegate void MyDelegate();

    public enum MyEnum
    {{
        One,
        Two,
        Three
    }}

    public struct MyStruct
    {{ }}

    public interface MyInterface
    {{ }}

    class MyClass
    {{
        void Method() {{ }}
    }}

    class MyOtherClass
    {{
        void Method() {{ }}
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_NestedNamespace()
        => TestMissingInRegularAndScriptAsync(
@"namespace A[||]
{
    namespace C 
    {
        class MyClass
        {
            void Method() { }
        }
    }
}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_NestedNamespace2()
        => TestMissingInRegularAndScriptAsync(
@"namespace A
{
    namespace C[||]
    {
        class MyClass
        {
            void Method() { }
        }
    }
}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Single()
        => TestInRegularAndScriptAsync(
@"namespace A
{
    class MyClass[||]
    {
    }
}",
@$"namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_SingleTop()
        => TestInRegularAndScriptAsync(
@"namespace A
{
    class MyClass[||]
    {
    }

    class MyClass2
    {
    }
}",
@$"namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass
    {{
    }}
}}

namespace A
{{
    class MyClass2
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_TopWithReference()
        => TestInRegularAndScriptAsync(
@"namespace A
{
    class MyClass[||] : IMyClass
    {
    }

    interface IMyClass
    {
    }
}",
@$"using A;

namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass : IMyClass
    {{
    }}
}}

namespace A
{{
    interface IMyClass
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Bottom()
        => TestInRegularAndScriptAsync(
@"namespace A
{
    class MyClass
    {
    }

    class MyClass2[||]
    {
    }
}",
@$"namespace A
{{
    class MyClass
    {{
    }}
}}

namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass2
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_BottomReference()
        => TestInRegularAndScriptAsync(
@"namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass[||]
    {
    }
}",
@$"namespace A
{{
    class MyClass : IMyClass
    {{
    }}
}}

namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    interface IMyClass
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Middle()
        => TestInRegularAndScriptAsync(
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
@$"namespace A
{{
    class MyClass
    {{
    }}
}}

namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass2
    {{
    }}
}}

namespace A
{{
    class MyClass3
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference()
        => TestInRegularAndScriptAsync(
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
@$"namespace A
{{
    class MyClass : IMyClass
    {{
    }}
}}

namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    interface IMyClass
    {{
    }}
}}

namespace A
{{
    class MyClass3 : IMyClass
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference2()
        => TestInRegularAndScriptAsync(
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
@$"using A;

namespace A
{{
    class MyClass : IMyClass
    {{
    }}

    interface IMyClass
    {{
    }}
}}

namespace {TestMoveToNamespaceOptionsService.NamespaceValue}
{{
    class MyClass3 : IMyClass
    {{
    }}
}}

namespace A
{{
    class MyClass4
    {{
    }}
}}");

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_NestedInNamespace()
        => TestMissingInRegularAndScriptAsync(
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
}");
    }
}
