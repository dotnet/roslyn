// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// <remarks> 
        /// Features: LINQ.
        /// </remarks>
        CSharp3 = 3,

        /// <summary>
        /// C# language version 4.0.
        /// </summary>
        /// <remarks> 
        /// Features: dynamic.
        /// </remarks>
        CSharp4 = 4,

        /// <summary>
        /// C# language version 5.0.
        /// </summary>
        /// <remarks> 
        /// Features: async.
        /// </remarks>
        CSharp5 = 5,

        /// <summary> 
        /// C# language version 6.0.
        /// </summary>
        /// <remarks>
        /// <para>Features:</para>
        /// <list type="bullet">
        /// <item><description>Using of a static class</description></item> 
        /// <item><description>Auto-property initializers</description></item> 
        /// <item><description>Expression-bodied methods and properties</description></item> 
        /// <item><description>Null-propagating operator ?.</description></item> 
        /// <item><description>Exception filters</description></item> 
        /// </list> 
        /// </remarks> 
        CSharp6 = 6,
        /// <summary>
        /// C# language version 7.
        /// </summary>
        CSharp7 = 7,
    }

    internal static partial class LanguageVersionExtensions
    {
        internal static bool IsValid(this LanguageVersion value)
        {
            return value >= LanguageVersion.CSharp1 && value <= LanguageVersion.CSharp7;
        }

        internal static object Localize(this LanguageVersion value)
        {
            return (int)value;
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
                case LanguageVersion.CSharp6:
                    return ErrorCode.ERR_FeatureNotAvailableInVersion6;
                case LanguageVersion.CSharp7:
                    return ErrorCode.ERR_FeatureNotAvailableInVersion7;
                default:
                    throw ExceptionUtilities.UnexpectedValue(version);
            }
        }
    }
}
