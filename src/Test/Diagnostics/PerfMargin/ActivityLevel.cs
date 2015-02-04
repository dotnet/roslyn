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
        private int isActive;
        private readonly List<ActivityLevel> children;
        private readonly ActivityLevel parent;

        public ActivityLevel(string name)
        {
            this.Name = name;
            this.children = new List<ActivityLevel>();
        }

        public ActivityLevel(string name, ActivityLevel parent, bool createChildList)
        {
            this.Name = name;
            this.parent = parent;
            this.parent.children.Add(this);

            if (createChildList)
            {
                this.children = new List<ActivityLevel>();
            }
        }

        public event EventHandler IsActiveChanged;

        public string Name { get; private set; }

        public bool IsActive
        {
            get
            {
                return this.isActive > 0;
            }
        }

        public void Start()
        {
            var current = Interlocked.Increment(ref isActive);
            if (current == 1)
            {
                ActivityLevelChanged();
            }

            if (this.parent != null)
            {
                this.parent.Start();
            }
        }

        public void Stop()
        {
            var current = Interlocked.Decrement(ref isActive);
            if (current == 0)
            {
                ActivityLevelChanged();
            }

            if (this.parent != null)
            {
                this.parent.Stop();
            }
        }

        internal void SortChildren()
        {
            if (this.children != null)
            {
                this.children.Sort(new Comparison<ActivityLevel>((a, b) => string.CompareOrdinal(a.Name, b.Name)));
                foreach (var child in this.children)
                {
                    child.SortChildren();
                }
            }
        }

        private void ActivityLevelChanged()
        {
            var handlers = this.IsActiveChanged;
            if (handlers != null)
            {
                handlers(this, EventArgs.Empty);
            }
        }

        public IReadOnlyCollection<ActivityLevel> Children
        {
            get { return this.children; }
        }
    }
}