// Copyright(c) Microsoft.All Rights Reserved.Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.MoveToNamespace;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveToNamespace
{
    public class MoveToNamespaceTests : AbstractMoveToNamespaceTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceName()
        {
            var markup =
@"using System;

namespace A[||] 
{
    class MyClass
    {
        void Method() { }
    }
}";

            var expectedMarkup =
@"using System;

namespace B
{
    class MyClass
    {
        void Method() { }
    }
}";
            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_CaretOnNamespaceKeyword()
        {
            var markup =
@"using System;

namespace[||] A
{
    class MyClass
    {
        void Method() { }
    }
}";

            var expectedMarkup =
@"using System;

namespace B
{
    class MyClass
    {
        void Method() { }
    }
}";
            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_MultipleDeclarations()
        {
            var markup =
@"using System;

namespace A[||] 
{
    class MyClass
    {
        void Method() { }
    }

    class MyOtherClass
    {
        void Method() { }
    }
}";

            var expectedMarkup =
@"using System;

namespace B
{
    class MyClass
    {
        void Method() { }
    }

    class MyOtherClass
    {
        void Method() { }
    }
}";
            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_WithVariousSymbols()
        {
            var markup =
@"using System;

namespace A[||] 
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
}";

            var expectedMarkup =
@"using System;

namespace B
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
}";
            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_NestedNamespace()
        {
            var markup =
@"using System;

namespace A[||]
{
    namespace C 
    {
        class MyClass
        {
            void Method() { }
        }
    }
}";
            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveItems_NestedNamespace2()
        {
            var markup =
@"using System;

namespace A
{
    namespace C[||]
    {
        class MyClass
        {
            void Method() { }
        }
    }
}";
            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Single()
        {
            var markup =
@"namespace A
{
    class MyClass[||]
    {
    }
}";

            var expected =
@"namespace B
{
    class MyClass
    {
    }
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_SingleTop()
        {
            var markup =
@"namespace A
{
    class MyClass[||]
    {
    }

    class MyClass2
    {
    }
}";

            var expected =
@"namespace B
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
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_TopWithReference()
        {
            var markup =
@"namespace A
{
    class MyClass[||] : IMyClass
    {
    }

    interface IMyClass
    {
    }
}";

            var expected =
@"using A;

namespace B
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
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Bottom()
        {
            var markup =
@"namespace A
{
    class MyClass
    {
    }

    class MyClass2[||]
    {
    }
}";

            var expected =
@"namespace A
{
    class MyClass
    {
    }
}

namespace B
{
    class MyClass2
    {
    }
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_BottomReference()
        {
            var markup =
@"namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass[||]
    {
    }
}";

            var expected =
@"namespace A
{
    class MyClass : IMyClass
    {
    }
}

namespace B
{
    interface IMyClass
    {
    }
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_Middle()
        {
            var markup =
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
}";

            var expected =
@"namespace A
{
    class MyClass
    {
    }
}

namespace B
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
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference()
        {
            var markup =
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
}";

            var expected =
@"namespace A
{
    class MyClass : IMyClass
    {
    }
}

namespace B
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
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_MiddleReference2()
        {
            var markup =
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
}";

            var expected =
@"using A;

namespace A
{
    class MyClass : IMyClass
    {
    }

    interface IMyClass
    {
    }
}

namespace B
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
}";

            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expected);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public Task MoveToNamespace_MoveType_NestedInNamespace()
        {
            var markup =
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
}";
            return TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }
    }
}
