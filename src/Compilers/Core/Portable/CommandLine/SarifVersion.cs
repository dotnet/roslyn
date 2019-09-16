// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Specifies the version of the SARIF log file to produce.
    /// </summary>
    public enum SarifVersion
    {
        /// <summary>
        /// The original, non-standardized version of the SARIF format.
        /// </summary>
        Sarif1 = 1,

        /// <summary>
        /// The first standardized version of the SARIF format.
        /// </summary>
        Sarif2 = 2,

        /// <summary>
        /// The default SARIF version, which is v1.0.0 for compatibility with
        /// previous versions of the compiler.
        /// </summary>
        Default = Sarif1,

        /// <summary>
        /// The latest supported SARIF version.
        /// </summary>
        Latest = int.MaxValue
    }

    public static class SarifVersionFacts
    {
        /// <summary>
        /// Try to parse the SARIF log file version from a string.
        /// </summary>
        public static bool TryParse(string version, out SarifVersion result)
        {
            if (version == null)
            {
                result = SarifVersion.Default;
                return true;
            }

            switch (CaseInsensitiveComparison.ToLower(version))
            {
                case "default":
                    result = SarifVersion.Default;
                    return true;

                case "latest":
                    result = SarifVersion.Latest;
                    return true;

                case "1":
                case "1.0":
                    result = SarifVersion.Sarif1;
                    return true;

                case "2":
                case "2.1":
                    result = SarifVersion.Sarif2;
                    return true;

                default:
                    result = SarifVersion.Default;
                    return false;
            }
        }
    }
}