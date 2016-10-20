// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Windows;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    partial class NamingRuleTreeItemViewModel : IDragDropTargetPattern
    {
        public DirectionalDropArea SupportedAreas
        {
            get { return DirectionalDropArea.Above | DirectionalDropArea.Below | DirectionalDropArea.On; }
        }

        public void OnDragEnter(DirectionalDropArea dropArea, DragEventArgs e)
        {
            UpdateAllowedEffects(dropArea, e);
        }

        private void UpdateAllowedEffects(DirectionalDropArea dropArea, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(NamingRuleTreeItemViewModel)))
            {
                NamingRuleTreeItemViewModel namingRule = e.Data.GetData(typeof(NamingRuleTreeItemViewModel)) as NamingRuleTreeItemViewModel;
                if (namingRule != null && IsDropAllowed(dropArea, target: this, source: namingRule))
                {
                    e.Effects = DragDropEffects.All;
                }
            }
        }

        public void OnDragLeave(DirectionalDropArea dropArea, DragEventArgs e)
        {
        }

        public void OnDragOver(DirectionalDropArea dropArea, DragEventArgs e)
        {
            UpdateAllowedEffects(dropArea, e);
        }

        public void OnDrop(DirectionalDropArea dropArea, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(NamingRuleTreeItemViewModel)))
            {
                NamingRuleTreeItemViewModel namingRule = e.Data.GetData(typeof(NamingRuleTreeItemViewModel)) as NamingRuleTreeItemViewModel;
                if (!namingRule.IsAncestorOfMe(this))
                {
                    switch (dropArea)
                    {
                        case DirectionalDropArea.On:
                            namingRule.Parent.Children.Remove(namingRule);
                            this.Children.Add(namingRule);
                            break;
                        case DirectionalDropArea.Above:
                            namingRule.Parent.Children.Remove(namingRule);
                            this.Parent.Children.Insert(this.Parent.Children.IndexOf(this), namingRule);
                            break;
                        case DirectionalDropArea.Below:
                            namingRule.Parent.Children.Remove(namingRule);
                            this.Parent.Children.Insert(this.Parent.Children.IndexOf(this) + 1, namingRule);
                            break;
                    }
                }
            }
        }

        private static bool IsDropAllowed(DirectionalDropArea dropArea, NamingRuleTreeItemViewModel target, NamingRuleTreeItemViewModel source)
        {
            switch (dropArea)
            {
                case DirectionalDropArea.On:
                    // Naming Rules can't be dropped on themselves or on any of their descendants.
                    if (source == target || source.IsAncestorOfMe(target))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                case DirectionalDropArea.Above:
                case DirectionalDropArea.Below:
                    // Naming Rules can't be dropped on themselves, or on any of their descendants.
                    if (source == target || source.IsAncestorOfMe(target))
                    {
                        return false;
                    }

                    // There must always be a single root Naming Rule
                    if (target.Parent == null)
                    {
                        return false;
                    }

                    // Insertions that would lead to the same order don't make sense
                    int direction = dropArea == DirectionalDropArea.Above ? -1 : 1;
                    return source.Parent != target.Parent || source.Parent.Children.IndexOf(source) != target.Parent.Children.IndexOf(target) + direction;
                default:
                    return false;
            }
        }
    }
}
