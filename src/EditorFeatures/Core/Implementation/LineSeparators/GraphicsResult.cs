// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Windows;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LineSeparators
{
    internal class GraphicsResult : IDisposable
    {
        private readonly UIElement _visualElement;
        private Action _dispose;

        public GraphicsResult(UIElement visualElement, Action dispose)
        {
            _visualElement = visualElement;
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

        public UIElement VisualElement
        {
            get
            {
                return _visualElement;
            }
        }
    }
}
