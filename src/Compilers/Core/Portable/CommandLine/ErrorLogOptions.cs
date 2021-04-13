// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Options controlling the generation of a SARIF log file containing compilation or analyzer diagnostics.
    /// </summary>
    public sealed class ErrorLogOptions
    {
        /// <summary>
        /// Absolute path of the error log file.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Version of the SARIF format used in the error log.
        /// </summary>
        public SarifVersion SarifVersion { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorLogOptions"/> class.
        /// </summary>
        /// <param name="path">Absolute path of the error log file.</param>
        /// <param name="sarifVersion">Version of the SARIF format used in the error log.</param>
        public ErrorLogOptions(string path, SarifVersion sarifVersion)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
            SarifVersion = sarifVersion;
        }
    }
}
