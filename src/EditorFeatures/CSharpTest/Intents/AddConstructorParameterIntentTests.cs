// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents;

[UseExportProvider]
public sealed class AddConstructorParameterIntentTests : IntentTestsBase
{
    [Fact]
    public async Task AddConstructorParameterWithField()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, """
            class C
            {
                private readonly int _someInt;{|priorSelection:|}

                public C()
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;

                public C(int som)
                {
                }
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
    public async Task AddConstructorParameterWithProperty()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, """
            class C
            {
                public int SomeInt { get; }{|priorSelection:|}

                public C()
                {
                }
            }
            """, """
            class C
            {
                public int SomeInt { get; }

                public C(int som)
                {
                }
            }
            """, """
            class C
            {
                public int SomeInt { get; }

                public C(int someInt)
                {
                    SomeInt = someInt;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task AddMultipleConstructorParameters()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, """
            class C
            {
                {|priorSelection:private readonly int _someInt;
                private readonly string _someString;|}

                public C()
                {
                }
            }
            """, """
            class C
            {
                {|priorSelection:private readonly int _someInt;
                private readonly string _someString;|}

                public C(int som)
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;
                private readonly string _someString;

                public C(int someInt, string someString)
                {
                    _someInt = someInt;
                    _someString = someString;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task AddConstructorParameterOnlyAddsSelected()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, """
            class C
            {
                private readonly int _someInt;{|priorSelection:|}
                private readonly string _someString;

                public C()
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;{|priorSelection:|}
                private readonly string _someString;

                public C(int som)
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;
                private readonly string _someString;

                public C(int someInt)
                {
                    _someInt = someInt;
                }
            }
            """).ConfigureAwait(false);
    }

    [Fact]
    public async Task AddConstructorParameterUsesCodeStyleOption()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, """
            class C
            {
                private readonly int _someInt;{|priorSelection:|}

                public C()
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;{|priorSelection:|}

                public C(int som)
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;

                public C(int someInt)
                {
                    this._someInt = someInt;
                }
            }
            """,
            options: new OptionsCollection(LanguageNames.CSharp)
            {
                { CodeStyleOptions2.QualifyFieldAccess, true }
            }).ConfigureAwait(false);
    }

    [Fact]
    public async Task AddConstructorParameterUsesExistingAccessibility()
    {
        await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, """
            class C
            {
                private readonly int _someInt;{|priorSelection:|}

                protected C()
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;{|priorSelection:|}

                protected C(int som)
                {
                }
            }
            """, """
            class C
            {
                private readonly int _someInt;

                protected C(int someInt)
                {
                    _someInt = someInt;
                }
            }
            """).ConfigureAwait(false);
    }
}
