// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;

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

        public IEnumerable<string> Children
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public bool IsExpandable
        {
            get
            {
                return true;
            }
        }

        public FontStyle FontStyle
        {
            get { return FontStyles.Normal; }
        }

        public FontWeight FontWeight
        {
            get { return FontWeights.Normal; }
        }

        public virtual ImageSource Icon
        {
            get { return null; }
        }

        public virtual ImageMoniker IconMoniker
        {
            get { return default; }
        }

        public virtual ImageSource ExpandedIcon
        {
            get { return null; }
        }

        public virtual ImageMoniker ExpandedIconMoniker
        {
            get { return default; }
        }

        public bool AllowIconTheming
        {
            get { return true; }
        }

        public bool AllowExpandedIconTheming
        {
            get { return true; }
        }

        public bool IsCut
        {
            get { return false; }
        }

        public ImageSource OverlayIcon
        {
            get { return null; }
        }

        public virtual ImageMoniker OverlayIconMoniker
        {
            get { return default; }
        }

        public ImageSource StateIcon
        {
            get { return null; }
        }

        public virtual ImageMoniker StateIconMoniker
        {
            get { return default; }
        }

        public string StateToolTipText
        {
            get { return null; }
        }

        public override string ToString()
        {
            return Text;
        }

        public string Text
        {
            get { return _name; }
        }

        public object ToolTipContent
        {
            get { return null; }
        }

        public string ToolTipText
        {
            get { return _name; }
        }

        private static readonly HashSet<Type> s_supportedPatterns = new HashSet<Type>()
        {
            typeof(ISupportExpansionEvents),
            typeof(IRenamePattern),
            typeof(IInvocationPattern),
            typeof(IContextMenuPattern),
            typeof(IDragDropSourcePattern),
            typeof(IDragDropTargetPattern),
            typeof(IBrowsablePattern),
            typeof(ITreeDisplayItem),
            typeof(ISupportDisposalNotification)
        };

        public TPattern GetPattern<TPattern>() where TPattern : class
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

        public bool CanPreview
        {
            get { return false; }
        }

        public virtual IInvocationController InvocationController
        {
            get
            {
                return null;
            }
        }

        public virtual IContextMenuController ContextMenuController
        {
            get
            {
                return null;
            }
        }

        public IDragDropSourceController DragDropSourceController
        {
            get
            {
                return null;
            }
        }

        public virtual object GetBrowseObject()
        {
            return this;
        }

        public bool IsDisposed
        {
            get { return false; }
        }

        public int Priority
        {
            get { return 0; }
        }

        public int CompareTo(object obj)
        {
            return 1;
        }
    }
}
