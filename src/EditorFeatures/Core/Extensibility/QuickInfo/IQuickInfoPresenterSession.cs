// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IQuickInfoPresenterSession : IIntelliSensePresenterSession
    {
        QuickInfoItem ConvertQuickInfoItem(ITextBuffer subjectBuffer, SnapshotPoint triggerPoint, Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem quickInfoItem);
    }
}
