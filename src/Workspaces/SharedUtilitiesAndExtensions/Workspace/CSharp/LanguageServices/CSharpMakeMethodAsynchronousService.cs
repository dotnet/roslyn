// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MakeMethodAsynchronous;

namespace Microsoft.CodeAnalysis.CSharp.MakeMethodAsynchronous
{
    [ExportLanguageService(typeof(IMakeMethodAsynchronousService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpMakeMethodAsynchronousService : AbstractMakeMethodAsynchronousService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpMakeMethodAsynchronousService()
        {
        }

        public override bool IsAsyncReturnType(ITypeSymbol type, KnownTaskTypes knownTaskTypes)
        {
            return IsIAsyncEnumerableOrEnumerator(type, knownTaskTypes) || IsTaskLikeType(type, knownTaskTypes);
        }
    }
}
