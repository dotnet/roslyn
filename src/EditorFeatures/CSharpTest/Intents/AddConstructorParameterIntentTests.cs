// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.ExternalAccess.IntelliCode.Api;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents
{
    [UseExportProvider]
    public class AddConstructorParameterIntentTests : IntentTestsBase
    {
        [Fact]
        public async Task AddConstructorParameterWithField()
        {
            var initialText =
                """
                class C
                {
                    private readonly int _someInt;{|priorSelection:|}

                    public C()
                    {
                    }
                }
                """;

            var currentText =
                """
                class C
                {
                    private readonly int _someInt;

                    public C(int som)
                    {
                    }
                }
                """;
            var expectedText =
                """
                class C
                {
                    private readonly int _someInt;

                    public C(int someInt)
                    {
                        _someInt = someInt;
                    }
                }
                """;

            await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, initialText, currentText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddConstructorParameterWithProperty()
        {
            var initialText =
                """
                class C
                {
                    public int SomeInt { get; }{|priorSelection:|}

                    public C()
                    {
                    }
                }
                """;
            var currentText =
                """
                class C
                {
                    public int SomeInt { get; }

                    public C(int som)
                    {
                    }
                }
                """;
            var expectedText =
                """
                class C
                {
                    public int SomeInt { get; }

                    public C(int someInt)
                    {
                        SomeInt = someInt;
                    }
                }
                """;

            await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, initialText, currentText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddMultipleConstructorParameters()
        {
            var initialText =
                """
                class C
                {
                    {|priorSelection:private readonly int _someInt;
                    private readonly string _someString;|}

                    public C()
                    {
                    }
                }
                """;
            var currentText =
                """
                class C
                {
                    {|priorSelection:private readonly int _someInt;
                    private readonly string _someString;|}

                    public C(int som)
                    {
                    }
                }
                """;
            var expectedText =
                """
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
                """;

            await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, initialText, currentText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddConstructorParameterOnlyAddsSelected()
        {
            var initialText =
                """
                class C
                {
                    private readonly int _someInt;{|priorSelection:|}
                    private readonly string _someString;

                    public C()
                    {
                    }
                }
                """;
            var currentText =
                """
                class C
                {
                    private readonly int _someInt;{|priorSelection:|}
                    private readonly string _someString;

                    public C(int som)
                    {
                    }
                }
                """;
            var expectedText =
                """
                class C
                {
                    private readonly int _someInt;
                    private readonly string _someString;

                    public C(int someInt)
                    {
                        _someInt = someInt;
                    }
                }
                """;

            await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, initialText, currentText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddConstructorParameterUsesCodeStyleOption()
        {
            var initialText =
                """
                class C
                {
                    private readonly int _someInt;{|priorSelection:|}

                    public C()
                    {
                    }
                }
                """;
            var currentText =
                """
                class C
                {
                    private readonly int _someInt;{|priorSelection:|}

                    public C(int som)
                    {
                    }
                }
                """;
            var expectedText =
                """
                class C
                {
                    private readonly int _someInt;

                    public C(int someInt)
                    {
                        this._someInt = someInt;
                    }
                }
                """;
            await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, initialText, currentText, expectedText,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CodeStyleOptions2.QualifyFieldAccess, true }
                }).ConfigureAwait(false);
        }

        [Fact]
        public async Task AddConstructorParameterUsesExistingAccessibility()
        {
            var initialText =
                """
                class C
                {
                    private readonly int _someInt;{|priorSelection:|}

                    protected C()
                    {
                    }
                }
                """;
            var currentText =
                """
                class C
                {
                    private readonly int _someInt;{|priorSelection:|}

                    protected C(int som)
                    {
                    }
                }
                """;
            var expectedText =
                """
                class C
                {
                    private readonly int _someInt;

                    protected C(int someInt)
                    {
                        _someInt = someInt;
                    }
                }
                """;

            await VerifyExpectedTextAsync(WellKnownIntents.AddConstructorParameter, initialText, currentText, expectedText).ConfigureAwait(false);
        }
    }
}
