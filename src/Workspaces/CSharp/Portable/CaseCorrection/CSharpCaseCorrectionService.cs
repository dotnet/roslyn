﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CaseCorrection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CaseCorrection
{
    [ExportLanguageService(typeof(ICaseCorrectionService), LanguageNames.CSharp), Shared]
    internal class CSharpCaseCorrectionService : AbstractCaseCorrectionService
    {
        [ImportingConstructor]
        public CSharpCaseCorrectionService()
        {
        }

        protected override void AddReplacements(
            SemanticModel? semanticModel,
            SyntaxNode root,
            ImmutableArray<TextSpan> spans,
            Workspace workspace,
            ConcurrentDictionary<SyntaxToken, SyntaxToken> replacements,
            CancellationToken cancellationToken)
        {
            // C# doesn't support case correction since we are a case sensitive language.
            return;
        }
    }
}
