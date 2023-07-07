// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
{
    /// <summary>
    /// Exception region affected by a managed update.
    /// </summary>
    [DataContract]
    internal readonly struct ManagedExceptionRegionUpdate
    {
        /// <summary>
        /// Creates an ExceptionRegionUpdate.
        /// </summary>
        /// <param name="method">Method information before the change was made.</param>
        /// <param name="delta">Total of lines modified after the update.</param>
        /// <param name="newSpan">Updated text span for the active statement.</param>
        public ManagedExceptionRegionUpdate(
            ManagedModuleMethodId method,
            int delta,
            SourceSpan newSpan)
        {
            Method = method;
            Delta = delta;
            NewSpan = newSpan;
        }

        /// <summary>
        /// Method ID. It has the token for the method that contains the exception region
        /// and the version when the change was made.
        /// </summary>
        [DataMember(Name = "method")]
        public ManagedModuleMethodId Method { get; }

        /// <summary>
        /// The delta is the total of lines modified after the update. This value is inverse:
        /// 
        ///   OldSpan = NewSpan + Delta
        ///   NewSpan = OldSpan - Delta
        ///
        /// For example, if 2 new lines were added preceding the exception region, this value will be -2.
        /// </summary>
        [DataMember(Name = "delta")]
        public int Delta { get; }

        /// <summary>
        /// Specifies where the exception region starts and ends after the update. This value is 0-based.
        /// An exception region value generally corresponds to a catch { } block source span before any update is made.
        /// </summary>
        /// <remarks>
        /// This value will take into account any lines affected by the update, so we can correctly track the new exception regions
        /// when remapping the instruction pointer.
        /// The new span is expected to be: [PreviousExceptionRegionSpan] + [Delta of updated lines].
        /// </remarks>
        [DataMember(Name = "newSpan")]
        public SourceSpan NewSpan { get; }
    }
}
