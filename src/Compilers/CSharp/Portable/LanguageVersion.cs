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
        /// The default language version, which is the latest major supported version.
        /// </summary>
        Default = 0,

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

        /// <summary>
        /// The latest version of the language supported.
        /// </summary>
        Latest = int.MaxValue,
    }

    internal static partial class LanguageVersionExtensionsInternal
    {
        internal static bool IsValid(this LanguageVersion value)
        {
            return value >= LanguageVersion.CSharp1 && value <= LanguageVersion.CSharp7;
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

    /// <summary>
    /// This type is attached to diagnostics for required language version and should only be used
    /// on such diagnostics, as they are scrapped out by <see cref="CSharpCompilation.GetRequiredLanguageVersion"/>.
    /// </summary>
    internal class RequiredLanguageVersion : IMessageSerializable
    {
        internal LanguageVersion Version { get; }

        internal RequiredLanguageVersion(LanguageVersion version)
        {
            Version = version;
        }

        public override string ToString()
        {
            return Version.ToDisplayString();
        }
    }

    public static class LanguageVersionExtensions
    {
        /// <summary>
        /// Displays the version number in the format expected on the command-line (/langver flag).
        /// For instance, "6", "7", "7.1", "latest".
        /// </summary>
        public static string ToDisplayString(this LanguageVersion version)
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

        public static bool TryParseDisplayString(this LanguageVersion _, string newVersion, out LanguageVersion result)
        {
            switch (newVersion)
            {
                case "1":
                    result = LanguageVersion.CSharp1;
                    break;
                case "2":
                    result = LanguageVersion.CSharp2;
                    break;
                case "3":
                    result = LanguageVersion.CSharp3;
                    break;
                case "4":
                    result = LanguageVersion.CSharp4;
                    break;
                case "5":
                    result = LanguageVersion.CSharp5;
                    break;
                case "6":
                    result = LanguageVersion.CSharp6;
                    break;
                case "7":
                    result = LanguageVersion.CSharp7;
                    break;
                case "default":
                    result = LanguageVersion.Default;
                    break;
                case "latest":
                    result = LanguageVersion.Latest;
                    break;
                default:
                    result = LanguageVersion.Default;
                    return false;
            }
            return true;
        }

        public static LanguageVersion MapSpecifiedToEffectiveVersion(this LanguageVersion version)
        {
            switch (version)
            {
                case LanguageVersion.Latest:
                case LanguageVersion.Default:
                    return LanguageVersion.CSharp7;
                default:
                    return version;
            }
        }
    }
}