// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class AwaitKeywordRecommenderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType() => typeof(AwaitCompletionProvider);

        // Copy from:
        // https://github.com/dotnet/roslyn/blob/71130c8d2c59c36b8c098bc77b1b281efdd40071/src/EditorFeatures/CSharpTest2/Recommendations/RecommenderTests.cs#L198
        private static string AddInsideMethod(string text, bool isAsync = false, string returnType = "void", bool topLevelStatement = false)
        {
            if (topLevelStatement)
            {
                return returnType switch
                {
                    "void" => text,
                    "int" => text,
                    _ => throw new ArgumentException("Unsupported return type", nameof(returnType)),
                };
            }
            var builder = new StringBuilder();
            if (isAsync && returnType != "void")
            {
                builder.AppendLine("using System.Threading.Tasks;");
            }
            builder.AppendLine("class C");
            builder.AppendLine("{");
            builder.Append("  ");
            if (isAsync)
            {
                builder.Append("async ");
                if (returnType == "void")
                {
                    builder.Append("Task");
                }
                else
                {
                    builder.Append($"Task<{returnType}>");
                }
            }
            else
            {
                builder.Append(returnType);
            }
            builder.AppendLine(" F()");
            builder.AppendLine("  {");
            builder.Append("    ").Append(text);
            builder.AppendLine("  }");
            builder.Append("}");
            return builder.ToString();
        }

        private async Task VerifyAbsenceAsync(string code)
        {
            await VerifyItemIsAbsentAsync(code, "await");
        }

        private async Task VerifyAbsenceAsync(string code, LanguageVersion languageVersion)
        {
            await VerifyItemIsAbsentAsync(GetMarkup(code, languageVersion), "await");
        }

        private async Task VerifyKeywordAsync(string code, LanguageVersion languageVersion)
        {
            await VerifyItemExistsAsync(GetMarkup(code, languageVersion), "await");
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

        [Theory]
        [CombinatorialData]
        public async Task TestStatementInMethod(bool isAsync, bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", isAsync: isAsync, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestExpressionInAsyncMethod(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var z = $$", isAsync: true, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestUsingStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"using $$", topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [Fact]
        public async Task TestUsingDirective()
            => await VerifyAbsenceAsync("using $$");

        [Theory]
        [CombinatorialData]
        public async Task TestForeachStatement(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach $$", topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotInQuery(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var z = from a in ""char""
          select $$", isAsync: true, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Theory]
        [CombinatorialData]
        public async Task TestInFinally(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try { }
finally { $$ }", isAsync: true, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [WorkItem(907052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/907052")]
        [Theory]
        [CombinatorialData]
        public async Task TestInCatch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try { }
catch { $$ }", isAsync: true, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotInLock(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"lock(this) { $$ }", isAsync: true, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInAsyncLambdaInCatch(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"try { }
catch { var z = async () => $$ }", isAsync: true, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAwaitInLock(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"lock($$", isAsync: true, topLevelStatement: topLevelStatement), LanguageVersion.CSharp9);
        }
    }
}
