namespace Roslyn.Utilities
{
    internal partial class TaskExtensions
    {
        // Used as a placeholder TResult to indicate that a Task<TResult> has a void TResult
        private struct VoidResult
        {
        }
    }
}