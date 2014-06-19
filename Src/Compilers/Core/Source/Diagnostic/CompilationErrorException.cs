using System;
using System.Runtime.Serialization;
using Roslyn.Compilers.Common;

namespace Roslyn.Compilers
{
    /// <summary>
    /// An exception thrown when the compilation stage of interactive execution produces compilation errors.
    /// </summary>
    [Serializable]
    public class CompilationErrorException : Exception, ISerializable
    {
        private readonly ReadOnlyArray<CommonDiagnostic> diagnostics;

        internal CompilationErrorException(string message, ReadOnlyArray<CommonDiagnostic> diagnostics)
            : base(message)
        {
            this.diagnostics = diagnostics;
        }

        protected CompilationErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            this.diagnostics = (ReadOnlyArray<CommonDiagnostic>)info.GetValue("diagnostics", typeof(ReadOnlyArray<CommonDiagnostic>));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("diagnostics", diagnostics);
        }

        /// <summary>
        /// The list of diagnostics produced by compilation.
        /// </summary>
        public ReadOnlyArray<CommonDiagnostic> Diagnostics
        {
            get { return diagnostics; }
        }
    }
}
