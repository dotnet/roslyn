// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics
{
    public abstract class AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest : AbstractDiagnosticProviderBasedUserDiagnosticTest
    {
        protected override ParseOptions GetScriptOptions() => Options.Script;

        protected override string GetLanguage() => LanguageNames.CSharp;

        protected override TestWorkspace CreateWorkspaceFromFile(string initialMarkup, TestParameters parameters)
            => TestWorkspace.CreateCSharp(initialMarkup, parameters.parseOptions, parameters.compilationOptions);

        private static readonly CSharpParseOptions s_73Options = new CSharpParseOptions(LanguageVersion.CSharp7_3);

        internal Task TestInRegular73AndScript1Async(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            CodeActionPriority? priority = null,
            TestParameters parameters = default)
        {
            parameters = parameters.WithParseOptions(
                ((CSharpParseOptions)parameters.parseOptions)?.WithLanguageVersion(LanguageVersion.CSharp7_3) ?? s_73Options);
            return base.TestInRegularAndScript1Async(initialMarkup, expectedMarkup, index, priority, parameters);
        }

        internal Task TestInRegular73AndScriptAsync(
            string initialMarkup,
            string expectedMarkup,
            int index = 0,
            CodeActionPriority? priority = null,
            CompilationOptions compilationOptions = null,
            IDictionary<OptionKey, object> options = null,
            object fixProviderData = null,
            ParseOptions parseOptions = null,
            string title = null)
        {
            parseOptions = ((CSharpParseOptions)parseOptions)?.WithLanguageVersion(LanguageVersion.CSharp7_3) ?? s_73Options;
            return TestInRegularAndScriptAsync(
                initialMarkup,
                expectedMarkup,
                index,
                priority,
                compilationOptions,
                options,
                fixProviderData,
                parseOptions,
                title);
        }

    }
}
