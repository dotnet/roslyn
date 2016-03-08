// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
