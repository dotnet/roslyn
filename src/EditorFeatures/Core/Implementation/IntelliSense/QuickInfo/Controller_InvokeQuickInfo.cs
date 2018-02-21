// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller
    {
        public void InvokeQuickInfo(int position, IAsyncQuickInfoSession augmentSession)
        {
            StartSession(position, augmentSession);
        }
    }
}
