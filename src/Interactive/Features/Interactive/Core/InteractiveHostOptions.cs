// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Interactive
{
    /// <summary>
    /// Settings that affect InteractiveHost process and initialization.
    /// </summary>
    public sealed class InteractiveHostOptions
    {
        public static readonly InteractiveHostOptions Default = new InteractiveHostOptions(null);

        /// <summary>
        /// Optional path to the .rsp file to process when initializing context of the process.
        /// </summary>
        public string InitializationFile { get; }

        private InteractiveHostOptions(string initializationFile)
        {
            this.InitializationFile = initializationFile;
        }

        public InteractiveHostOptions WithInitializationFile(string initializationFile)
        {
            if (this.InitializationFile == initializationFile)
            {
                return this;
            }

            return new InteractiveHostOptions(initializationFile);
        }
    }
}
