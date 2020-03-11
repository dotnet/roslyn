// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal sealed class VSTypeScriptInlineRenameReplacementInfo : IInlineRenameReplacementInfo
    {
        private readonly IVSTypeScriptInlineRenameReplacementInfo _info;

        public VSTypeScriptInlineRenameReplacementInfo(IVSTypeScriptInlineRenameReplacementInfo info)
        {
            Contract.ThrowIfNull(info);
            _info = info;
        }

        public Solution NewSolution => _info.NewSolution;

        public bool ReplacementTextValid => _info.ReplacementTextValid;

        public IEnumerable<DocumentId> DocumentIds => _info.DocumentIds;

        public IEnumerable<InlineRenameReplacement> GetReplacements(DocumentId documentId)
        {
            return _info.GetReplacements(documentId)?.Select(x =>
                new InlineRenameReplacement(VSTypeScriptInlineRenameReplacementKindHelpers.ConvertTo(x.Kind), x.OriginalSpan, x.NewSpan));
        }
    }
}
