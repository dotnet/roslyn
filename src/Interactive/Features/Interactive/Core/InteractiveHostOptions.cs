// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Settings that affect InteractiveHost process and initialization.
    /// </summary>
    internal sealed class InteractiveHostOptions
    {
        /// <summary>
        /// Optional path to the .rsp file to process when initializing context of the process.
        /// </summary>
        public string InitializationFile { get; }

        /// <summary>
        /// Host culture used for localization of doc comments, errors.
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Host process bitness.
        /// </summary>
        public bool Is64Bit { get; }

        public InteractiveHostOptions(
            string initializationFile = null,
            CultureInfo culture = null,
            bool is64Bit = false)
        {
            InitializationFile = initializationFile;
            Culture = culture ?? CultureInfo.CurrentUICulture;
            Is64Bit = is64Bit;
        }
    }
}
