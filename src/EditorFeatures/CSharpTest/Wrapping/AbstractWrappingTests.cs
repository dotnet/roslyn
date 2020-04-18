// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public abstract class AbstractWrappingTests : AbstractCSharpCodeActionTest
    {
        protected sealed override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        private protected OptionsCollection GetIndentionColumn(int column)
            => new OptionsCollection(GetLanguage())
               {
                   { FormattingOptions2.PreferredWrappingColumn, column }
               };

        protected Task TestAllWrappingCasesAsync(
            string input,
            params string[] outputs)
        {
            return TestAllWrappingCasesAsync(input, options: null, outputs);
        }

        private protected Task TestAllWrappingCasesAsync(
            string input,
            OptionsCollection options,
            params string[] outputs)
        {
            var parameters = new TestParameters(options: options);
            return TestAllInRegularAndScriptAsync(input, parameters, outputs);
        }
    }
}
