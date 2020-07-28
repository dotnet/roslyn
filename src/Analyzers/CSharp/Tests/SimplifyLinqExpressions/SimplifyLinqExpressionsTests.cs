// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpressions;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpressions
{
    public partial class SimplifyLinqExpressionsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpSimplifyLinqExpressionsDiagnosticAnalyzer(), new CSharpSimplifyLinqExpressionsCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpressions)]
        public async Task TestBasicCase()

        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().Where(x => x==1).Single();
    }
}";
            var fixedSource = @"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{
    static void Main()
    {
        static IEnumerable<int> Data()
        {
            yield return 1;
            yield return 2;
        }

        var test = Data().Single(x => x==1);
    }
}";
            await TestInRegularAndScriptAsync(source, fixedSource);
        }
    }
}
