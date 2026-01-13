// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    [Trait(Traits.Feature, Traits.Features.Completion)]
    public class YieldCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override Type GetCompletionProviderType()
            => typeof(YieldCompletionProvider);

        private string Normalize(string text) => text.Replace("\r\n", "\n").Replace("\n", "\r\n");

        [WpfFact]
        public async Task TestInIteratorMethod()
        {
            await VerifyItemExistsAsync(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestInAsyncIteratorMethod()
        {
            await VerifyItemExistsAsync(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    async IAsyncEnumerable<int> M()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestInMethodReturningIEnumerator()
        {
            await VerifyItemExistsAsync(@"
using System.Collections.Generic;
class C
{
    IEnumerator<int> M()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestInMethodReturningIAsyncEnumerator()
        {
            await VerifyItemExistsAsync(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    async IAsyncEnumerator<int> M()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestInMethodReturningIEnumerable()
        {
            await VerifyItemExistsAsync(@"
using System.Collections;
class C
{
    IEnumerable M()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestInMethodReturningIEnumeratorNonGeneric()
        {
            await VerifyItemExistsAsync(@"
using System.Collections;
class C
{
    IEnumerator M()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestInLocalFunction()
        {
            await VerifyItemExistsAsync(@"
using System.Collections.Generic;
class C
{
    void M()
    {
        IEnumerable<int> L()
        {
            $$
        }
    }
}", "yield");
        }

        [WpfFact]
        public async Task TestInPropertyGetter()
        {
            await VerifyItemExistsAsync(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> P
    {
        get
        {
            $$
        }
    }
", "yield");
        }

        [WpfFact]
        public async Task TestNotInConstructor()
        {
            await VerifyItemIsAbsentAsync(@"
using System.Collections.Generic;
class C
{
    C()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestNotInVoidMethod()
        {
            await VerifyItemIsAbsentAsync(@"
using System.Collections.Generic;
class C
{
    void M()
    {
        $$
    }
", "yield");
        }

        [WpfFact]
        public async Task TestNotInLambda()
        {
            await VerifyItemIsAbsentAsync(@"
using System.Collections.Generic;
using System;
class C
{
    void M()
    {
        Func<IEnumerable<int>> f = () =>
        {
            $$
        };
    }
}", "yield");
        }

        [WpfFact]
        public async Task TestAddAsyncToIAsyncEnumerableMethod()
        {
            await VerifyCustomCommitProviderAsync(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    IAsyncEnumerable<int> M()
    {
        $$
    }
", "yield", Normalize(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    async IAsyncEnumerable<int> M()
    {
        yield
    }
"));
        }

        [WpfFact]
        public async Task TestAddAsyncToIAsyncEnumeratorMethod()
        {
            await VerifyCustomCommitProviderAsync(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    IAsyncEnumerator<int> M()
    {
        $$
    }
", "yield", Normalize(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    async IAsyncEnumerator<int> M()
    {
        yield
    }
"));
        }

        [WpfFact]
        public async Task TestDoNotAddAsyncIfAlreadyAsync()
        {
            await VerifyCustomCommitProviderAsync(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    async IAsyncEnumerable<int> M()
    {
        $$
    }
", "yield", Normalize(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    async IAsyncEnumerable<int> M()
    {
        yield
    }
"));
        }

        [WpfFact]
        public async Task TestDoNotAddAsyncToIEnumerableMethod()
        {
            await VerifyCustomCommitProviderAsync(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        $$
    }
", "yield", Normalize(@"
using System.Collections.Generic;
class C
{
    IEnumerable<int> M()
    {
        yield
    }
"));
        }

        [WpfFact]
        public async Task TestDoNotAddAsyncToProperty()
        {
            // Properties cannot be async
            await VerifyCustomCommitProviderAsync(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    IAsyncEnumerable<int> P
    {
        get
        {
            $$
        }
    }
", "yield", Normalize(@"
using System.Collections.Generic;
using System.Threading.Tasks;
class C
{
    IAsyncEnumerable<int> P
    {
        get
        {
            yield
        }
    }
"));
        }
    }
}