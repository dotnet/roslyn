﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Wrapping
{
    public abstract class AbstractWrappingTests : AbstractCSharpCodeActionTest
    {
        protected sealed override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
            => FlattenActions(actions);

        private protected TestParameters GetIndentionColumn(int column)
            => new(globalOptions: Option(CodeActionOptionsStorage.WrappingColumn, column));

        protected Task TestAllWrappingCasesAsync(
            string input,
            params string[] outputs)
        {
            return TestAllWrappingCasesAsync(input, parameters: null, outputs);
        }

        private protected Task TestAllWrappingCasesAsync(
            string input,
            TestParameters parameters,
            params string[] outputs)
        {
            return TestAllInRegularAndScriptAsync(input, parameters, outputs);
        }
    }
}
