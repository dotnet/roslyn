// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.RequestOrdering
{
    [Shared, ExportLspMethod(MethodName)]
    internal class SlowParallelHandler : AbstractTestRequestHandler
    {
        public const string MethodName = nameof(SlowParallelHandler);

        protected override TimeSpan Delay => TimeSpan.FromSeconds(1);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SlowParallelHandler(ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
        }
    }
}
