// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal sealed class NoOpFixAllProvider : FixAllProvider
    {
        public static readonly NoOpFixAllProvider Instance = new();

        private NoOpFixAllProvider()
        {
        }

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => Task.FromResult<CodeAction?>(null);
    }
}
