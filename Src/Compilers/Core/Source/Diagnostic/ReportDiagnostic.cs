// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Describes how to report a warning diagnostic.
    /// </summary>
    public enum ReportDiagnostic
    {
        /// <summary>
        /// Report a diagnostic by default.
        /// </summary>
        Default,

        /// <summary>
        /// Report a diagnostic as an error.  
        /// </summary>
        Error,

        /// <summary>
        /// Report a diagnostic as a warning even though /warnaserror is specified.
        /// </summary>
        Warn,

        /// <summary>
        /// Report a diagnostic as an info.
        /// </summary>
        Info,

        /// <summary>
        /// Report a diagnostic as hidden.
        /// </summary>
        Hidden,

        /// <summary>
        /// Suppress a diagnostic.
        /// </summary>
        Suppress
    }
}