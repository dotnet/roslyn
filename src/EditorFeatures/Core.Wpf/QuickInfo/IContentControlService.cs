// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal interface IContentControlService : IWorkspaceService
    {
        /// <summary>
        /// return <see cref="ViewHostingControl"/> from the given <paramref name="textBuffer"/>'s <paramref name="contentSpan"/>
        /// </summary>
        ViewHostingControl CreateViewHostingControl(ITextBuffer textBuffer, Span contentSpan);

        /// <summary>
        /// get <see cref="DisposableToolTip"/> /> from the given <paramref name="textBuffer"/>'s <paramref name="contentSpan"/>
        /// based on given <paramref name="baseDocument"/>
        /// 
        /// tooltip will show embeded textview which shows code from the content span of the text buffer with the context of the
        /// base document
        /// </summary>
        /// <param name="baseDocument">document to be used as a context for the code</param>
        /// <param name="textBuffer">buffer to show in the tooltip text view</param>
        /// <param name="contentSpan">actual span to show in the tooptip</param>
        /// <param name="backgroundResourceKey">background of the tooltip control</param>
        /// <returns>ToolTip control with dispose method</returns>
        DisposableToolTip CreateDisposableToolTip(Document baseDocument, ITextBuffer textBuffer, Span contentSpan, object backgroundResourceKey);

        /// <summary>
        /// get <see cref="DisposableToolTip"/> /> from the given <paramref name="textBuffer"/>
        /// 
        /// tooltip will show embeded textview with whole content from the buffer. if the buffer has associated tags
        /// in its property bag, it will be picked up by taggers associated with the tooltip
        /// </summary>
        DisposableToolTip CreateDisposableToolTip(ITextBuffer textBuffer, object backgroundResourceKey);

        /// <summary>
        /// attach <see cref="DisposableToolTip"/> to the given <paramref name="element"/>
        /// 
        /// this will lazily create the tooltip and dispose it properly when it goes away
        /// </summary>
        void AttachToolTipToControl(FrameworkElement element, Func<DisposableToolTip> createToolTip);
    }
}
