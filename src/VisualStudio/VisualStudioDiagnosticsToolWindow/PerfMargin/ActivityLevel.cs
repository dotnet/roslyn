// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Roslyn.Hosting.Diagnostics.PerfMargin
{
    /// <summary>
    /// Represents whether each feature is active or inactive.
    /// 
    /// The IsActive property indicates whether a given feature is currently in the
    /// middle of an operation.  Features can be grouped into a parent ActivityLevel
    /// which is active when any of its children are active.
    /// </summary>
    internal sealed class ActivityLevel
    {
        private int _isActive;
        private readonly List<ActivityLevel> _children;
        private readonly ActivityLevel _parent;

        public ActivityLevel(string name)
        {
            Name = name;
            _children = [];
        }

        public ActivityLevel(string name, ActivityLevel parent, bool createChildList)
        {
            Name = name;
            _parent = parent;
            _parent._children.Add(this);

            if (createChildList)
            {
                _children = [];
            }
        }

        public event EventHandler IsActiveChanged;

        public string Name { get; }

        public bool IsActive
            => _isActive > 0;

        public void Start()
        {
            var current = Interlocked.Increment(ref _isActive);
            if (current == 1)
            {
                ActivityLevelChanged();
            }

            _parent?.Start();
        }

        public void Stop()
        {
            var current = Interlocked.Decrement(ref _isActive);
            if (current == 0)
            {
                ActivityLevelChanged();
            }

            _parent?.Stop();
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
            IsActiveChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyCollection<ActivityLevel> Children
            => _children;
    }
}
