// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TypeInferrer
{
    public partial class TypeInferrerTests
    {
        private async Task TestDelegateAsync(string text, string expectedType)
        {
            TextSpan textSpan;
            MarkupTestFile.GetSpan(text, out text, out textSpan);

            Document document = await fixture.UpdateDocumentAsync(text, SourceCodeKind.Regular);

            var root = (await document.GetSyntaxTreeAsync()).GetRoot();
            var node = FindExpressionSyntaxFromSpan(root, textSpan);

            var typeInference = document.GetLanguageService<ITypeInferenceService>();
            var delegateType = typeInference.InferDelegateType(await document.GetSemanticModelAsync(), node, CancellationToken.None);

            Assert.NotNull(delegateType);
            Assert.Equal(expectedType, delegateType.ToNameDisplayString());
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDeclaration1()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    Func<int> q = [|here|];
  }
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestAssignment1()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    Func<int> f;
    f = [|here|]
  }
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestArgument1()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    Bar([|here|]);
  }

  void Bar(Func<int> f);
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestConstructor1()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    new C([|here|]);
  }

  public C(Func<int> f);
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestDelegateConstructor1()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    new Func<int>([|here|]);
  }
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCastExpression1()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    (Func<int>)[|here|]
  }
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestCastExpression2()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    (Func<int>)([|here|]
  }
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestReturnFromMethod()
        {
            var text =
@"using System;
class C
{
  Func<int> M()
  {
    return [|here|]
  }
}";

            await TestDelegateAsync(text, "System.Func<int>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public async Task TestInsideLambda1()
        {
            var text =
@"using System;
class C
{
  void M()
  {
    Func<int,Func<string,bool>> f = i => [|here|]
  }
}";

            await TestDelegateAsync(text, "System.Func<string, bool>");
        }
    }
}
