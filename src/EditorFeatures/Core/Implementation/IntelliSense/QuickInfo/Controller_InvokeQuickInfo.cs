// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal partial class Controller
    {
#pragma warning disable CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
        public void InvokeQuickInfo(int position, bool trackMouse, IQuickInfoSession augmentSession)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            StartSession(position, trackMouse, augmentSession);
        }
#pragma warning restore CS0618 // IQuickInfo* is obsolete, tracked by https://github.com/dotnet/roslyn/issues/24094
    }
}
