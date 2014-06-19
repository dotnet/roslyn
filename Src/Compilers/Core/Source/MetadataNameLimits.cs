using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class MetadataNameLimits
    {
        /// <summary>
        /// This is the maximum length of a type or member name in metadata, assuming
        /// the name is in UTF-8 format and not (yet) null-terminated.
        /// </summary>
        internal const int MAX_UTF8_NAME_LENGTH = 1024 - 1; //MAX_CLASS_NAME = 1024  in dev10

        /// <summary>
        /// This is the maximum length of a path in metadata, assuming the path is in UTF-8
        /// format and not (yet) null-terminated.
        /// </summary>
        internal const int MAX_UTF8_PATH_LENGTH = 260 - 1; //MAX_PACKAGE_NAME = 1024  in dev10

        /// <summary>
        /// This is the same as the default UTF-8 encoding except that the decoder fallback
        /// behavior has been changed.  Instead of replacing unknown byte sequences with question
        /// marks, this encoding omits them completely.  Since the only unknown byte sequences
        /// we expect to see are partial characters on the truncation boundary (since we are
        /// doing the encoding ourselves), this has the effect of removing partial characters.
        /// 
        /// For example, suppose you begin with the string "abc\uFFFF".  In UTF-8, this is encoded
        /// in six bytes (without the null terminator) - one for each of 'a', 'b', and 'c' and 
        /// three for '\uFFFF'.  If you truncate to 5 bytes, then you end up with 'a', 'b', 'c',
        /// and the first two bytes of '\uFFFF'.  The decoder replacement fallback simply deletes
        /// these unrecognized bytes, leaving "abc".
        /// </summary>
        private static readonly Encoding Utf8Encoding =
            Encoding.GetEncoding(Encoding.UTF8.CodePage, Encoding.UTF8.EncoderFallback, new DecoderReplacementFallback(string.Empty));

        /// <summary>
        /// Returns true if the supplied name is longer than MAX_CLASS_NAME (See CLI Part II, section 22)
        /// when encoded as a UTF-8 string.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        internal static bool ExceedsMaxClassName(string fullName)
        {
            return IsTooLong(MAX_UTF8_NAME_LENGTH, fullName);
        }
        /// <summary>
        /// Returns true if the supplied name is longer than MAX_PATH_NAME (See CLI Part II, section 22)
        /// when encoded as a UTF-8 string.
        /// </summary>
        /// <param name="fullName"></param>
        /// <returns></returns>
        internal static bool ExceedsMaxPathName(string fullName)
        {
            return IsTooLong(MAX_UTF8_PATH_LENGTH, fullName);
        }

        /// <summary>
        /// Test the given name to see if it fits in metadata.
        /// </summary>
        /// <param name="maxLength">Max length for name.  (Expected to be at least 5.)</param>
        /// <param name="fullName">Name to test.</param>
        /// <returns>True if the name is too long.</returns>
        internal static bool IsTooLong(int maxLength, string fullName)
        {
            // If we have at least five characters to play with, then we can output one 
            // character of fullName (up to three bytes) surrounded by quotes.
            // Otherwise, just throw, since the scenario isn't interesting.
            Debug.Assert(maxLength >= 5, "Expected maxLength to be at least 5");

            if (string.IsNullOrEmpty(fullName) || fullName.Length < maxLength / 3) //UTF-8 uses at most 3 bytes per char
            {
                return false;
            }

            int utf8Length = Utf8Encoding.GetByteCount(fullName);

            // This kickout results in extra work in the event of truncation,
            // but in less work in the much more common case of no truncation.
            if (utf8Length <= maxLength)
            {
                return false;
            }

            return true;
        }
    }
}