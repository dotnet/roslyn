// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an identity of an assembly as defined by CLI metadata specification.
    /// </summary>
    /// <remarks>
    /// May represent assembly definition or assembly reference identity.
    /// </remarks>
    partial class AssemblyIdentity
    {
        /// <summary>
        /// Returns the display name of the assembly identity.
        /// </summary>
        /// <param name="fullKey">True if the full public key should be included in the name. Otherwise public key token is used.</param>
        /// <returns>The display name.</returns>
        /// <remarks>
        /// Characters ',', '=', '"', '\'', '\' occuring in the simple name are escaped by backslash in the display name.
        /// Any character '\t' is replaced by two characters '\' and 't',
        /// Any character '\n' is replaced by two characters '\' and 'n',
        /// Any character '\r' is replaced by two characters '\' and 'r',
        /// The assembly name in the display name is enclosed in double quotes if it starts or ends with 
        /// a whitespace character (' ', '\t', '\r', '\n').
        /// </remarks>
        public string GetDisplayName(bool fullKey = false)
        {
            if (fullKey)
            {
                return BuildDisplayName(fullKey: true);
            }

            if (lazyDisplayName == null)
            {
                lazyDisplayName = BuildDisplayName(fullKey: false);
            }

            return lazyDisplayName;
        }

        /// <summary>
        /// Returns the display name of the current instance.
        /// </summary>
        public override string ToString()
        {
            return GetDisplayName(fullKey: false);
        }

        private string BuildDisplayName(bool fullKey)
        {
            PooledStringBuilder pooledBuilder = PooledStringBuilder.GetInstance();
            var sb = pooledBuilder.Builder;
            EscapeName(sb, Name);

            sb.Append(", Version=");
            sb.Append(version.Major);
            sb.Append(".");
            sb.Append(version.Minor);
            sb.Append(".");
            sb.Append(version.Build);
            sb.Append(".");
            sb.Append(version.Revision);

            sb.Append(", Culture=");
            sb.Append(cultureName.Length != 0 ? cultureName : "neutral");

            if (fullKey && HasPublicKey)
            {
                sb.Append(", PublicKey=");
                AppendKey(sb, publicKey);
            }
            else
            {
                sb.Append(", PublicKeyToken=");
                if (PublicKeyToken.Length > 0)
                {
                    AppendKey(sb, PublicKeyToken);
                }
                else
                {
                    sb.Append("null");
                }
            }

            if (IsRetargetable)
            {
                sb.Append(", Retargetable=Yes");
            }

            switch (contentType)
            {
                case AssemblyContentType.Default:
                    break;

                case AssemblyContentType.WindowsRuntime:
                    sb.Append(", ContentType=WindowsRuntime");
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(contentType);
            }

            string result = sb.ToString();
            pooledBuilder.Free();
            return result;
        }

        private static void AppendKey(StringBuilder sb, ImmutableArray<byte> key)
        {
            foreach (byte b in key)
            {
                sb.Append(b.ToString("x2"));
            }
        }

        private string GetDebuggerDisplay()
        {
            return GetDisplayName(fullKey: true);
        }

        public static bool TryParseDisplayName(string displayName, out AssemblyIdentity identity)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException("displayName");
            }

            AssemblyIdentityParts parts;
            return TryParseDisplayName(displayName, out identity, out parts);
        }

        /// <summary>
        /// Parses display name filling defaults for any basic properties that are missing.
        /// </summary>
        /// <param name="displayName">Display name.</param>
        /// <param name="identity">A full assembly identity.</param>
        /// <param name="parts">
        /// Parts of the assembly identity that were specified in the display name, 
        /// or 0 if the parsing failed.
        /// </param>
        /// <returns>True if display name parsed correctly.</returns>
        /// <remarks>
        /// The simple name has to be non-empty.
        /// A partially specified version might be missing build and/or revision number. The default value for these is 65535.
        /// The default culture is neutral (<see cref="CultureName"/> is <see cref="String.Empty"/>.
        /// If neither public key nor token is specified the identity is considered weak.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public static bool TryParseDisplayName(string displayName, out AssemblyIdentity identity, out AssemblyIdentityParts parts)
        {
            // see ndp\clr\src\Binder\TextualIdentityParser.cpp, ndp\clr\src\Binder\StringLexer.cpp

            identity = null;
            parts = 0;

            if (displayName == null)
            {
                throw new ArgumentNullException("displayName");
            }

            if (displayName.IndexOf('\0') >= 0)
            {
                return false;
            }

            int position = 0;
            string simpleName = TryParseNameToken(displayName, ',', ref position);
            if (simpleName == null)
            {
                return false;
            }

            var parsedParts = AssemblyIdentityParts.Name;
            var seen = AssemblyIdentityParts.Name;

            Version version = null;
            string culture = null;
            bool isRetargetable = false;
            var contentType = AssemblyContentType.Default;
            var publicKey = default(ImmutableArray<byte>);
            var publicKeyToken = default(ImmutableArray<byte>);

            while (position < displayName.Length)
            {
                string propertyName = TryParseNameToken(displayName, '=', ref position);
                if (propertyName == null)
                {
                    return false;
                }

                string propertyValue = TryParseNameToken(displayName, ',', ref position);
                if (propertyValue == null)
                {
                    return false;
                }

                if (string.Equals(propertyName, "Version", StringComparison.OrdinalIgnoreCase))
                {
                    if ((seen & AssemblyIdentityParts.Version) != 0)
                    {
                        return false;
                    }

                    seen |= AssemblyIdentityParts.Version;

                    if (propertyValue == "*")
                    {
                        continue;
                    }

                    ulong versionLong;
                    AssemblyIdentityParts versionParts;
                    if (!TryParseVersion(propertyValue, out versionLong, out versionParts))
                    {
                        return false;
                    }

                    version = ToVersion(versionLong);
                    parsedParts |= versionParts;
                }
                else if (string.Equals(propertyName, "Culture", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(propertyName, "Language", StringComparison.OrdinalIgnoreCase))
                {
                    if ((seen & AssemblyIdentityParts.Culture) != 0)
                    {
                        return false;
                    }

                    seen |= AssemblyIdentityParts.Culture;

                    if (propertyValue == "*")
                    {
                        continue;
                    }

                    culture = string.Equals(propertyValue, "neutral", StringComparison.OrdinalIgnoreCase) ? null : propertyValue;
                    parsedParts |= AssemblyIdentityParts.Culture;
                }
                else if (string.Equals(propertyName, "PublicKey", StringComparison.OrdinalIgnoreCase))
                {
                    if ((seen & AssemblyIdentityParts.PublicKey) != 0)
                    {
                        return false;
                    }

                    seen |= AssemblyIdentityParts.PublicKey;

                    if (propertyValue == "*")
                    {
                        continue;
                    }

                    ImmutableArray<byte> value = ParseKey(propertyValue);
                    if (value.Length == 0)
                    {
                        return false;
                    }

                    publicKey = value;
                    parsedParts |= AssemblyIdentityParts.PublicKey;
                }
                else if (string.Equals(propertyName, "PublicKeyToken", StringComparison.OrdinalIgnoreCase))
                {
                    if ((seen & AssemblyIdentityParts.PublicKeyToken) != 0)
                    {
                        return false;
                    }

                    seen |= AssemblyIdentityParts.PublicKeyToken;

                    if (propertyValue == "*")
                    {
                        continue;
                    }

                    ImmutableArray<byte> value;
                    if (string.Equals(propertyValue, "null", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(propertyValue, "neutral", StringComparison.OrdinalIgnoreCase))
                    {
                        value = ImmutableArray.Create<byte>();
                    }
                    else
                    {
                        value = ParseKey(propertyValue);
                        if (value.Length != PublicKeyTokenSize)
                        {
                            return false;
                        }
                    }

                    publicKeyToken = value;
                    parsedParts |= AssemblyIdentityParts.PublicKeyToken;
                }
                else if (string.Equals(propertyName, "Retargetable", StringComparison.OrdinalIgnoreCase))
                {
                    if ((seen & AssemblyIdentityParts.Retargetability) != 0)
                    {
                        return false;
                    }

                    seen |= AssemblyIdentityParts.Retargetability;

                    if (propertyValue == "*")
                    {
                        continue;
                    }

                    if (string.Equals(propertyValue, "Yes", StringComparison.OrdinalIgnoreCase))
                    {
                        isRetargetable = true;
                    }
                    else if (string.Equals(propertyValue, "No", StringComparison.OrdinalIgnoreCase))
                    {
                        isRetargetable = false;
                    }
                    else
                    {
                        return false;
                    }

                    parsedParts |= AssemblyIdentityParts.Retargetability;
                }
                else if (string.Equals(propertyName, "ContentType", StringComparison.OrdinalIgnoreCase))
                {
                    if ((seen & AssemblyIdentityParts.ContentType) != 0)
                    {
                        return false;
                    }

                    seen |= AssemblyIdentityParts.ContentType;

                    if (propertyValue == "*")
                    {
                        continue;
                    }

                    if (string.Equals(propertyValue, "WindowsRuntime", StringComparison.OrdinalIgnoreCase))
                    {
                        contentType = AssemblyContentType.WindowsRuntime;
                    }
                    else
                    {
                        return false;
                    }

                    parsedParts |= AssemblyIdentityParts.ContentType;
                }
                else
                {
                    parsedParts |= AssemblyIdentityParts.Unknown;
                }
            }

            // incompatible values:
            if (isRetargetable && contentType == AssemblyContentType.WindowsRuntime)
            {
                return false;
            }

            bool hasPublicKey = !publicKey.IsDefault;
            bool hasPublicKeyToken = !publicKeyToken.IsDefault;

            identity = new AssemblyIdentity(simpleName, version, culture, hasPublicKey ? publicKey : publicKeyToken, hasPublicKey, isRetargetable, contentType);

            if (hasPublicKey && hasPublicKeyToken && !identity.PublicKeyToken.SequenceEqual(publicKeyToken))
            {
                identity = null;
                return false;
            }

            parts = parsedParts;
            return true;
        }

        private static string TryParseNameToken(string displayName, char terminator, ref int position)
        {
            Debug.Assert(displayName.IndexOf('\0') == -1);

            int i = position;

            // skip leading whitespace:
            while (i < displayName.Length && IsWhiteSpace(displayName[i]))
            {
                i++;
            }

            if (i == displayName.Length)
            {
                return null;
            }

            char quote;
            if (IsQuote(displayName[i]))
            {
                quote = displayName[i++];
            }
            else
            {
                quote = '\0';
            }

            int valueStart = i;
            int valueEnd = displayName.Length;
            int escapeCount = 0;

            while (i < displayName.Length)
            {
                char c = displayName[i];
                if (c == '\\')
                {
                    escapeCount++;
                    i += 2;
                    continue;
                }

                if (quote == 0)
                {
                    if (c == terminator)
                    {
                        int j = i - 1;
                        while (j >= valueStart && IsWhiteSpace(displayName[j]))
                        {
                            j--;
                        }

                        valueEnd = j + 1;
                        break;
                    }

                    if (IsQuote(c) || IsNameTokenTerminator(c))
                    {
                        return null;
                    }
                }
                else if (c == quote)
                {
                    valueEnd = i;
                    i++;

                    // skip any whitespace following the quote
                    while (i < displayName.Length && IsWhiteSpace(displayName[i]))
                    {
                        i++;
                    }

                    if (i < displayName.Length && displayName[i] != terminator)
                    {
                        return null;
                    }

                    break;
                }

                i++;
            }

            Debug.Assert(i >= displayName.Length || IsNameTokenTerminator(displayName[i]));
            position = (i >= displayName.Length) ? displayName.Length : i + 1;

            // empty
            if (valueEnd == valueStart)
            {
                return null;
            }

            if (escapeCount == 0)
            {
                return displayName.Substring(valueStart, valueEnd - valueStart);
            }
            else
            {
                return Unescape(displayName, valueStart, valueEnd);
            }
        }

        private static bool IsNameTokenTerminator(char c)
        {
            return c == '=' || c == ',';
        }

        private static bool IsQuote(char c)
        {
            return c == '"' || c == '\'';
        }

        internal static Version ToVersion(ulong version)
        {
            return new Version(
                unchecked((ushort)(version >> 48)),
                unchecked((ushort)(version >> 32)),
                unchecked((ushort)(version >> 16)),
                unchecked((ushort)version));
        }

        // internal for testing
        // Parses version format: 
        //   [version-part]{[.][version-part], 3}
        // Where version part is
        //   [*]|[0-9]*
        // The number of dots in the version determines the present parts, i.e.
        //   "1..2" parses as "1.0.2.0" with Major, Minor and Build parts.
        //   "1.*" parses as "1.0.0.0" with Major and Minor parts.
        internal static bool TryParseVersion(string str, out ulong result, out AssemblyIdentityParts parts)
        {
            Debug.Assert(str.Length > 0);
            Debug.Assert(str.IndexOf('\0') < 0);

            const int MaxVersionParts = 4;
            const int BitsPerVersionPart = 16;

            parts = 0;
            result = 0;
            int partOffset = BitsPerVersionPart * (MaxVersionParts - 1);
            int partIndex = 0;
            int partValue = 0;
            bool partHasValue = false;
            bool partHasWildcard = false;

            int i = 0;
            while (true)
            {
                char c = (i < str.Length) ? str[i++] : '\0';

                if (c == '.' || c == 0)
                {
                    if (partIndex == MaxVersionParts || partHasValue && partHasWildcard)
                    {
                        return false;
                    }

                    result |= ((ulong)partValue) << partOffset;

                    if (partHasValue || partHasWildcard)
                    {
                        parts |= (AssemblyIdentityParts)((int)AssemblyIdentityParts.VersionMajor << partIndex);
                    }

                    if (c == 0)
                    {
                        return true;
                    }

                    // next part:
                    partValue = 0;
                    partOffset -= BitsPerVersionPart;
                    partIndex++;
                    partHasWildcard = partHasValue = false;
                }
                else if (c >= '0' && c <= '9')
                {
                    partHasValue = true;
                    partValue = partValue * 10 + c - '0';
                    if (partValue > ushort.MaxValue)
                    {
                        return false;
                    }
                }
                else if (c == '*')
                {
                    partHasWildcard = true;
                }
                else
                {
                    return false;
                }
            }
        }

        private static ImmutableArray<byte> ParseKey(string value)
        {
            byte[] result = new byte[value.Length / 2];
            for (int i = 0; i < result.Length; i++)
            {
                int hi = HexValue(value[i * 2]);
                int lo = HexValue(value[i * 2 + 1]);

                if (hi < 0 || lo < 0)
                {
                    return ImmutableArray.Create<byte>();
                }

                result[i] = (byte)((hi << 4) | lo);
            }

            return result.AsImmutable();
        }

        internal static int HexValue(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return c - '0';
            }

            if (c >= 'a' && c <= 'f')
            {
                return c - 'a' + 10;
            }

            if (c >= 'A' && c <= 'F')
            {
                return c - 'A' + 10;
            }

            return -1;
        }

        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        private static void EscapeName(StringBuilder result, string name)
        {
            bool quoted = false;
            if (IsWhiteSpace(name[0]) || IsWhiteSpace(name[name.Length - 1]))
            {
                result.Append('"');
                quoted = true;
            }

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                switch (c)
                {
                    case ',':
                    case '=':
                    case '\\':
                    case '"':
                    case '\'':
                        result.Append('\\');
                        result.Append(c);
                        break;

                    case '\t':
                        result.Append("\\t");
                        break;

                    case '\r':
                        result.Append("\\r");
                        break;

                    case '\n':
                        result.Append("\\n");
                        break;

                    default:
                        result.Append(c);
                        break;
                }
            }

            if (quoted)
            {
                result.Append('"');
            }
        }

        private static bool CanBeEscaped(char c)
        {
            switch (c)
            {
                case ',':
                case '=':
                case '\\':
                case '/':
                case '"':
                case '\'':
                case 't':
                case 'n':
                case 'r':
                case 'u':
                    return true;

                default:
                    return false;
            }
        }

        private static string Unescape(string str, int start, int end)
        {
            var sb = PooledStringBuilder.GetInstance();

            int i = start;
            while (i < end)
            {
                char c = str[i++];
                if (c == '\\')
                {
                    Debug.Assert(CanBeEscaped(c));
                    if (!Unescape(sb.Builder, str, ref i))
                    {
                        return null;
                    }
                }
                else
                {
                    sb.Builder.Append(c);
                }
            }

            return sb.ToStringAndFree();
        }

        private static bool Unescape(StringBuilder sb, string str, ref int i)
        {
            if (i == str.Length)
            {
                return false;
            }

            char c = str[i++];
            switch (c)
            {
                case ',':
                case '=':
                case '\\':
                case '/':
                case '"':
                case '\'':
                    sb.Append(c);
                    return true;

                case 't':
                    sb.Append("\t");
                    return true;

                case 'n':
                    sb.Append("\n");
                    return true;

                case 'r':
                    sb.Append("\r");
                    return true;

                case 'u':
                    int semicolon = str.IndexOf(';', i);
                    if (semicolon == -1)
                    {
                        return false;
                    }

                    try
                    {
                        int codepoint = Convert.ToInt32(str.Substring(i, semicolon - i), 16);

                        // \0 is not valid in an assembly name
                        if (codepoint == 0)
                        {
                            return false;
                        }

                        sb.Append(char.ConvertFromUtf32(codepoint));
                    }
                    catch
                    {
                        return false;
                    }

                    i = semicolon + 1;
                    return true;

                default:
                    return false;
            }
        }
    }
}