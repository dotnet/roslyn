// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class CopilotOnTheFlyDocsInfoWrapper
    {
        private readonly OnTheFlyDocsInfo _onTheFlyDocsInfo;

        public CopilotOnTheFlyDocsInfoWrapper(OnTheFlyDocsInfo onTheFlyDocsInfo)
        {
            _onTheFlyDocsInfo = onTheFlyDocsInfo;

        }

        public string SymbolSignature => _onTheFlyDocsInfo.SymbolSignature;
        public ImmutableArray<string> DeclarationCode => _onTheFlyDocsInfo.DeclarationCode;
        public string Language => _onTheFlyDocsInfo.Language;
        public bool IsContentExcluded => _onTheFlyDocsInfo.IsContentExcluded;
        public ImmutableArray<string> AdditionalContext => _onTheFlyDocsInfo.AdditionalContext;
        public bool HasComments => _onTheFlyDocsInfo.HasComments;
    }
}
