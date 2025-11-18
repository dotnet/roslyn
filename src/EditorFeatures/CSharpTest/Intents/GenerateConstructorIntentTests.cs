// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents;

[UseExportProvider]
public sealed class GenerateConstructorIntentTests : IntentTestsBase
{
    [Fact]
    public async Task GenerateConstructorSimpleResult()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, """
            class C
            {
                private readonly int _someInt;

                {|priorSelection:|}
            }
            """, """
            class C
            {
                private readonly int _someInt;

                public C
            }
            """, """
            class C
            {
                private readonly int _someInt;

                public C(int someInt)
                {
                    _someInt = someInt;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateConstructorTypedPrivateWithoutIntentData()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, """
            class C
            {
                private readonly int _someInt;

                {|priorSelection:|}
            }
            """, """
            class C
            {
                private readonly int _someInt;

                private C
            }
            """, """
            class C
            {
                private readonly int _someInt;

                public C(int someInt)
                {
                    _someInt = someInt;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateConstructorTypedPrivateWithIntentData()
    {

        // lang=json
        var intentData = @"{ ""accessibility"": ""Private""}";

        await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, """
            class C
            {
                private readonly int _someInt;

                {|priorSelection:|}
            }
            """, """
            class C
            {
                private readonly int _someInt;

                private C
            }
            """, """
            class C
            {
                private readonly int _someInt;

                private C(int someInt)
                {
                    _someInt = someInt;
                }
            }
            """, intentData: intentData).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateConstructorTypedPrivateProtectedWithIntentData()
    {

        // lang=json
        var intentData = @"{ ""accessibility"": ""ProtectedAndInternal""}";

        await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, """
            class C
            {
                private readonly int _someInt;

                {|priorSelection:|}
            }
            """, """
            class C
            {
                private readonly int _someInt;

                private protected C
            }
            """, """
            class C
            {
                private readonly int _someInt;

                private protected C(int someInt)
                {
                    _someInt = someInt;
                }
            }
            """, intentData: intentData).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateConstructorWithFieldsInPartial()
    {
        var additionalDocuments = new string[]
        {
            """
            partial class C
            {
                private readonly int _someInt;
            }
            """
        };
        var expectedText =
            """
            partial class C
            {
                public C(int someInt)
                {
                    _someInt = someInt;
                }
            }
            """;

        await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, """
            partial class C
            {
                {|priorSelection:|}
            }
            """, """
            partial class C
            {
                public C
            }
            """, additionalDocuments, [expectedText]).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateConstructorWithReferenceType()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, """
            class C
            {
                private readonly object _someObject;

                {|priorSelection:|}
            }
            """, """
            class C
            {
                private readonly object _someObject;

                public C
            }
            """, """
            class C
            {
                private readonly object _someObject;

                public C(object someObject)
                {
                    _someObject = someObject;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task GenerateConstructorWithExpressionBodyOption()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, """
            class C
            {
                private readonly int _someInt;

                {|priorSelection:|}
            }
            """, """
            class C
            {
                private readonly int _someInt;

                public C
            }
            """, """
            class C
            {
                private readonly int _someInt;

                public C(int someInt) => _someInt = someInt;
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement }
            }).ConfigureAwait(false);
    }
}
