using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Cci
{
    /// <summary>
    /// A single CLR IL operation.
    /// </summary>
    internal interface IOperation
    {
        /// <summary>
        /// The actual value of the operation code
        /// </summary>
        Roslyn.Compilers.CodeGen.ILOpCode OperationCode { get; }

        /// <summary>
        /// The offset from the start of the operation stream of a method
        /// </summary>
        uint Offset { get; }

        /// <summary>
        /// The location that corresponds to this instruction.
        /// </summary>
        ILocation Location { get; }

        /// <summary>
        /// Immediate data such as a string, the address of a branch target, or a metadata reference, such as a Field
        /// </summary>
        object/*?*/ Value
        {
            get;
        }
    }
}