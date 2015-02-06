using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// The result of command execution.  
    /// </summary>
    public struct ExecutionResult
    {
        public static readonly ExecutionResult Success = new ExecutionResult(true);
        public static readonly ExecutionResult Failure = new ExecutionResult(false);
        public static readonly Task<ExecutionResult> Succeeded = Task.FromResult(Success);
        public static readonly Task<ExecutionResult> Failed = Task.FromResult(Failure);

        private readonly bool isSuccessful;

        public ExecutionResult(bool isSuccessful)
        {
            this.isSuccessful = isSuccessful;
        }

        public bool IsSuccessful
        {
            get
            {
                return isSuccessful;
            }
        }
    }
}