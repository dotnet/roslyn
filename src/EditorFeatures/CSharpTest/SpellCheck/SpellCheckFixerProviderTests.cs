// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.SpellCheck;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SpellCheck;

[UseExportProvider]
public class SpellCheckFixerProviderTests : AbstractSpellCheckFixerProviderTests
{
    protected override EditorTestWorkspace CreateWorkspace(string content)
        => EditorTestWorkspace.CreateCSharp(content);

    [WpfFact]
    public async Task TestRenameClassName()
    {
        await TestSuccessAsync(
            """
            class {|CorrectlySpelled:CrrectlySpelled|}
            {
                public CrrectlySpelled() { }
            }
            """,
            """
            class CorrectlySpelled
            {
                public CorrectlySpelled() { }
            }
            """);
    }

    [WpfFact]
    public async Task TestBogusLocation()
    {
        // Should not be called inside a string.  But we should still apply the change.
        await TestFailureAsync(
            """
            class C
            {
                void M()
                {
                    var v1 = "{|word:wrd|}";
                    var v2 = "wrd";
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    var v1 = "word";
                    var v2 = "wrd";
                }
            }
            """);
    }

    [WpfFact]
    public async Task TestReplacementThatLanguageDoesNotSupport()
    {
        // Should not be called inside a string.  But we should still apply the change.
        await TestFailureAsync(
            """
            class {|Bo()gus:Orginal|}
            {
                public Orginal() { }
            }
            """,
            """
            class Bo()gus
            {
                public Orginal() { }
            }
            """);
    }

    [WpfFact]
    public async Task TestReplacementSpanLargerThanToken()
    {
        // Replacement span is larger than the lang token to rename.
        await TestFailureAsync(
            """
            class {|Replacement:Class |}
            {
                public Class() { }
            }
            """,
            """
            class Replacement
            {
                public Class() { }
            }
            """);
    }
}
