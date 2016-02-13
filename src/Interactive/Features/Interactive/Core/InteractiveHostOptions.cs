// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public InteractiveHostOptions(
            string initializationFile = null,
            CultureInfo culture = null)
        {
            InitializationFile = initializationFile;
            Culture = culture ?? CultureInfo.CurrentUICulture;
        }

        public InteractiveHostOptions WithInitializationFile(string initializationFile)
        {
            if (InitializationFile == initializationFile)
            {
                return this;
            }

            return new InteractiveHostOptions(initializationFile, Culture);
        }

        public InteractiveHostOptions WithCulture(CultureInfo culture)
        {
            if (Culture == culture)
            {
                return this;
            }

            return new InteractiveHostOptions(InitializationFile, culture);
        }
    }
}
