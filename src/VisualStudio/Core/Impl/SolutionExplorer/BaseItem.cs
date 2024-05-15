// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    /// <summary>
    /// Abstract base class for a custom node in Solution Explorer. This utilizes the core
    /// SolutionExplorer extensibility similar to 
    /// Microsoft.VisualStudio.Shell.TreeNavigation.HierarchyProvider.dll and
    /// Microsoft.VisualStudio.Shell.TreeNavigation.GraphProvider.dll.
    /// </summary>
    internal abstract class BaseItem :
        LocalizableProperties,
        ITreeDisplayItem,
        IInteractionPatternProvider,
        IInvocationPattern,
        IContextMenuPattern,
        INotifyPropertyChanged,
        IDragDropSourcePattern,
        IBrowsablePattern,
        ISupportDisposalNotification,
        IPrioritizedComparable
    {
        public virtual event PropertyChangedEventHandler PropertyChanged { add { } remove { } }

        private readonly string _name;

        public BaseItem(string name)
        {
            _name = name;
        }

        public IEnumerable<string> Children => SpecializedCollections.EmptyEnumerable<string>();

        public bool IsExpandable => true;

        public FontStyle FontStyle => FontStyles.Normal;
        public FontWeight FontWeight => FontWeights.Normal;

        public virtual ImageSource? Icon => null;
        public virtual ImageMoniker IconMoniker => default;
        public virtual ImageSource? ExpandedIcon => null;
        public virtual ImageMoniker ExpandedIconMoniker => IconMoniker;

        public bool AllowIconTheming => true;
        public bool AllowExpandedIconTheming => true;
        public bool IsCut => false;
        public ImageSource? OverlayIcon => null;
        public virtual ImageMoniker OverlayIconMoniker => default;
        public ImageSource? StateIcon => null;
        public virtual ImageMoniker StateIconMoniker => default;
        public string? StateToolTipText => null;
        public override string ToString() => Text;
        public string Text => _name;
        public object? ToolTipContent => null;
        public string ToolTipText => _name;

        private static readonly HashSet<Type> s_supportedPatterns =
        [
            typeof(ISupportExpansionEvents),
            typeof(IRenamePattern),
            typeof(IInvocationPattern),
            typeof(IContextMenuPattern),
            typeof(IDragDropSourcePattern),
            typeof(IDragDropTargetPattern),
            typeof(IBrowsablePattern),
            typeof(ITreeDisplayItem),
            typeof(ISupportDisposalNotification)
        ];

        public TPattern? GetPattern<TPattern>() where TPattern : class
        {
            if (!IsDisposed)
            {
                if (s_supportedPatterns.Contains(typeof(TPattern)))
                {
                    return this as TPattern;
                }
            }
            else
            {
                // If this item has been deleted, it no longer supports any patterns
                // other than ISupportDisposalNotification.
                // It's valid to use GetPattern on a deleted item, but there are no
                // longer any pattern contracts it fulfills other than the contract
                // that reports the item as a dead ITransientObject.
                if (typeof(TPattern) == typeof(ISupportDisposalNotification))
                {
                    return this as TPattern;
                }
            }

            return null;
        }

        public bool CanPreview => false;

        public virtual IInvocationController? InvocationController => null;

        public virtual IContextMenuController? ContextMenuController => null;

        public IDragDropSourceController? DragDropSourceController => null;

        public virtual object GetBrowseObject()
        {
            return this;
        }

        public bool IsDisposed => false;
        public int Priority => 0;

        public int CompareTo(object obj)
        {
            return 1;
        }
    }
}
