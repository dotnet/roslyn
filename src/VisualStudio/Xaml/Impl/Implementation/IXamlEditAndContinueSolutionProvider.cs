// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    /// <summary>
    /// A copy of IEditAndContinueSolutionProvider that's usable by the XAML Language Service
    /// </summary>
    internal interface IXamlEditAndContinueSolutionProvider
    {
        event Action<Solution>? SolutionCommitted;
    }
}
