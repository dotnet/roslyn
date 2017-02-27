// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class LanguageVersionExtensions
    {
        /// <summary>
        /// Displays the version number in the format expected on the command-line (/langver flag).
        /// For instance, "6", "7", "7.1", "latest".
        /// </summary>
        internal static string Display(this LanguageVersion version)
        {
            switch (version)
            {
                case LanguageVersion.CSharp1:
                    return "1";
                case LanguageVersion.CSharp2:
                    return "2";
                case LanguageVersion.CSharp3:
                    return "3";
                case LanguageVersion.CSharp4:
                    return "4";
                case LanguageVersion.CSharp5:
                    return "5";
                case LanguageVersion.CSharp6:
                    return "6";
                case LanguageVersion.CSharp7:
                    return "7";
                case LanguageVersion.Default:
                    return "default";
                case LanguageVersion.Latest:
                    return "latest";
                default:
                    throw ExceptionUtilities.UnexpectedValue(version);
            }
        }
    }
}
