// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Specifies the language version.
    /// </summary>
    public enum LanguageVersion
    {
        /// <summary>
        /// C# language version 1.0.
        /// </summary>
        CSharp1 = 1,

        /// <summary>
        /// C# language version 2.0.
        /// </summary>
        CSharp2 = 2,

        /// <summary>
        /// C# language version 3.0.
        /// </summary>
        /// <remarks> Features: LINQ.
        /// </remarks>
        CSharp3 = 3,

        /// <summary>
        /// C# language version 4.0.
        /// </summary>
        /// <remarks> Features: dynamic.
        /// </remarks>
        CSharp4 = 4,

        /// <summary>
        /// C# language version 5.0.
        /// </summary>
        /// <remarks> Features: async.
        /// </remarks>
        CSharp5 = 5,

        /// <summary> 
        /// C# language version 6.0.
        /// </summary>
        /// <remarks>
        /// Features: 
        /// Using of a static class.
        /// Exception filters.
        /// Autoprop initializers.
        /// </remarks>
        CSharp6 = 6,

        /// <summary>
        /// C# language version 6.0 + experimental features. 
        /// </summary>
        Experimental = 7,
    }

    internal static partial class LanguageVersionExtensions
    {
        internal static bool IsValid(this LanguageVersion value)
        {
            return value >= LanguageVersion.CSharp1 && value <= LanguageVersion.Experimental;
        }

        internal static ErrorCode GetErrorCode(this LanguageVersion version)
        {
            switch (version)
            {
                case LanguageVersion.CSharp1:
                    return ErrorCode.ERR_FeatureNotAvailableInVersion1;
                case LanguageVersion.CSharp2:
                    return ErrorCode.ERR_FeatureNotAvailableInVersion2;
                case LanguageVersion.CSharp3:
                    return ErrorCode.ERR_FeatureNotAvailableInVersion3;
                case LanguageVersion.CSharp4:
                    return ErrorCode.ERR_FeatureNotAvailableInVersion4;
                case LanguageVersion.CSharp5:
                    return ErrorCode.ERR_FeatureNotAvailableInVersion5;
                default:
                    throw ExceptionUtilities.UnexpectedValue(version);
            }
        }
    }
}