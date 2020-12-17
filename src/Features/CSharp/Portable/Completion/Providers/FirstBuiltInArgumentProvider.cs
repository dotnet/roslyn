// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    /// <summary>
    /// Provides an argument provider that always appears before any built-in argument provider. This argument
    /// provider does not provide any argument values.
    /// </summary>
    [ExportArgumentProvider(nameof(FirstBuiltInArgumentProvider), LanguageNames.CSharp)]
    [Shared]
    internal sealed class FirstBuiltInArgumentProvider : ArgumentProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FirstBuiltInArgumentProvider()
        {
        }

        public override Task ProvideArgumentAsync(ArgumentContext context)
            => Task.CompletedTask;
    }
}
