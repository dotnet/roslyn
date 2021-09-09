// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportArgumentProvider(nameof(ContextVariableArgumentProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(FirstBuiltInArgumentProvider))]
    [Shared]
    internal sealed class ContextVariableArgumentProvider : AbstractContextVariableArgumentProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ContextVariableArgumentProvider()
        {
        }
    }
}
