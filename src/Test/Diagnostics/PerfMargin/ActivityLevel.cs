// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Roslyn.Hosting.Diagnostics.PerfMargin
{
    /// <summary>
    /// Represents whether each feature is active or inactive.
    /// 
    /// The IsActive property indicates whether a given feature is currently in the
    /// middle of an operation.  Features can be grouped into a parent ActivityLevel
    /// which is active when any of its children are active.
    /// </summary>
    internal class ActivityLevel
    {
        private int _isActive;
        private readonly List<ActivityLevel> _children;
        private readonly ActivityLevel _parent;

        public ActivityLevel(string name)
        {
            this.Name = name;
            _children = new List<ActivityLevel>();
        }

        public ActivityLevel(string name, ActivityLevel parent, bool createChildList)
        {
            this.Name = name;
            _parent = parent;
            _parent._children.Add(this);

            if (createChildList)
            {
                _children = new List<ActivityLevel>();
            }
        }

        public event EventHandler IsActiveChanged;

        public string Name { get; }

        public bool IsActive
        {
            get
            {
                return _isActive > 0;
            }
        }

        public void Start()
        {
            var current = Interlocked.Increment(ref _isActive);
            if (current == 1)
            {
                ActivityLevelChanged();
            }

            if (_parent != null)
            {
                _parent.Start();
            }
        }

        public void Stop()
        {
            var current = Interlocked.Decrement(ref _isActive);
            if (current == 0)
            {
                ActivityLevelChanged();
            }

            if (_parent != null)
            {
                _parent.Stop();
            }
        }

        internal void SortChildren()
        {
            if (_children != null)
            {
                _children.Sort(new Comparison<ActivityLevel>((a, b) => string.CompareOrdinal(a.Name, b.Name)));
                foreach (var child in _children)
                {
                    child.SortChildren();
                }
            }
        }

        private void ActivityLevelChanged()
        {
            this.IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyCollection<ActivityLevel> Children
        {
            get { return _children; }
        }
    }
}