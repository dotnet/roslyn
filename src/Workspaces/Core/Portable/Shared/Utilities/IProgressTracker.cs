namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal interface IProgressTracker
    {
        int CompletedItems { get; }
        int TotalItems { get; }

        void AddItems(int count);
        void ItemCompleted();
        void Clear();
    }
}