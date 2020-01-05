// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Adornments
{
    internal class GraphicsResult : IDisposable
    {
        public UIElement VisualElement { get; }
        private Action _dispose;

        public GraphicsResult(UIElement visualElement, Action dispose)
        {
            VisualElement = visualElement;
            _dispose = dispose;
        }

        public void Dispose()
        {
            if (_dispose != null)
            {
                _dispose();

                _dispose = null;
            }
        }
    }
}
