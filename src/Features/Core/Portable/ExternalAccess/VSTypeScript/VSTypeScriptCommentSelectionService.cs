// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [ExportLanguageService(typeof(ICommentSelectionService), InternalLanguageNames.TypeScript), Shared]
    internal sealed class VSTypeScriptCommentSelectionService : ICommentSelectionService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptCommentSelectionService()
        {
        }

        public CommentSelectionInfo GetInfo()
            => new(
                supportsSingleLineComment: true,
                supportsBlockComment: true,
                singleLineCommentString: "//",
                blockCommentStartString: "/*",
                blockCommentEndString: "*/");
    }
}
