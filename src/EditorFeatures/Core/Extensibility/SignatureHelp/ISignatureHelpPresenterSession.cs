// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ISignatureHelpPresenterSession : IIntelliSensePresenterSession
    {
        void PresentItems(ITrackingSpan triggerSpan, IList<SignatureHelpItem> items, SignatureHelpItem selectedItem, int? selectedParameter);
        void SelectPreviousItem();
        void SelectNextItem();

        event EventHandler<SignatureHelpItemEventArgs> ItemSelected;

        bool EditorSessionIsActive { get; }
    }
}
