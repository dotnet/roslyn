// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// Functions for dealing with the specially formatted errors returned by
    /// build tools.
    /// </summary>
    /// <remarks>
    /// Various tools produce and consume CanonicalErrors in various formats.
    ///
    /// DEVENV Format When Clicking on Items in the Output Window
    /// (taken from env\msenv\core\findutil.cpp ParseLocation function)
    ///
    ///      v:\dir\file.ext (loc) : msg
    ///      \\server\share\dir\file.ext(loc):msg
    ///      url
    ///
    ///      loc:
    ///      (line)
    ///      (line-line)
    ///      (line,col)
    ///      (line,col-col)
    ///      (line,col,len)
    ///      (line,col,line,col)
    ///
    /// DevDiv Build Process
    /// (taken from tools\devdiv2.def)
    ///
    ///      To echo warnings and errors to the build console, the
    ///      "description block" must be recognized by build. To do this,
    ///      add a $(ECHO_COMPILING_COMMAND) or $(ECHO_PROCESSING_COMMAND)
    ///      to the first line of the description block, e.g.
    ///
    ///          $(ECHO_COMPILING_CMD) Resgen_$&lt;
    ///
    ///      Errors must have the format:
    ///
    ///          &lt;text&gt; : error [num]: &lt;msg&gt;
    ///
    ///      Warnings must have the format:
    ///
    ///          &lt;text&gt; : warning [num]: &lt;msg&gt;
    /// </remarks>
    /// <owner>JomoF</owner>
    internal static class CanonicalError
    {
        // Defines the main pattern for matching messages.
        private static readonly Regex s_originCategoryCodeTextExpression = new Regex
             (
                // Beginning of line and any amount of whitespace.
                @"^\s*"
                // Match a [optional project number prefix 'ddd>'], single letter + colon + remaining filename, or
                // string with no colon followed by a colon.
                + @"(((?<ORIGIN>(((\d+>)?[a-zA-Z]?:[^:]*)|([^:]*))):)"
                // Origin may also be empty. In this case there's no trailing colon.
                + "|())"
                // Match the empty string or a string without a colon that ends with a space
                + "(?<SUBCATEGORY>(()|([^:]*? )))"
                // Match 'error' or 'warning'.
                + @"(?<CATEGORY>(error|warning))"
                // Match anything starting with a space that's not a colon/space, followed by a colon. 
                // Error code is optional in which case "error"/"warning" can be followed immediately by a colon.
                + @"( \s*(?<CODE>[^: ]*))?\s*:"
                // Whatever's left on this line, including colons.
                + "(?<TEXT>.*)$",
                RegexOptions.IgnoreCase
             );

        // Matches and extracts filename and location from an 'origin' element.
        private static readonly Regex s_filenameLocationFromOrigin = new Regex(@"
                ^                                           # Beginning of line
                (\d+>)?                                     # Optional ddd> project number prefix
                (?<FILENAME>.*)                             # Match anything.
                \(                                          # Find a parenthesis.
                (?<LOCATION>[\,,0-9,-]*)                    # Match any combination of numbers and ',' and '-'
                \)\s*                                       # Find the closing paren then any amount of spaces.
                $                                           # End-of-line",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        // Matches location that is a simple number.
        // Example: line
        private static readonly Regex s_lineFromLocation = new Regex(@"
                ^                                           # Beginning of line
                (?<LINE>[0-9]*)                             # Match any number.
                $                                           # End-of-line",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        // Matches location that is a range of lines.
        // Example: line-line
        private static readonly Regex s_lineLineFromLocation = new Regex(@"
                ^                                           # Beginning of line
                (?<LINE>[0-9]*)                             # Match any number.
                -                                           # Dash
                (?<ENDLINE>[0-9]*)                          # Match any number.
                $                                           # End-of-line",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        // Matches location that is a line and column
        // Example: line,col
        private static readonly Regex s_lineColFromLocation = new Regex(@"
                ^                                           # Beginning of line
                (?<LINE>[0-9]*)                             # Match any number.
                ,                                           # Comma
                (?<COLUMN>[0-9]*)                           # Match any number.
                $                                           # End-of-line",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        // Matches location that is a line and column-range
        // Example: line,col-col
        private static readonly Regex s_lineColColFromLocation = new Regex(@"
                ^                                           # Beginning of line
                (?<LINE>[0-9]*)                             # Match any number.
                ,                                           # Comma
                (?<COLUMN>[0-9]*)                           # Match any number.
                -                                           # Dash
                (?<ENDCOLUMN>[0-9]*)                        # Match any number.
                $                                           # End-of-line",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        // Matches location that is line,col,line,col
        // Example: line,col,line,col
        private static readonly Regex s_lineColLineColFromLocation = new Regex(@"
                ^                                           # Beginning of line
                (?<LINE>[0-9]*)                             # Match any number.
                ,                                           # Comma
                (?<COLUMN>[0-9]*)                           # Match any number.
                ,                                           # Comma
                (?<ENDLINE>[0-9]*)                          # Match any number.
                ,                                           # Comma
                (?<ENDCOLUMN>[0-9]*)                        # Match any number.
                $                                           # End-of-line",
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        /// <summary>
        /// Represents the parts of a decomposed canonical message.
        /// </summary>
        /// <owner>JomoF</owner>
        internal sealed class Parts
        {
            /// <summary>
            /// Defines the error category\severity level.
            /// </summary>
            internal enum Category
            {
                Warning,
                Error
            }

            /// <summary>
            /// Value used for unspecified line and column numbers, which are 1-relative.
            /// </summary>
            internal const int numberNotSpecified = 0;

            /// <summary>
            /// Initializes a new instance of the <see cref="Parts"/> class.
            /// </summary>
            internal Parts()
            {
            }

            /// <summary>
            /// Name of the file or tool (not localized)
            /// </summary>
            internal string origin;

            /// <summary>
            /// The line number.
            /// </summary>
            internal int line = Parts.numberNotSpecified;

            /// <summary>
            /// The column number.
            /// </summary>
            internal int column = Parts.numberNotSpecified;

            /// <summary>
            /// The ending line number.
            /// </summary>
            internal int endLine = Parts.numberNotSpecified;

            /// <summary>
            /// The ending column number.
            /// </summary>
            internal int endColumn = Parts.numberNotSpecified;

            /// <summary>
            /// The category/severity level
            /// </summary>
            internal Category category;

            /// <summary>
            /// The sub category (localized)
            /// </summary>
            internal string subcategory;

            /// <summary>
            /// The error code (not localized)
            /// </summary>
            internal string code;

            /// <summary>
            /// The error message text (localized)
            /// </summary>
            internal string text;

#if NEVER
            internal new string ToString()
            {
                return String.Format
                (
                     "Origin='{0}'\n"
                    +"Filename='{1}'\n"
                    +"Line='{2}'\n"
                    +"Column='{3}'\n"
                    +"EndLine='{4}'\n"
                    +"EndColumn='{5}'\n"
                    +"Category='{6}'\n"
                    +"Subcategory='{7}'\n"
                    +"Text='{8}'\n"
                    , origin, line, column, endLine, endColumn, category.ToString(), subcategory, code, text
                );

            }
#endif
        }

        /// <summary>
        /// A small custom int conversion method that treats invalid entries as missing (0). This is done to work around tools
        /// that don't fully conform to the canonical message format - we still want to salvage what we can from the message.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>'value' converted to int or 0 if it can't be parsed or is negative</returns>
        /// <owner>LukaszG</owner>
        private static int ConvertToIntWithDefault(string value)
        {
            int result;
            bool success = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);

            if (!success || (result < 0))
            {
                result = CanonicalError.Parts.numberNotSpecified;
            }

            return result;
        }

        /// <summary>
        /// Decompose an error or warning message into constituent parts. If the message isn't in the canonical form, return null.
        /// </summary>
        /// <remarks>This method is thread-safe, because the Regex class is thread-safe (per MSDN).</remarks>
        /// <owner>JomoF</owner>
        /// <param name="message"></param>
        /// <returns>Decomposed canonical message, or null.</returns>
        internal static Parts Parse(string message)
        {
            // An unusually long string causes pathologically slow Regex back-tracking.
            // To avoid that, only scan the first 400 characters. That's enough for 
            // the longest possible prefix: MAX_PATH, plus a huge subcategory string, and an error location.
            // After the regex is done, we can append the overflow.
            string messageOverflow = String.Empty;
            if (message.Length > 400)
            {
                messageOverflow = message.Substring(400);
                message = message.Substring(0, 400);
            }

            // If a tool has a large amount of output that isn't an error or warning (eg., "dir /s %hugetree%")
            // the regex below is slow. It's faster to pre-scan for "warning" and "error" 
            // and bail out if neither are present.
            if (message.IndexOf("warning", StringComparison.OrdinalIgnoreCase) == -1 &&
                message.IndexOf("error", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return null;
            }

            Parts parsedMessage = new Parts();

            // First, split the message into three parts--Origin, Category, Code, Text.
            // Example,
            //      Main.cs(17,20):Command line warning CS0168: The variable 'goo' is declared but never used
            //      -------------- ------------ ------- ------  ----------------------------------------------
            //      Origin         SubCategory  Cat.    Code    Text
            // 
            // To accommodate absolute filenames in Origin, tolerate a colon in the second position
            // as long as its preceded by a letter.
            //
            // Localization Note:
            //  Even in foreign-language versions of tools, the category field needs to be in English.
            //  Also, if origin is a tool name, then that needs to be in English.
            //
            //  Here's an example from the Japanese version of CL.EXE:
            //   cl : ???? ??? warning D4024 : ?????????? 'AssemblyInfo.cs' ?????????????????? ???????????
            //
            //  Here's an example from the Japanese version of LINK.EXE:
            //   AssemblyInfo.cpp : fatal error LNK1106: ???????????? ??????????????: 0x6580 ??????????
            //
            Match match = s_originCategoryCodeTextExpression.Match(message);

            if (!match.Success)
            {
                // If no match here, then this message is not an error or warning.
                return null;
            }

            string origin = match.Groups["ORIGIN"].Value.Trim();
            string category = match.Groups["CATEGORY"].Value.Trim();
            parsedMessage.code = match.Groups["CODE"].Value.Trim();
            parsedMessage.text = (match.Groups["TEXT"].Value + messageOverflow).Trim();
            parsedMessage.subcategory = match.Groups["SUBCATEGORY"].Value.Trim();

            // Next, see if category is something that is recognized.
            if (0 == String.Compare(category, "error", StringComparison.OrdinalIgnoreCase))
            {
                parsedMessage.category = Parts.Category.Error;
            }
            else if (0 == String.Compare(category, "warning", StringComparison.OrdinalIgnoreCase))
            {
                parsedMessage.category = Parts.Category.Warning;
            }
            else
            {
                // Not an error\warning message.
                return null;
            }

            // Origin is not a simple file, but it still could be of the form,
            //  goo.cpp(location)
            match = s_filenameLocationFromOrigin.Match(origin);

            if (match.Success)
            {
                // The origin is in the form,
                //  goo.cpp(location)
                // Assume the filename exists, but don't verify it. What else could it be?
                string location = match.Groups["LOCATION"].Value.Trim();
                parsedMessage.origin = match.Groups["FILENAME"].Value.Trim();

                // Now, take apart the location. It can be one of these:
                //      loc:
                //      (line)
                //      (line-line)
                //      (line,col)
                //      (line,col-col)
                //      (line,col,len)
                //      (line,col,line,col)
                if (location.Length > 0)
                {
                    match = s_lineFromLocation.Match(location);
                    if (match.Success)
                    {
                        parsedMessage.line = ConvertToIntWithDefault(match.Groups["LINE"].Value.Trim());
                    }
                    else
                    {
                        match = s_lineLineFromLocation.Match(location);
                        if (match.Success)
                        {
                            parsedMessage.line = ConvertToIntWithDefault(match.Groups["LINE"].Value.Trim());
                            parsedMessage.endLine = ConvertToIntWithDefault(match.Groups["ENDLINE"].Value.Trim());
                        }
                        else
                        {
                            match = s_lineColFromLocation.Match(location);
                            if (match.Success)
                            {
                                parsedMessage.line = ConvertToIntWithDefault(match.Groups["LINE"].Value.Trim());
                                parsedMessage.column = ConvertToIntWithDefault(match.Groups["COLUMN"].Value.Trim());
                            }
                            else
                            {
                                match = s_lineColColFromLocation.Match(location);
                                if (match.Success)
                                {
                                    parsedMessage.line = ConvertToIntWithDefault(match.Groups["LINE"].Value.Trim());
                                    parsedMessage.column = ConvertToIntWithDefault(match.Groups["COLUMN"].Value.Trim());
                                    parsedMessage.endColumn = ConvertToIntWithDefault(match.Groups["ENDCOLUMN"].Value.Trim());
                                }
                                else
                                {
                                    match = s_lineColLineColFromLocation.Match(location);
                                    if (match.Success)
                                    {
                                        parsedMessage.line = ConvertToIntWithDefault(match.Groups["LINE"].Value.Trim());
                                        parsedMessage.column = ConvertToIntWithDefault(match.Groups["COLUMN"].Value.Trim());
                                        parsedMessage.endLine = ConvertToIntWithDefault(match.Groups["ENDLINE"].Value.Trim());
                                        parsedMessage.endColumn = ConvertToIntWithDefault(match.Groups["ENDCOLUMN"].Value.Trim());
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // The origin does not fit the filename(location) pattern.
                parsedMessage.origin = origin;
            }

            return parsedMessage;
        }
    }
}
