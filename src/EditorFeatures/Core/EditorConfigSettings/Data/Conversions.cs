// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;

internal readonly struct Conversions<TFrom, TTo>(Func<TFrom, TTo> to, Func<TTo, TFrom> from)
{
    public readonly Func<TFrom, TTo> To = to;
    public readonly Func<TTo, TFrom> From = from;
}
