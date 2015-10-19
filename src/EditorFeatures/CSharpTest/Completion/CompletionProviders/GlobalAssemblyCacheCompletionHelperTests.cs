// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Completion.FileSystem;
using Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    public class GlobalAssemblyCacheCompletionHelperTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ExistingReference()
        {
            var code = "System.Windows";
            VerifyPresence(code, "System.Windows.Forms");
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FullReferenceIdentity()
        {
            var code = "System,";
            VerifyPresence(code, typeof(System.Diagnostics.Process).Assembly.FullName);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void FullReferenceIdentityDescription()
        {
            var code = "System";
            var completions = GetItems(code);
            var systemsColl = from completion in completions
                              where completion.DisplayText == "System"
                              select completion;

            Assert.True(systemsColl.Any(
                completion => completion.GetDescriptionAsync().Result.GetFullText() == typeof(System.Diagnostics.Process).Assembly.FullName));
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NothingOnForwardSlash()
        {
            var code = "System.Windows/";
            VerifyAbsence(code);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NothingOnBackSlash()
        {
            var code = @"System.Windows\";
            VerifyAbsence(code);
        }

        private static void VerifyPresence(string pathSoFar, string completionItem)
        {
            var completions = GetItems(pathSoFar);
            Assert.True(completions.Any(c => c.DisplayText == completionItem));
        }

        private static void VerifyAbsence(string pathSoFar)
        {
            var completions = GetItems(pathSoFar);
            Assert.True(completions == null || !completions.Any(), "Expected null or non-empty completions");
        }

        private static IEnumerable<CompletionItem> GetItems(string pathSoFar)
        {
            var helper = new GlobalAssemblyCacheCompletionHelper(null, new TextSpan());

            return helper.GetItems(pathSoFar, documentPath: null);
        }
    }
}
