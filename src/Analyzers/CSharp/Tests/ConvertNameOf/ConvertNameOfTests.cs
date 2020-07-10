// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConvertTypeofToNameof;
using Microsoft.CodeAnalysis.CSharp.CSharpConvertTypeOfToNameOfCodeFixProvider;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNameOf
{
    public partial class ConvertNameOfTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpConvertNameOfDiagnosticAnalyzer(), new CSharpConvertTypeOfToNameOfCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task BasicType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var typeName = [||]typeof(Test).Name;
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var typeName = [||]nameof(Test);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task ClassLibraryType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var typeName = [||]typeof(System.String).Name;
    }
}
";
            var expected = @"
class Test
{
    void Method()
    {
        var typeName = [||]nameof(System.String);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task ClassLibraryTypeWithUsing()
        {
            var text = @"
using System;

class Test
{
    void Method()
    {
        var typeName = [||]typeof(String).Name;
    }
}
";
            var expected = @"
using System;

class Test
{
    void Method()
    {
        var typeName = [||]nameof(String);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task NotOnVariableContainingType()
        {
            var text = @"using System;

class Test
{
    void Method()
    {
        var typeVar = typeof(String);[||]
        var typeName = typeVar.Name;
    }
}
";
            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task PrimitiveType()
        {
            var text = @"class Test
{
    void Method()
    {
            var typeName = [||]typeof(int).Name;
    }
}
";
            var expected = @"class Test
{
    void Method()
    {
            var typeName = [||]nameof(System.Int32);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task PrimitiveTypeWithUsing()
        {
            var text = @"using System;

class Test
{
    void Method()
    {
            var typeName = [||]typeof(int).Name;
    }
}
";
            var expected = @"using System;

class Test
{
    void Method()
    {
            var typeName = [||]nameof(Int32);
    }
}
";
            await TestInRegularAndScriptAsync(text, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task NotOnGenericType()
        {
            var text = @"class Test<T>
{
    void Method()[||]
    {
        var typeName = typeof(T).Name;
    }
}
";
            await TestMissingInRegularAndScriptAsync(text);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ConvertNameOf)]
        public async Task NotOnSimilarStatements()
        {
            var text = @"class Test
{
    void Method()[||]
    {
        var typeName1 = typeof(Test);
        var typeName2 = typeof(Test).toString();
        var typeName3 = typeof(Test).FullName;
    }
}
";
            await TestMissingInRegularAndScriptAsync(text);
        }
    }
}
