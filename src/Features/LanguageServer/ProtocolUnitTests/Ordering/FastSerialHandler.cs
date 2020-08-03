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
    internal class FastSerialHandler : AbstractTestRequestHandler
    {
        public const string MethodName = nameof(FastSerialHandler);

        public override RequestProcessingMode Type => RequestProcessingMode.Serial;

        protected override TimeSpan Delay => TimeSpan.FromMilliseconds(100);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FastSerialHandler(ILspSolutionProvider solutionProvider)
            : base(solutionProvider)
        {
        }
    }
}
