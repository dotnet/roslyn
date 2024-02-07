// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Text.Adornments
{
    internal sealed class ContainerElement
    {
        public IEnumerable<object> Elements { get; }

        public ContainerElementStyle Style { get; }

        public ContainerElement(ContainerElementStyle style, IEnumerable<object> elements)
        {
            Style = style;
            Elements = elements?.ToImmutableList() ?? throw new ArgumentNullException("elements");
        }

        public ContainerElement(ContainerElementStyle style, params object[] elements)
        {
            Style = style;
            Elements = elements?.ToImmutableList() ?? throw new ArgumentNullException("elements");
        }
    }
}