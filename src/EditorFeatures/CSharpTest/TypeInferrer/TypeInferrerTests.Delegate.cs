// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TypeInferrer
{
    public partial class TypeInferrerTests
    {
        private void TestDelegate(string text, string expectedType)
        {
            TextSpan textSpan;
            MarkupTestFile.GetSpan(text, out text, out textSpan);

            Document document = fixture.UpdateDocument(text, SourceCodeKind.Regular);

            var root = document.GetSyntaxTreeAsync().Result.GetRoot();
            var node = FindExpressionSyntaxFromSpan(root, textSpan);

            var typeInference = document.GetLanguageService<ITypeInferenceService>();
            var delegateType = typeInference.InferDelegateType(document.GetSemanticModelAsync().Result, node, CancellationToken.None);

            Assert.NotNull(delegateType);
            Assert.Equal(expectedType, delegateType.ToNameDisplayString());
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestDeclaration1()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestAssignment1()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestArgument1()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestConstructor1()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestDelegateConstructor1()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCastExpression1()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestCastExpression2()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestReturnFromMethod()
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

            TestDelegate(text, "System.Func<int>");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
        public void TestInsideLambda1()
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

            TestDelegate(text, "System.Func<string, bool>");
        }
    }
}
