﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.MoveToNamespace;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        private static readonly IExportProviderFactory ExportProviderFactory =
            ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(typeof(TestMoveToNamespaceOptionsService)));

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => CreateWorkspaceFromFile(initialMarkup, parameters, ExportProviderFactory);

        protected TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters, IExportProviderFactory exportProviderFactory)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions, exportProvider: exportProviderFactory.CreateExportProvider());

        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected override string GetLanguage() => LanguageNames.CSharp;

        public static IEnumerable<object[]> SupportedKeywords => new[]
        {
            new []{"class" },
            new []{"enum" },
            new []{"interface"}
        };

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "A",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.B.C.MyClass", "A.MyClass" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyClass", "B.MyClass" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.B.C.MyClass", "B.MyClass" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
    {
        {"A.MyClass", "B.MyClass"}
    }
);

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
    {
        {"A.MyClass", "B.MyClass"}
    });

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyClass", "B.MyClass" },
    {"A.MyOtherClass", "B.MyOtherClass" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyDelegate", "B.MyDelegate" },
    {"A.MyEnum", "B.MyEnum" },
    {"A.MyStruct", "B.MyStruct" },
    {"A.MyInterface", "B.MyInterface" },
    {"A.MyClass", "B.MyClass" },
    {"A.MyOtherClass", "B.MyOtherClass" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_Nested(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    class MyClass
    {{
        {typeKeyword} NestedType[||]
        {{
        }}
    }}
}}",
expectedSuccess: false);

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_Single(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    {typeKeyword} MyType[||]
    {{
    }}
}}",
expectedMarkup: @$"namespace {{|Warning:B|}}
{{
    {typeKeyword} MyType
    {{
    }}
}}",
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyType", "B.MyType" }
});

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_SingleTop(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    {typeKeyword} MyType[||]
    {{
    }}

    {typeKeyword} MyType2
    {{
    }}
}}",
expectedMarkup: @$"namespace {{|Warning:B|}}
{{
    {typeKeyword} MyType
    {{
    }}
}}

namespace A
{{
    {typeKeyword} MyType2
    {{
    }}
}}",
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyType", "B.MyType" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyClass", "B.MyClass" }
});

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_Bottom(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}

    {typeKeyword} MyType2[||]
    {{
    }}
}}",
expectedMarkup: @$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}
}}

namespace {{|Warning:B|}}
{{
    {typeKeyword} MyType2
    {{
    }}
}}",
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyType2", "B.MyType2" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.IMyClass", "B.IMyClass" }
});

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_Middle(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}

    {typeKeyword} MyType2[||]
    {{
    }}

    {typeKeyword} MyType3
    {{
    }}
}}",
expectedMarkup: @$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}
}}

namespace {{|Warning:B|}}
{{
    {typeKeyword} MyType2
    {{
    }}
}}

namespace A
{{
    {typeKeyword} MyType3
    {{
    }}
}}",
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyType2", "B.MyType2" }
});

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_Middle_CaretBeforeKeyword(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}

    [||]{typeKeyword} MyType2
    {{
    }}

    {typeKeyword} MyType3
    {{
    }}
}}",
expectedMarkup: @$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}
}}

namespace {{|Warning:B|}}
{{
    {typeKeyword} MyType2
    {{
    }}
}}

namespace A
{{
    {typeKeyword} MyType3
    {{
    }}
}}",
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyType2", "B.MyType2" }
});

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_Middle_CaretAfterTypeKeyword(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}

    {typeKeyword}[||] MyType2
    {{
    }}

    {typeKeyword} MyType3
    {{
    }}
}}",
expectedMarkup: @$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}
}}

namespace {{|Warning:B|}}
{{
    {typeKeyword} MyType2
    {{
    }}
}}

namespace A
{{
    {typeKeyword} MyType3
    {{
    }}
}}",
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyType2", "B.MyType2" }
});

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveType_Middle_CaretBeforeTypeName(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}

    {typeKeyword} [||]MyType2
    {{
    }}

    {typeKeyword} MyType3
    {{
    }}
}}",
expectedMarkup: @$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}
}}

namespace {{|Warning:B|}}
{{
    {typeKeyword} MyType2
    {{
    }}
}}

namespace A
{{
    {typeKeyword} MyType3
    {{
    }}
}}",
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyType2", "B.MyType2" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.IMyClass", "B.IMyClass" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyClass3", "B.MyClass3" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "My.New.Namespace",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.B.C.MyClass3", "My.New.Namespace.MyClass3" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "My.New.Namespace",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.MyClass3", "My.New.Namespace.MyClass3" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "B",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"A.B.C.MyClass3", "B.MyClass3" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveItems_ComplexNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace [||]A.Complex.Namespace
{
    class MyClass
    {
    }
}",
expectedNamespaceName: "A.Complex.Namespace");

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveType_ComplexNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace A.Complex.Namespace
{
    class [||]MyClass
    {
    }
}",
expectedNamespaceName: "A.Complex.Namespace");

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveItems_WeirdNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace A  [||].    B   .   C
{
    class MyClass
    {
    }
}",
expectedNamespaceName: "A  .    B   .   C");

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_Analysis_MoveType_WeirdNamespace()
           => TestMoveToNamespaceAnalysisAsync(
@"namespace A  .    B   .   C
{
    class MyClass[||]
    {
    }
}",
expectedNamespaceName: "A  .    B   .   C");

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
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
targetNamespace: "Three",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"Two.C2", "Three.C2" }
});

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(35577, "https://github.com/dotnet/roslyn/issues/35577")]
        public async Task MoveToNamespace_WithoutOptionsService()
        {
            var code = @"namespace A[||]
{
class MyClass
{
    void Method() { }
}
}";

            var exportProviderWithoutOptionsService = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithoutPartsOfType(typeof(IMoveToNamespaceOptionsService)));

            using var workspace = CreateWorkspaceFromFile(code, new TestParameters(), exportProviderWithoutOptionsService);
            using var testState = new TestState(workspace);
            Assert.Null(testState.TestMoveToNamespaceOptionsService);

            var actions = await testState.MoveToNamespaceService.GetCodeActionsAsync(
                testState.InvocationDocument,
                testState.TestInvocationDocument.SelectedSpans.Single(),
                CancellationToken.None);

            Assert.Empty(actions);
        }

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(980758, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveOnlyTypeInGlobalNamespace(string typeKeyword)
        => TestMoveToNamespaceAsync(
@$"{typeKeyword} MyType[||]
{{
}}",
expectedMarkup: @$"namespace {{|Warning:A|}}
{{
    {typeKeyword} MyType
    {{
    }}
}}",
targetNamespace: "A",
expectedSymbolChanges: new Dictionary<string, string>()
{
    {"MyType", "A.MyType" }
});

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(980758, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
        [MemberData(nameof(SupportedKeywords))]
        public async Task MoveToNamespace_MoveOnlyTypeToGlobalNamespace(string typeKeyword)
        {
            // We will not get "" as target namespace in VS, but the refactoring should be able
            // to handle it w/o crashing.
            await TestMoveToNamespaceAsync(
 @$"namespace A
{{
    {typeKeyword} MyType[||]
    {{
    }}
}}",
  expectedMarkup: @$"namespace A
{{
    {typeKeyword} MyType
    {{
    }}
}}",
      targetNamespace: "");
        }

        [Theory, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(980758, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
        [MemberData(nameof(SupportedKeywords))]
        public Task MoveToNamespace_MoveOneTypeInGlobalNamespace(string typeKeyword)
            => TestMoveToNamespaceAsync(
@$"{typeKeyword} MyType1[||]
{{
}}

{typeKeyword} MyType2
{{
}}",
    expectedSuccess: false);

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(980758, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
        public Task MoveToNamespace_PartialTypesInNamesapce_SelectType()
            => TestMoveToNamespaceAsync(
@"namespace NS
{
    partial class MyClass[||]
    {
    }

    partial class MyClass
    {
    }
}",
    expectedSuccess: false);

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(980758, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
        public Task MoveToNamespace_PartialTypesInNamesapce_SelectNamespace()
            => TestMoveToNamespaceAsync(
@"namespace NS[||]
{
    partial class MyClass
    {
    }

    partial class MyClass
    {
    }
}",
    expectedSuccess: false);

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(980758, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/980758")]
        public Task MoveToNamespace_PartialTypesInGlobalNamesapce()
            => TestMoveToNamespaceAsync(
@"partial class MyClass[||]
{
}
partial class MyClass
{
}",
    expectedSuccess: false);

        [Fact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        [WorkItem(39234, "https://github.com/dotnet/roslyn/issues/39234")]
        public async Task TestMultiTargetingProject()
        {
            // Create two projects with same project file path and single linked document to simulate a multi-targeting project.
            var input = @"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj1"" FilePath=""SharedProj.csproj"">
        <Document FilePath=""CurrentDocument.cs"">
namespace A
{
    public class Class1
    {
    }

    public class Class2[||]
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""Proj2"" FilePath=""SharedProj.csproj"">
        <Document IsLinkFile=""true"" LinkAssemblyName=""Proj1"" LinkFilePath=""CurrentDocument.cs""/>
    </Project>
</Workspace>";

            var expected =
@"namespace A
{
    public class Class1
    {
    }
}

namespace B
{
    public class Class2
    {
    }
}";
            using var workspace = TestWorkspace.Create(System.Xml.Linq.XElement.Parse(input), exportProvider: ExportProviderFactory.CreateExportProvider());

            // Set the target namespace to "B"
            var testDocument = workspace.Projects.Single(p => p.Name == "Proj1").Documents.Single();
            var document = workspace.CurrentSolution.GetDocument(testDocument.Id);
            var movenamespaceService = document.GetLanguageService<IMoveToNamespaceService>();
            var moveToNamespaceOptions = new MoveToNamespaceOptionsResult("B");
            ((TestMoveToNamespaceOptionsService)movenamespaceService.OptionsService).SetOptions(moveToNamespaceOptions);

            var (_, action) = await GetCodeActionsAsync(workspace, default);
            var operations = await VerifyActionAndGetOperationsAsync(workspace, action, default);
            var result = ApplyOperationsAndGetSolution(workspace, operations);

            // Make sure both linked documents are changed.
            foreach (var id in workspace.Documents.Select(d => d.Id))
            {
                var changedDocument = result.Item2.GetDocument(id);
                var changedRoot = await changedDocument.GetSyntaxRootAsync();
                var actualText = changedRoot.ToFullString();
                Assert.Equal(expected, actualText);
            }
        }
    }
}
