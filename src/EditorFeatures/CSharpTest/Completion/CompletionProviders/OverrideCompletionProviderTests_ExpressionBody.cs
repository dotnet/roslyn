// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    // OverrideCompletionProviderTests overrides SetWorkspaceOptions to disable
    // expression-body members. This class does the opposite.
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class OverrideCompletionProviderTests_ExpressionBody : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(OverrideCompletionProvider);

        internal override OptionsCollection NonCompletionOptions
            => new(LanguageNames.CSharp)
            {
                { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedProperties, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement },
                { CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement }
            };

        [WpfFact, WorkItem(16331, "https://github.com/dotnet/roslyn/issues/16334")]
        public async Task CommitProducesExpressionBodyProperties()
        {
            var markupBeforeCommit = """
                class B
                {
                    public virtual int A { get; set; }
                    class C : B
                    {
                        override A$$
                    }
                }
                """;

            var expectedCodeAfterCommit = """
                class B
                {
                    public virtual int A { get; set; }
                    class C : B
                    {
                        public override int A { get => base.A$$; set => base.A = value; }
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "A", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem(16331, "https://github.com/dotnet/roslyn/issues/16334")]
        public async Task CommitProducesExpressionBodyGetterOnlyProperty()
        {
            var markupBeforeCommit = """
                class B
                {
                    public virtual int A { get; }
                    class C : B
                    {
                        override A$$
                    }
                }
                """;

            var expectedCodeAfterCommit = """
                class B
                {
                    public virtual int A { get; }
                    class C : B
                    {
                        public override int A => base.A;$$
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "A", expectedCodeAfterCommit);
        }

        [WpfFact, WorkItem(16331, "https://github.com/dotnet/roslyn/issues/16334")]
        public async Task CommitProducesExpressionBodyMethod()
        {
            var markupBeforeCommit = """
                class B
                {
                    public virtual int A() => 2;
                    class C : B
                    {
                        override A$$
                    }
                }
                """;

            var expectedCodeAfterCommit = """
                class B
                {
                    public virtual int A() => 2;
                    class C : B
                    {
                        public override int A() => base.A();$$
                    }
                }
                """;

            await VerifyCustomCommitProviderAsync(markupBeforeCommit, "A()", expectedCodeAfterCommit);
        }
    }
}
