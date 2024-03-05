// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus;

internal partial class ContainedLanguage : IVsContainedCode
{
    public int HostSpansUpdated()
        => VSConstants.S_OK;

    /// <summary>
    /// Returns the list of code blocks in the generated .cs file that comes from the ASP.NET
    /// markup compiler. These blocks of code are delimited by #line directives (ExternSource
    /// directives in VB). The TextSpan that we return is the span of the lines between the
    /// start #line and ending #line default directives (#End ExternSource in VB), and the
    /// cookie is the numeric line number given in the #line directive.
    /// </summary>
    public int EnumOriginalCodeBlocks(out IVsEnumCodeBlocks ppEnum)
    {
        IList<TextSpanAndCookie> result = null;

        var uiThreadOperationExecutor = ComponentModel.GetService<IUIThreadOperationExecutor>();
        uiThreadOperationExecutor.Execute(
            "Intellisense",
            defaultDescription: "",
            allowCancellation: false,
            showProgress: false,
            action: c => result = EnumOriginalCodeBlocksWorker(c.UserCancellationToken));

        ppEnum = new CodeBlockEnumerator(result);
        return VSConstants.S_OK;
    }

    private IList<TextSpanAndCookie> EnumOriginalCodeBlocksWorker(CancellationToken cancellationToken)
    {
        var snapshot = this.SubjectBuffer.CurrentSnapshot;
        var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            return SpecializedCollections.EmptyList<TextSpanAndCookie>();
        }

        return document.GetVisibleCodeBlocks(cancellationToken)
            .Select(tuple => new TextSpanAndCookie
            {
                CodeSpan = new VsTextSpan
                {
                    iStartLine = snapshot.GetLineNumberFromPosition(tuple.Item1.Start),
                    iStartIndex = 0,
                    iEndLine = snapshot.GetLineNumberFromPosition(tuple.Item1.End),
                    iEndIndex = tuple.Item1.End - snapshot.GetLineFromPosition(tuple.Item1.End).Start,
                },
                ulHTMLCookie = tuple.Item2,
            })
            .ToArray();
    }
}
