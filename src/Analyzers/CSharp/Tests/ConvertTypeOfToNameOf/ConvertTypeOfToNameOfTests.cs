// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ConvertTypeOfToNameOf;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertTypeOfToNameOf
{
    using VerifyCS = CSharpCodeFixVerifier<CSharpConvertTypeOfToNameOfDiagnosticAnalyzer,
        CSharpConvertTypeOfToNameOfCodeFixProvider>;

    public partial class ConvertTypeOfToNameOfTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task BasicType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var typeName = [|typeof(Test).Name|];
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var typeName = nameof(Test);
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task ClassLibraryType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var typeName = [|typeof(System.String).Name|];
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var typeName = nameof(System.String);
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task ClassLibraryTypeWithUsing()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var typeName = [|typeof(String).Name|];
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var typeName = nameof(String);
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NestedCall()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var typeName = Foo([|typeof(System.String).Name|]);
    }

    int Foo(String typeName) {
        return 0;
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var typeName = Foo(nameof(String));
    }

    int Foo(String typeName) {
        return 0;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NotOnVariableContainingType()
        {
            var text = @"using System;

class Test
{
    void Method()
    {
        var typeVar = typeof(String);
        var typeName = typeVar.Name;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task PrimitiveType()
        {
            var text = @"class Test
{
    void Method()
    {
            var typeName = [|typeof(int).Name|];
    }
}
";
            var expected = @"class Test
{
    void Method()
    {
            var typeName = nameof(System.Int32);
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task PrimitiveTypeWithUsing()
        {
            var text = @"using System;

class Test
{
    void Method()
    {
            var typeName = [|typeof(int).Name|];
    }
}
";
            var expected = @"using System;

class Test
{
    void Method()
    {
            var typeName = nameof(Int32);
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NotOnGenericType()
        {
            var text = @"class Test<T>
{
    void Method()
    {
        var typeName = typeof(T).Name;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NotOnSimilarStatements()
        {
            var text = @"class Test
{
    void Method()
    {
        var typeName1 = typeof(Test);
        var typeName2 = typeof(Test).ToString();
        var typeName3 = typeof(Test).FullName;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NotInGenericType()
        {
            var text = @"class Test
{
    class Goo<T> 
    { 
        void M() 
        {
            _ = typeof(Goo<int>).Name;
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, text);
        }

        [WorkItem(47129, "https://github.com/dotnet/roslyn/issues/47129")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NestedInGenericType()
        {
            var text = @"class Test
{
    class Goo<T> 
    { 
        class Bar 
        { 
            void M() 
            {
                _ = [|typeof(Bar).Name|];
            }
        }
    }
}
";
            var expected = @"class Test
{
    class Goo<T> 
    { 
        class Bar 
        { 
            void M() 
            {
                _ = nameof(Bar);
            }
        }
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [WorkItem(47129, "https://github.com/dotnet/roslyn/issues/47129")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NestedInGenericType2()
        {
            var text = @"using System;
using System.Collections.Generic;

class Test
{
    public void M()
    {
        Console.WriteLine([|typeof(List<int>.Enumerator).Name|]);
    }
}
";
            var expected = @"using System;
using System.Collections.Generic;

class Test
{
    public void M()
    {
        Console.WriteLine(nameof(List<Int32>.Enumerator));
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, expected);
        }

        [WorkItem(54233, "https://github.com/dotnet/roslyn/issues/54233")]
        [Fact, Trait(Traits.Feature, Traits.Features.ConvertTypeOfToNameOf)]
        public async Task NotOnVoid()
        {
            var text = @"
class C
{
    void M()
    {
        var x = typeof(void).Name;
    }
}
";
            await VerifyCS.VerifyCodeFixAsync(text, text);
        }
    }
}
