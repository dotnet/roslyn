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
        public async Task MoveToNamespace_CaretOnNamespaceName()
        {
            var markup = @"
using System;

namespace A$$ 
{
    class MyClass
    {
        void Method() { }
    }
}";

            var expectedMarkup = @"
using System;

namespace B
{
    class MyClass
    {
        void Method() { }
    }
}";
            await TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public async Task MoveToNamespace_CaretOnNamespaceKeyword()
        {
            var markup = @"
using System;

namespace$$ A
{
    class MyClass
    {
        void Method() { }
    }
}";

            var expectedMarkup = @"
using System;

namespace B
{
    class MyClass
    {
        void Method() { }
    }
}";
            await TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public async Task MoveToNamespace_CaretOnNamespaceNameMultipleDeclarations()
        {
            var markup = @"
using System;

namespace A$$ 
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

            var expectedMarkup = @"
using System;

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
            await TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public async Task MoveToNamespace_CaretOnTypeDeclaration()
        {
            var markup = @"
using System;
namespace A 
{
    class MyClass$$ 
    {
        void Method() {}
    }
}";
            await TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public async Task MoveToNamespace_WithVariousSymbols()
        {
            var markup = @"
using System;

namespace A$$ 
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

            var expectedMarkup = @"
using System;

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
            await TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: true,
                expectedNamespace: "B",
                expectedMarkup: expectedMarkup);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.MoveToNamespace)]
        public async Task MoveToNamespace_NestedNamespace()
        {
            var markup = @"
using System;

namespace A$$
{
    namespace C 
    {
        class MyClass
        {
            void Method() { }
        }
    }
}";
            await TestMoveToNamespaceCommandCSharpAsync(
                markup,
                expectedSuccess: false);
        }
    }
}
