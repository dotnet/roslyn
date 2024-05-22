// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Classification;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Classification
{
    public abstract class AbstractCSharpClassifierTests : AbstractClassifierTests
    {
        protected static EditorTestWorkspace CreateWorkspace(string code, ParseOptions options, TestHost testHost)
        {
            var composition = EditorTestCompositions.EditorFeatures.WithTestHostParts(testHost);
            return EditorTestWorkspace.CreateCSharp(code, parseOptions: options, composition: composition, isMarkup: false);
        }

        protected override async Task DefaultTestAsync(string code, string allCode, TestHost testHost, FormattedClassification[] expected)
        {
            await TestAsync(code, allCode, testHost, parseOptions: null, expected);
            await TestAsync(code, allCode, testHost, parseOptions: Options.Script, expected);
        }

        protected override string WrapInClass(string className, string code)
=> $@"class {className} {{
    {code}
}}";

        protected override string WrapInExpression(string code)
=> $@"class C {{
    void M() {{
        var q =
            {code}
    }}
}}";

        protected override string WrapInMethod(string className, string methodName, string code)
=> $@"class {className} {{
    void {methodName}() {{
        {code}
    }}
}}";

        protected override string WrapInNamespace(string code)
=> $@"namespace N {{
    {code}
}}";
    }
}
