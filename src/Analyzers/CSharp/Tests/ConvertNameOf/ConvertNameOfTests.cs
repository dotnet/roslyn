// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ConvertNameOf;
using Microsoft.CodeAnalysis.CSharp.CSharpConvertNameOfCodeFixProvider;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertNameOf
{
    public partial class ConvertNameOfTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpConvertNameOfDiagnosticAnalyzer(), new CSharpConvertNameOfCodeFixProvider());

        [Fact]
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

        [Fact]
        public async Task ClassLibraryType()
        {
            var text = @"
class Test
{
    void Method()
    {
        var typeName = [||]typeof(String).Name;
    }
}
";
            var expected = @"
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
    }
}
