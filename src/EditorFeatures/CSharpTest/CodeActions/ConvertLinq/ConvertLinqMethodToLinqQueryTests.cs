// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    public class ConvertLinqMethodToLinqQueryTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new Microsoft.CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqMethodToLinqQueryProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task TestBasicConvertLinqMethodToLinqQuery()
        {
            await TestInRegularAndScriptAsync(
@"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> numbers)
    {
        return [||]numbers.Where(num => num % 2 == 0).OrderBy(n => n);
    }
}
",
@"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M(IEnumerable<int> numbers)
    {
        return from num in numbers
            where num % 2 == 0
            orderby num
            select num;
    }
}
");
        }
    }
}
