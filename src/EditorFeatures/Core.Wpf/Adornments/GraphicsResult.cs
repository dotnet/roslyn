﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
