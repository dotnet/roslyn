// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal interface IProgressTracker
    {
        string Description { get; set; }
        int CompletedItems { get; }
        int TotalItems { get; }

        void AddItems(int count);
        void ItemCompleted();
        void Clear();
    }

    internal class NoOpProgressTracker : IProgressTracker
    {
        public static readonly IProgressTracker Instance = new NoOpProgressTracker();

        private NoOpProgressTracker()
        {
        }

        public string Description { get => null; set { } }

        public int CompletedItems => 0;

        public int TotalItems => 0;

        public void AddItems(int count)
        {
        }

        public void Clear()
        {
        }

        public void ItemCompleted()
        {
        }
    }
}
