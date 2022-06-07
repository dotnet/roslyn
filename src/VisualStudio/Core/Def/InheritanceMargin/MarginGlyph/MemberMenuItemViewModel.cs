// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Wpf;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.InheritanceMargin.MarginGlyph
{
    /// <summary>
    /// View model used to display a member in MenuItem. Only used when there are multiple members on the same line.
    /// e.g.
    /// interface IBar
    /// {
    ///     event EventHandler e1, e2
    /// }
    /// public class Bar : IBar
    /// {
    ///    public event EventHandler e1, e2
    /// }
    /// And this view model is used to show the first level entry to let the user choose member.
    /// </summary>
    internal class MemberMenuItemViewModel : MenuItemViewModel
    {
        /// <summary>
        /// Inheritance Targets for this member.
        /// </summary>
        public ImmutableArray<MenuItemViewModel> Targets { get; }

        public MemberMenuItemViewModel(
            string displayContent,
            ImageMoniker imageMoniker,
            ImmutableArray<MenuItemViewModel> targets) : base(displayContent, imageMoniker)
        {
            Targets = targets;
        }
    }
}
