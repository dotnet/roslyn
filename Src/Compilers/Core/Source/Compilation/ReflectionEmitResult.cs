using System.Reflection;

namespace Microsoft.CodeAnalysis.Emit
{
    public abstract class CommonReflectionEmitResult : CommonEmitResult
    {
        private readonly MethodInfo entryPoint;
        private readonly bool isUncollectible;

        internal CommonReflectionEmitResult(MethodInfo entryPoint, bool success, bool isUncollectible)
            : base(success, generation: null)
        {
            this.entryPoint = entryPoint;
            this.isUncollectible = isUncollectible;
        }

        /// <summary>
        /// Gets method information about the entrypoint of the emitted assembly.
        /// </summary>
        public MethodInfo EntryPoint
        {
            get { return entryPoint; }
        }

        /// <summary>
        /// Indicates whether the emitted assembly can be garbage collected.
        /// </summary>
        public bool IsUncollectible
        {
            get { return isUncollectible; }
        }
    }
}
