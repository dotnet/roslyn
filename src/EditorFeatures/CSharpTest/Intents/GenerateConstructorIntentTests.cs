﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Intents
{
    [UseExportProvider]
    public class GenerateConstructorIntentTests : IntentTestsBase
    {
        [Fact]
        public async Task GenerateConstructorSimpleResult()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    {|typed:public C|}
}";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, initialText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorTypedPrivate()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    {|typed:private C|}
}";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, initialText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithFieldsInPartial()
        {
            var initialText =
@"partial class C
{
    {|typed:public C|}
}";
            var additionalDocuments = new string[]
            {
@"partial class C
{
    private readonly int _someInt;
}"
            };
            var expectedText =
@"partial class C
{
    public C(int someInt)
    {
        _someInt = someInt;
    }
}";

            await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, initialText, additionalDocuments, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithReferenceType()
        {
            var initialText =
@"class C
{
    private readonly object _someObject;

    {|typed:public C|}
}";
            var expectedText =
@"class C
{
    private readonly object _someObject;

    public C(object someObject)
    {
        _someObject = someObject;
    }
}";

            await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, initialText, expectedText).ConfigureAwait(false);
        }

        [Fact]
        public async Task GenerateConstructorWithExpressionBodyOption()
        {
            var initialText =
@"class C
{
    private readonly int _someInt;

    {|typed:public C|}
}";
            var expectedText =
@"class C
{
    private readonly int _someInt;

    public C(int someInt) => _someInt = someInt;
}";

            await VerifyExpectedTextAsync(WellKnownIntents.GenerateConstructor, initialText, expectedText,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedConstructors, CSharpCodeStyleOptions.WhenPossibleWithSilentEnforcement }
                }).ConfigureAwait(false);
        }
    }
}
