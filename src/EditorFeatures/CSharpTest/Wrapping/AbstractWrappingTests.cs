// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public abstract class AbstractWrappingTests : AbstractCSharpCodeActionTest
    {
        protected sealed override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        protected static Dictionary<OptionKey, object> GetIndentionColumn(int column)
            => new Dictionary<OptionKey, object>
               {
                   { FormattingOptions.PreferredWrappingColumn, column }
               };

        protected Task TestAllWrappingCasesAsync(
            string input,
            params string[] outputs)
        {
            return TestAllWrappingCasesAsync(input, options: null, outputs);
        }

        protected Task TestAllWrappingCasesAsync(
            string input,
            IDictionary<OptionKey, object> options,
            params string[] outputs)
        {
            var parameters = new TestParameters(options: options);
            return TestAllInRegularAndScriptAsync(input, parameters, outputs);
        }
    }
}
