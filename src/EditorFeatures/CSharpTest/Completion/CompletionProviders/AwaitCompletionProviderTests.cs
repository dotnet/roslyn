// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    /// <summary>
    /// The <see cref="AwaitCompletionProvider"/> adds async modifier if the return type is Task or ValueTask.
    /// The tests here are only checking whether the completion item is provided or not.
    /// Tests for checking adding async modifier are in:
    /// src/EditorFeatures/Test2/IntelliSense/CSharpCompletionCommandHandlerTests_AwaitCompletion.vb
    /// </summary>
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class AwaitCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType() => typeof(AwaitCompletionProvider);

        private async Task VerifyAbsenceAsync(string code)
        {
            await VerifyItemIsAbsentAsync(code, "await");
        }

        private async Task VerifyAbsenceAsync(string code, LanguageVersion languageVersion)
        {
            await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), "await");
        }

        private async Task VerifyKeywordAsync(string code, LanguageVersion languageVersion, string? inlineDescription = null)
        {
            await VerifyItemExistsAsync(GetMarkup(code, languageVersion), "await", glyph: (int)Glyph.Keyword, inlineDescription: inlineDescription);
        }

        [Fact]
        public async Task TestNotInTypeContext()
        {
            await VerifyAbsenceAsync(@"
class Program
{
    $$
}");
        }

        [Fact]
        public async Task TestStatementInMethod()
        {
            await VerifyKeywordAsync(@"
class C
{
  void F()
  {
    $$  }
}", LanguageVersion.CSharp9, FeaturesResources.Make_containing_scope_async);
        }

        [Fact]
        public async Task TestStatementInMethod_Async()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    $$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestStatementInMethod_TopLevel()
        {
            await VerifyKeywordAsync("$$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestExpressionInAsyncMethod()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    var z = $$  }
}
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestExpressionInNonAsyncMethodWithTaskReturn()
        {
            await VerifyKeywordAsync(@"
class C
{
  Task F()
  {
    var z = $$  }
}
", LanguageVersion.CSharp9, FeaturesResources.Make_containing_scope_async);
        }

        [Fact]
        public async Task TestExpressionInAsyncMethod_TopLevel()
        {
            await VerifyKeywordAsync("var z = $$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUsingStatement()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    using $$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUsingStatement_TopLevel()
        {
            await VerifyAbsenceAsync("using $$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUsingDirective()
            => await VerifyAbsenceAsync("using $$");

        [Fact]
        public async Task TestGlobalUsingDirective()
            => await VerifyAbsenceAsync("global using $$");

        [Fact]
        public async Task TestForeachStatement()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    foreach $$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestForeachStatement_TopLevel()
        {
            await VerifyAbsenceAsync("foreach $$", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInQuery()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    var z = from a in ""char""
          select $$  }
    }
", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInQuery_TopLevel()
        {
            await VerifyAbsenceAsync(
@"var z = from a in ""char""
          select $$", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInFinally()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    try { }
finally { $$ }  }
}", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInFinally_TopLevel()
        {
            await VerifyKeywordAsync(
@"try { }
finally { $$ }", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInCatch()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    try { }
catch { $$ }  }
}", LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Fact]
        public async Task TestInCatch_TopLevel()
        {
            await VerifyKeywordAsync(
@"try { }
catch { $$ }", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInLock()
        {
            await VerifyAbsenceAsync(@"
class C
{
  async Task F()
  {
    lock(this) { $$ }  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestNotInLock_TopLevel()
        {
            await VerifyAbsenceAsync("lock (this) { $$ }", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestInAsyncLambdaInCatch()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    try { }
catch { var z = async () => $$ }  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestInAsyncLambdaInCatch_TopLevel()
        {
            await VerifyKeywordAsync(
@"try { }
catch { var z = async () => $$ }", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestAwaitInLock()
        {
            await VerifyKeywordAsync(@"
class C
{
  async Task F()
  {
    lock($$  }
}", LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestAwaitInLock_TopLevel()
        {
            await VerifyKeywordAsync("lock($$", LanguageVersion.CSharp9);
        }
    }
}
