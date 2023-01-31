// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.BracePairs;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.BracePairs
{
    [ExportLanguageService(typeof(IBracePairsService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpBracePairsService : AbstractBracePairsService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpBracePairsService()
            : base(CSharpSyntaxKinds.Instance)
        {
        }
    }
}
