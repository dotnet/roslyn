// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
{
    /// <summary>
    /// Diagnostic information about a particular edit made through hot reload.
    /// </summary>
    [DataContract]
    internal readonly struct ManagedHotReloadDiagnostic
    {
        /// <summary>
        /// Creates a new <see cref="ManagedHotReloadDiagnostic"/> for an edit made by the user.
        /// </summary>
        /// <param name="id">Diagnostic information identifier.</param>
        /// <param name="message">User message.</param>
        /// <param name="severity">Severity of the edit, whether it's an error or a warning.</param>
        /// <param name="filePath">File path for the target edit.</param>
        /// <param name="span">Source span of the edit.</param>
        public ManagedHotReloadDiagnostic(
            string id,
            string message,
            ManagedHotReloadDiagnosticSeverity severity,
            string filePath,
            SourceSpan span)
        {
            Id = id;
            Message = message;
            Severity = severity;
            FilePath = filePath;
            Span = span;
        }

        /// <summary>
        /// Diagnostic information identifier.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; }

        /// <summary>
        /// User message which will be displayed for the edit.
        /// </summary>
        [DataMember(Name = "message")]
        public string Message { get; }

        /// <summary>
        /// Severity of the diagnostic information.
        /// </summary>
        [DataMember(Name = "severity")]
        public ManagedHotReloadDiagnosticSeverity Severity { get; }

        /// <summary>
        /// File path where the edit was made.
        /// </summary>
        [DataMember(Name = "filePath")]
        public string FilePath { get; }

        /// <summary>
        /// Source span for the edit.
        /// </summary>
        [DataMember(Name = "span")]
        public SourceSpan Span { get; }
    }
}
