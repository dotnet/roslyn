// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.InheritanceMargin;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.CodeAnalysis.Editor.InheritanceMargin.MarginGlyph
{
    internal class TargetDisplayViewModel
    {
        private readonly DefinitionItem _definitionItem;
        public string DisplayName { get; }
        public ImageMoniker ImageMoniker { get; }
        public string ToolTip { get; }
        public DelegateCommand Command { get; }

        public TargetDisplayViewModel(InheritanceTargetItem target)
        {
            DisplayName = target.DefinitionItem.DisplayParts.JoinText();
            ImageMoniker = InheritanceMarginHelpers.GetMoniker(target.RelationToMember);
            ToolTip = $"Navigate to {DisplayName}.";
            _definitionItem = target.DefinitionItem;
            Command = new DelegateCommand(NavigateToTarget);
        }

        private void NavigateToTarget()
        {

        }
    }
}
