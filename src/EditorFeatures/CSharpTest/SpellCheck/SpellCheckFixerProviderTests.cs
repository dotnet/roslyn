// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.SpellCheck;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SpellCheck;

[UseExportProvider]
public sealed class SpellCheckFixerProviderTests : AbstractSpellCheckFixerProviderTests
{
    protected override EditorTestWorkspace CreateWorkspace(string content)
        => EditorTestWorkspace.CreateCSharp(content);

    [WpfFact]
    public Task TestRenameClassName()
        => TestSuccessAsync(
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

    [WpfFact]
    public Task TestBogusLocation()
        => TestFailureAsync(
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

    [WpfFact]
    public Task TestReplacementThatLanguageDoesNotSupport()
        => TestFailureAsync(
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

    [WpfFact]
    public Task TestReplacementSpanLargerThanToken()
        => TestFailureAsync(
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
