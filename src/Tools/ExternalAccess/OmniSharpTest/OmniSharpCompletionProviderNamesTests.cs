// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.CSharp.Completion;
using Xunit;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.UnitTests
{
    public class OmniSharpCompletionProviderNamesTests
    {
        [Fact]
        public void AllProviderNamesAreCorrect()
        {
            Assert.Equal(typeof(ObjectCreationCompletionProvider).FullName, OmniSharpCompletionProviderNames.ObjectCreationCompletionProvider);
            Assert.Equal(typeof(OverrideCompletionProvider).FullName, OmniSharpCompletionProviderNames.OverrideCompletionProvider);
            Assert.Equal(typeof(PartialMethodCompletionProvider).FullName, OmniSharpCompletionProviderNames.PartialMethodCompletionProvider);
            Assert.Equal(typeof(InternalsVisibleToCompletionProvider).FullName, OmniSharpCompletionProviderNames.InternalsVisibleToCompletionProvider);
            Assert.Equal(typeof(TypeImportCompletionProvider).FullName, OmniSharpCompletionProviderNames.TypeImportCompletionProvider);
            Assert.Equal(typeof(ExtensionMethodImportCompletionProvider).FullName, OmniSharpCompletionProviderNames.ExtensionMethodImportCompletionProvider);

            Assert.Equal(6, typeof(OmniSharpCompletionProviderNames).GetFields(BindingFlags.NonPublic | BindingFlags.Static).Length);
        }
    }
}
