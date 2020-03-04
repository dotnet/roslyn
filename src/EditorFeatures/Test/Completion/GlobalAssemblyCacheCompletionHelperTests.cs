﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Completion.FileSystem;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntelliSense.CompletionSetSources
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class GlobalAssemblyCacheCompletionHelperTests
    {
        [ConditionalFact(typeof(WindowsOnly))]
        public void ExistingReference()
        {
            var code = "System.Windows";
            VerifyPresence(code, "System.Windows.Forms");
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void FullReferenceIdentity()
        {
            var code = "System,";
            VerifyPresence(code, typeof(System.Diagnostics.Process).Assembly.FullName);
        }

        private static void VerifyPresence(string pathSoFar, string completionItem)
        {
            var completions = GetItems(pathSoFar);
            Assert.True(completions.Any(c => c.DisplayText == completionItem));
        }

        private static IEnumerable<CompletionItem> GetItems(string pathSoFar)
        {
            var helper = new GlobalAssemblyCacheCompletionHelper(CompletionItemRules.Default);
            return helper.GetItems(pathSoFar, CancellationToken.None);
        }
    }
}
