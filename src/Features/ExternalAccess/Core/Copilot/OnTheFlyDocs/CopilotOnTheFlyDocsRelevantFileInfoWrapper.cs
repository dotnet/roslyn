// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
namespace Microsoft.CodeAnalysis.ExternalAccess.Unified.Copilot;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;
#endif

internal sealed class CopilotOnTheFlyDocsRelevantFileInfoWrapper(OnTheFlyDocsRelevantFileInfo onTheFlyDocsRelevantFileInfo)
{
    private readonly OnTheFlyDocsRelevantFileInfo _onTheFlyDocsRelevantFileInfo = onTheFlyDocsRelevantFileInfo;

    public Document Document => _onTheFlyDocsRelevantFileInfo.Document;
    public TextSpan TextSpan => _onTheFlyDocsRelevantFileInfo.TextSpan;
}
