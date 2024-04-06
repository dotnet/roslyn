// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeSignature;
using Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities.ChangeSignature;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ChangeSignature
{
    [Trait(Traits.Feature, Traits.Features.ChangeSignature)]
    public partial class ChangeSignatureTests : AbstractChangeSignatureTests
    {
        [Fact]
        public async Task AddParameterAddsAllImports()
        {
            var markup = """
                class C
                {
                    void $$M() { }
                }
                """;

            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(
                    new AddedParameter(
                        null,
                        "Dictionary<ConsoleColor, Task<AsyncOperation>>",
                        "test",
                        CallSiteKind.Todo),
                    "System.Collections.Generic.Dictionary<System.ConsoleColor, System.Threading.Tasks.Task<System.ComponentModel.AsyncOperation>>")};

            var updatedCode = """
                using System;
                using System.Collections.Generic;
                using System.ComponentModel;
                using System.Threading.Tasks;

                class C
                {
                    void M(Dictionary<ConsoleColor, Task<AsyncOperation>> test) { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddParameterAddsOnlyMissingImports()
        {
            var markup = """
                using System.ComponentModel;

                class C
                {
                    void $$M() { }
                }
                """;

            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(
                    new AddedParameter(
                        null,
                        "Dictionary<ConsoleColor, Task<AsyncOperation>>",
                        "test",
                        CallSiteKind.Todo),
                    "System.Collections.Generic.Dictionary<System.ConsoleColor, System.Threading.Tasks.Task<System.ComponentModel.AsyncOperation>>")};

            var updatedCode = """
                using System;
                using System.Collections.Generic;
                using System.ComponentModel;
                using System.Threading.Tasks;

                class C
                {
                    void M(Dictionary<ConsoleColor, Task<AsyncOperation>> test) { }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }

        [Fact]
        public async Task AddParameterAddsImportsOnCascading()
        {
            var markup = """
                using NS1;

                namespace NS1
                {
                    class B
                    {
                        public virtual void M() { }
                    }
                }

                namespace NS2
                {
                    using System;
                    using System.Collections.Generic;
                    using System.ComponentModel;
                    using System.Threading.Tasks;

                    class D : B
                    {
                        public override void $$M() { }
                    }
                }
                """;

            var updatedSignature = new[] {
                new AddedParameterOrExistingIndex(
                    new AddedParameter(
                        null,
                        "Dictionary<ConsoleColor, Task<AsyncOperation>>",
                        "test",
                        CallSiteKind.Todo),
                    "System.Collections.Generic.Dictionary<System.ConsoleColor, System.Threading.Tasks.Task<System.ComponentModel.AsyncOperation>>")};

            var updatedCode = """
                using System;
                using System.Collections.Generic;
                using System.ComponentModel;
                using System.Threading.Tasks;
                using NS1;

                namespace NS1
                {
                    class B
                    {
                        public virtual void M(Dictionary<ConsoleColor, Task<AsyncOperation>> test) { }
                    }
                }

                namespace NS2
                {
                    using System;
                    using System.Collections.Generic;
                    using System.ComponentModel;
                    using System.Threading.Tasks;

                    class D : B
                    {
                        public override void M(Dictionary<ConsoleColor, Task<AsyncOperation>> test) { }
                    }
                }
                """;

            await TestChangeSignatureViaCommandAsync(LanguageNames.CSharp, markup, updatedSignature: updatedSignature, expectedUpdatedInvocationDocumentCode: updatedCode);
        }
    }
}
