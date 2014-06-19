namespace Roslyn.Compilers.CSharp
{
    partial class Binder
    {
        /// <summary>
        /// Expression lvalue and rvalue requirements.
        /// </summary>
        private enum BindValueKind
        {
            /// <summary>
            /// Expression is the RHS of an assignment operation.
            /// </summary>
            RValue,

            /// <summary>
            /// Expression is the LHS of a simple assignment operation.
            /// </summary>
            Assignment,

            /// <summary>
            /// Expression is the operand of an increment
            /// or decrement operation.
            /// </summary>
            IncrementDecrement,

            /// <summary>
            /// Expression is the LHS of a compound assignment
            /// operation (such as +=).
            /// </summary>
            CompoundAssignment,

            /// <summary>
            /// Expression is an out parameter.
            /// </summary>
            OutParameter,
        }
    }
}