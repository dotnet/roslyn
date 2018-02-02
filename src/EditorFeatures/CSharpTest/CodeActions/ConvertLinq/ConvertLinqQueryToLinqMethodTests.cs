// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLinq
{
    public class ConvertLinqQueryToLinqMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
         => new CodeAnalysis.CSharp.ConvertLinq.CSharpConvertLinqQueryToLinqMethodProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert1()
        {
            await Test(
@"from num in numbers
where num % 2 == 0
orderby num
select num",
@"numbers.Where(num => num % 2 == 0).OrderBy(num => num)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert2()
        {
            await Test(
@"from a in new[] { 1, 2, 3 }
    select a",
@"new[] { 1, 2, 3 }");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert3()
        {
            await Test(
@"from x in numbers where x< 5 select x* x",
@"numbers.Where(x => x < 5).Select(x => x * x)");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert4()
        {
            await Test(
@"from x in ""123"" 
    let z = x.ToString()
    select z into w
    select int.Parse(w)",
@"""123"".Select(x => x.ToString())...");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert5()
        {
            await Test(@"
        return from t in TypeAndBaseTypes(node)
                   from f in Fields(t)
                   select f", @"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert6()
        {
            await Test(@"
          return from t in types
                       where t.InferredType != null && t.InferredType.OriginalDefinition.Equals(taskOfT)
                       let nt = (INamedTypeSymbol) t.InferredType
                       where nt.TypeArguments.Length == 1
                       select new TypeInferenceInfo(nt.TypeArguments[0]);", @"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert7()
        {
            await Test(@"
        return from a in args join[|$$b |] in args on a equals b;", @"");
        }


        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert8()
        {
            await Test(@"
         from a in args select a into[|$$b |] from c in b select c;", @"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert9()
        {
            await Test(@"
         return from handle in reader.MethodDefinitions
                   let method = reader.GetMethodDefinition(handle)
                   let import = method.GetImport()
                   where !import.Name.IsNil
                   select method", @"");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLinq)]
        public async Task Convert10()
        {
            await Test(@"
          from sentence in strings
          let words = sentence.Split(' ')
          from word in words
          let w = word.ToLower()
          where w[0] == 'a' || w[0] == 'e'
              || w[0] == 'i' || w[0] == 'o'
              || w[0] == 'u'
          select word", @"");
        }

        private async Task Test(string input, string expectedOutput)
        {
            const string code = @"
using System.Collections.Generic;
using System.Linq;
class C
{
    IEnumerable<int> M()
    {
       return ##;
    }
}
";
            await TestInRegularAndScriptAsync(code.Replace("##", "[||]" + input), code.Replace("##", expectedOutput));
        }
    }
}
