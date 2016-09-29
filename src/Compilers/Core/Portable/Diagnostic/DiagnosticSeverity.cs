// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes how severe a diagnostic is.
    /// </summary>
    public enum DiagnosticSeverity
    {
        /// <summary>
        /// Something that is an issue, as determined by some authority,
        /// but is not surfaced through normal means.
        /// There may be different mechanisms that act on these issues.
        /// </summary>
        Hidden = 0,

        /// <summary>
        /// Information that does not indicate a problem (i.e. not prescriptive).
        /// </summary>
        Info = 1,

        /// <summary>
        /// Something suspicious but allowed.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Something not allowed by the rules of the language or other authority.
        /// </summary>
        Error = 3,
    }

    /// <summary>
    /// Values for severity that are used internally by the compiler but are not exposed.
    /// </summary>
    internal static class InternalDiagnosticSeverity
    {
        /// <summary>
        /// An unknown severity diagnostic is something whose severity has not yet been determined.
        /// </summary>
        public const DiagnosticSeverity Unknown = (DiagnosticSeverity)InternalErrorCode.Unknown;

        /// <summary>
        /// If an unknown diagnostic is resolved and found to be unnecessary then it is 
        /// treated as a "Void" diagnostic
        /// </summary>
        public const DiagnosticSeverity Void = (DiagnosticSeverity)InternalErrorCode.Void;
    }

    /// <summary>
    /// Values for ErrorCode/ERRID that are used internally by the compiler but are not exposed.
    /// </summary>
    internal static class InternalErrorCode
    {
        /// <summary>
        /// The code has yet to be determined.
        /// </summary>
        public const int Unknown = -1;

        /// <summary>
        /// The code was lazily determined and does not need to be reported.
        /// </summary>
        public const int Void = -2;
    }
}
