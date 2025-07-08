// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.TypeInferrer;

[Trait(Traits.Feature, Traits.Features.TypeInferenceService)]
public sealed partial class TypeInferrerTests
{
    private async Task TestDelegateAsync(string text, string expectedType)
    {
        using var workspaceFixture = GetOrCreateWorkspaceFixture();

        MarkupTestFile.GetSpan(text, out text, out var textSpan);

        var document = workspaceFixture.Target.UpdateDocument(text, SourceCodeKind.Regular);

        var root = await document.GetSyntaxRootAsync();
        var node = FindExpressionSyntaxFromSpan(root, textSpan);

        var typeInference = document.GetLanguageService<ITypeInferenceService>();
        var delegateType = typeInference.InferDelegateType(await document.GetSemanticModelAsync(), node, CancellationToken.None);

        Assert.NotNull(delegateType);
        Assert.Equal(expectedType, delegateType.ToNameDisplayString());
    }

    [Fact]
    public async Task TestDeclaration1()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                Func<int> q = [|here|];
              }
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestAssignment1()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                Func<int> f;
                f = [|here|]
              }
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestArgument1()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                Bar([|here|]);
              }

              void Bar(Func<int> f);
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestConstructor1()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                new C([|here|]);
              }

              public C(Func<int> f);
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestDelegateConstructor1()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                new Func<int>([|here|]);
              }
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestCastExpression1()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                (Func<int>)[|here|]
              }
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestCastExpression2()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                (Func<int>)([|here|]
              }
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestReturnFromMethod()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              Func<int> M()
              {
                return [|here|]
              }
            }
            """, "System.Func<int>");
    }

    [Fact]
    public async Task TestInsideLambda1()
    {
        await TestDelegateAsync("""
            using System;
            class C
            {
              void M()
              {
                Func<int,Func<string,bool>> f = i => [|here|]
              }
            }
            """, "System.Func<string, bool>");
    }
}
