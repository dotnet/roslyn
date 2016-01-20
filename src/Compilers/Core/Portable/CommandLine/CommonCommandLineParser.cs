// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    public abstract class CommandLineParser
    {
        private readonly CommonMessageProvider _messageProvider;
        internal readonly bool IsScriptRunner;
        private static readonly char[] s_searchPatternTrimChars = new char[] { '\t', '\n', '\v', '\f', '\r', ' ', '\x0085', '\x00a0' };

        internal CommandLineParser(CommonMessageProvider messageProvider, bool isScriptRunner)
        {
            Debug.Assert(messageProvider != null);
            _messageProvider = messageProvider;
            IsScriptRunner = isScriptRunner;
        }

        internal CommonMessageProvider MessageProvider
        {
            get { return _messageProvider; }
        }

        protected abstract string RegularFileExtension { get; }
        protected abstract string ScriptFileExtension { get; }

        // internal for testing
        internal virtual TextReader CreateTextFileReader(string fullPath)
        {
            return new StreamReader(
                PortableShim.FileStream.Create(fullPath, PortableShim.FileMode.Open, PortableShim.FileAccess.Read, PortableShim.FileShare.Read),
                detectEncodingFromByteOrderMarks: true);
        }

        /// <summary>
        /// Enumerates files in the specified directory and subdirectories whose name matches the given pattern.
        /// </summary>
        /// <param name="directory">Full path of the directory to enumerate.</param>
        /// <param name="fileNamePattern">File name pattern. May contain wildcards '*' (matches zero or more characters) and '?' (matches any character).</param>
        /// <param name="searchOption">Specifies whether to search the specified <paramref name="directory"/> only, or all its subdirectories as well.</param>
        /// <returns>Sequence of file paths.</returns>
        internal virtual IEnumerable<string> EnumerateFiles(string directory, string fileNamePattern, object searchOption)
        {
            Debug.Assert(PathUtilities.IsAbsolute(directory));
            return PortableShim.Directory.EnumerateFiles(directory, fileNamePattern, searchOption);
        }

        internal abstract CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string sdkDirectoryOpt, string additionalReferenceDirectories);

        /// <summary>
        /// Parses a command line.
        /// </summary>
        /// <param name="args">A collection of strings representing the command line arguments.</param>
        /// <param name="baseDirectory">The base directory used for qualifying file locations.</param>
        /// <param name="sdkDirectory">The directory to search for mscorlib, or null if not available.</param>
        /// <param name="additionalReferenceDirectories">A string representing additional reference paths.</param>
        /// <returns>a <see cref="CommandLineArguments"/> object representing the parsed command line.</returns>
        public CommandLineArguments Parse(IEnumerable<string> args, string baseDirectory, string sdkDirectory, string additionalReferenceDirectories)
        {
            return CommonParse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories);
        }

        private static bool IsOption(string arg)
        {
            return !string.IsNullOrEmpty(arg) && (arg[0] == '/' || arg[0] == '-');
        }

        internal static bool TryParseOption(string arg, out string name, out string value)
        {
            if (!IsOption(arg))
            {
                name = null;
                value = null;
                return false;
            }

            int colon = arg.IndexOf(':');

            // temporary heuristic to detect Unix-style rooted paths
            // pattern /foo/*  or  //* will not be treated as a compiler option
            //
            // TODO: consider introducing "/s:path" to disambiguate paths starting with /
            if (arg.Length > 1)
            {
                int separator = arg.IndexOf('/', 1);
                if (separator > 0 && (colon < 0 || separator < colon))
                {
                    //   "/foo/
                    //   "//
                    name = null;
                    value = null;
                    return false;
                }
            }

            if (colon >= 0)
            {
                name = arg.Substring(1, colon - 1);
                value = arg.Substring(colon + 1);
            }
            else
            {
                name = arg.Substring(1);
                value = null;
            }

            name = name.ToLowerInvariant();
            return true;
        }

        internal static void ParseAndNormalizeFile(
            string unquoted,
            string baseDirectory,
            out string outputFileName,
            out string outputDirectory,
            out string invalidPath)
        {
            outputFileName = null;
            outputDirectory = null;
            invalidPath = unquoted;

            string resolvedPath = FileUtilities.ResolveRelativePath(unquoted, baseDirectory);
            if (resolvedPath != null)
            {
                try
                {
                    // Check some ancient reserved device names, such as COM1,..9, LPT1..9, PRN, CON, or AUX etc., and bail out earlier
                    // Win32 API - GetFullFileName - will resolve them, say 'COM1', as "\\.\COM1" 
                    resolvedPath = PortableShim.Path.GetFullPath(resolvedPath);
                    // preserve possible invalid path info for diagnostic purpose
                    invalidPath = resolvedPath;

                    outputFileName = Path.GetFileName(resolvedPath);
                    outputDirectory = Path.GetDirectoryName(resolvedPath);
                }
                catch (Exception)
                {
                    resolvedPath = null;
                }

                if (outputFileName != null)
                {
                    // normalize file
                    outputFileName = RemoveTrailingSpacesAndDots(outputFileName);
                }
            }

            if (resolvedPath == null ||
                // NUL-terminated, non-empty, valid Unicode strings
                !MetadataHelpers.IsValidMetadataIdentifier(outputDirectory) ||
                !MetadataHelpers.IsValidMetadataIdentifier(outputFileName))
            {
                outputFileName = null;
            }
        }

        /// <summary>
        /// Trims all '.' and whitespace from the end of the path
        /// </summary>
        internal static string RemoveTrailingSpacesAndDots(string path)
        {
            if (path == null)
            {
                return path;
            }

            int length = path.Length;
            for (int i = length - 1; i >= 0; i--)
            {
                char c = path[i];
                if (!char.IsWhiteSpace(c) && c != '.')
                {
                    return i == (length - 1) ? path : path.Substring(0, i + 1);
                }
            }

            return string.Empty;
        }

        protected ImmutableArray<KeyValuePair<string, string>> ParsePathMap(string pathMap, IList<Diagnostic> errors)
        {
            var pathMapBuilder = ArrayBuilder<KeyValuePair<string, string>>.GetInstance();
            if (pathMap.IsEmpty())
            {
                return pathMapBuilder.ToImmutableAndFree();
            }

            foreach (var kEqualsV in pathMap.Split(','))
            {
                var kv = kEqualsV.Split('=');
                if (kv.Length != 2)
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.ERR_InvalidPathMap, kEqualsV));
                    continue;
                }
                var from = PathUtilities.TrimTrailingSeparators(kv[0]);
                var to = PathUtilities.TrimTrailingSeparators(kv[1]);

                if (from.Length == 0 || (to.Length == 0 && kv[1] != "/"))
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.ERR_InvalidPathMap, kEqualsV));
                }
                else
                {
                    pathMapBuilder.Add(new KeyValuePair<string, string>(from, to));
                }
            }

            return pathMapBuilder.ToImmutableAndFree();
        }

        internal void ParseOutputFile(
            string value,
            IList<Diagnostic> errors,
            string baseDirectory,
            out string outputFileName,
            out string outputDirectory)
        {
            outputFileName = null;
            outputDirectory = null;
            string invalidPath = null;

            string unquoted = RemoveQuotesAndSlashes(value);
            ParseAndNormalizeFile(unquoted, baseDirectory, out outputFileName, out outputDirectory, out invalidPath);
            if (outputFileName == null ||
                !MetadataHelpers.IsValidAssemblyOrModuleName(outputFileName))
            {
                errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InputFileNameTooLong, invalidPath));
                outputFileName = null;
                outputDirectory = baseDirectory;
            }
        }

        internal string ParsePdbPath(
            string value,
            IList<Diagnostic> errors,
            string baseDirectory)
        {
            string outputFileName = null;
            string outputDirectory = null;
            string pdbPath = null;
            string invalidPath = null;

            string unquoted = RemoveQuotesAndSlashes(value);
            ParseAndNormalizeFile(unquoted, baseDirectory, out outputFileName, out outputDirectory, out invalidPath);
            if (outputFileName == null ||
                PathUtilities.ChangeExtension(outputFileName, extension: null).Length == 0)
            {
                errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InputFileNameTooLong, invalidPath));
            }
            else
            {
                pdbPath = Path.ChangeExtension(Path.Combine(outputDirectory, outputFileName), ".pdb");
            }

            return pdbPath;
        }

        internal string ParseGenericPathToFile(
            string unquoted,
            IList<Diagnostic> errors,
            string baseDirectory,
            bool generateDiagnostic = true)
        {
            string outputFileName = null;
            string outputDirectory = null;
            string genericPath = null;
            string invalidPath = null;

            ParseAndNormalizeFile(unquoted, baseDirectory, out outputFileName, out outputDirectory, out invalidPath);
            if (string.IsNullOrWhiteSpace(outputFileName))
            {
                if (generateDiagnostic)
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InputFileNameTooLong, invalidPath));
                }
            }
            else
            {
                genericPath = Path.Combine(outputDirectory, outputFileName);
            }

            return genericPath;
        }

        internal void FlattenArgs(
            IEnumerable<string> rawArguments,
            IList<Diagnostic> diagnostics,
            List<string> processedArgs,
            List<string> scriptArgsOpt,
            string baseDirectory,
            List<string> responsePaths = null)
        {
            bool parsingScriptArgs = false;
            bool sourceFileSeen = false;
            bool optionsEnded = false;

            var args = new Stack<string>(rawArguments.Reverse());
            while (args.Count > 0)
            {
                // EDMAURER trim off whitespace. Otherwise behavioral differences arise
                // when the strings which represent args are constructed by cmd or users.
                // cmd won't produce args with whitespace at the end.
                string arg = args.Pop().TrimEnd();

                if (parsingScriptArgs)
                {
                    scriptArgsOpt.Add(arg);
                    continue;
                }

                if (scriptArgsOpt != null)
                {
                    // The order of the following two checks matters.
                    //
                    // Command line:               Script:    Script args:
                    //   csi -- script.csx a b c   script.csx      ["a", "b", "c"]
                    //   csi script.csx -- a b c   script.csx      ["--", "a", "b", "c"]
                    //   csi -- @script.csx a b c  @script.csx     ["a", "b", "c"]
                    //
                    if (sourceFileSeen)
                    {
                        // csi/vbi: at most one script can be specified on command line, anything else is a script arg:
                        parsingScriptArgs = true;
                        scriptArgsOpt.Add(arg);
                        continue;
                    }

                    if (!optionsEnded && arg == "--")
                    {
                        // csi/vbi: no argument past "--" should be treated as an option/response file
                        optionsEnded = true;
                        processedArgs.Add(arg);
                        continue;
                    }
                }

                if (!optionsEnded && arg.StartsWith("@", StringComparison.Ordinal))
                {
                    // response file:
                    string path = RemoveQuotesAndSlashes(arg.Substring(1)).TrimEnd(null);
                    string resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                    if (resolvedPath != null)
                    {
                        foreach (string newArg in ParseResponseFile(resolvedPath, diagnostics).Reverse())
                        {
                            // Ignores /noconfig option specified in a response file
                            if (!string.Equals(newArg, "/noconfig", StringComparison.OrdinalIgnoreCase) && !string.Equals(newArg, "-noconfig", StringComparison.OrdinalIgnoreCase))
                            {
                                args.Push(newArg);
                            }
                            else
                            {
                                diagnostics.Add(Diagnostic.Create(_messageProvider, _messageProvider.WRN_NoConfigNotOnCommandLine));
                            }
                        }

                        if (responsePaths != null)
                        {
                            responsePaths.Add(FileUtilities.NormalizeAbsolutePath(PathUtilities.GetDirectoryName(resolvedPath)));
                        }
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InputFileNameTooLong, path));
                    }
                }
                else
                {
                    processedArgs.Add(arg);
                    sourceFileSeen |= optionsEnded || !IsOption(arg);
                }
            }
        }

        /// <summary>
        /// Returns false if any of the client arguments are invalid and true otherwise.
        /// </summary>
        /// <param name="args">
        /// The original args to the client.
        /// </param>
        /// <param name="parsedArgs">
        /// The original args minus the client args, if no errors were encountered.
        /// </param>
        /// <param name="containsShared">
        /// Only defined if no errors were encountered.
        /// True if '/shared' was an argument, false otherwise.
        /// </param>
        /// <param name="keepAliveValue">
        /// Only defined if no errors were encountered.
        /// The value to the '/keepalive' argument if one was specified, null otherwise.
        /// </param>
        /// <param name="errorMessage">
        /// Only defined if errors were encountered.
        /// The error message for the encountered error.
        /// </param>
        /// <param name="sessionKey">
        /// Only specified if <paramref name="containsShared"/> is true and the session key
        /// was provided.  Can be null
        /// </param>
        internal static bool TryParseClientArgs(
            IEnumerable<string> args,
            out List<string> parsedArgs,
            out bool containsShared,
            out string keepAliveValue,
            out string sessionKey,
            out string errorMessage)
        {
            containsShared = false;
            keepAliveValue = null;
            errorMessage = null;
            parsedArgs = null;
            sessionKey = null;
            var newArgs = new List<string>();
            foreach (var arg in args)
            {
                bool hasValue;
                string value;
                if (IsClientArgsOption(arg, "/keepalive", out hasValue, out value))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        errorMessage = CodeAnalysisResources.MissingKeepAlive;
                        return false;
                    }

                    int intValue;
                    if (int.TryParse(value, out intValue))
                    {
                        if (intValue < -1)
                        {
                            errorMessage = CodeAnalysisResources.KeepAliveIsTooSmall;
                            return false;
                        }
                        keepAliveValue = value;
                    }
                    else
                    {
                        errorMessage = CodeAnalysisResources.KeepAliveIsNotAnInteger;
                        return false;
                    }
                    continue;
                }

                if (IsClientArgsOption(arg, "/shared", out hasValue, out value))
                {
                    if (hasValue)
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            errorMessage = CodeAnalysisResources.SharedArgumentMissing;
                            return false;
                        }

                        sessionKey = value;
                    }

                    containsShared = true;
                    continue;
                }

                newArgs.Add(arg);
            }

            if (keepAliveValue != null && !containsShared)
            {
                errorMessage = CodeAnalysisResources.KeepAliveWithoutShared;
                return false;
            }
            else
            {
                parsedArgs = newArgs;
                return true;
            }
        }

        internal static bool IsClientArgsOption(string arg, string optionName, out bool hasValue, out string optionValue)
        {
            hasValue = false;
            optionValue = null;

            if (!arg.StartsWith(optionName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (arg.Length > optionName.Length && !(arg[optionName.Length] == ':' || arg[optionName.Length] == '='))
            {
                return false;
            }

            if (arg.Length > optionName.Length)
            {
                hasValue = true;
                optionValue = arg.Substring(optionName.Length + 1).Trim('"');
            }

            return true;
        }

        internal static string MismatchedVersionErrorText => CodeAnalysisResources.MismatchedVersion;

        /// <summary>
        /// Parse a response file into a set of arguments. Errors opening the response file are output into "errors".
        /// </summary>
        internal IEnumerable<string> ParseResponseFile(string fullPath, IList<Diagnostic> errors)
        {
            List<string> lines = new List<string>();
            try
            {
                Debug.Assert(PathUtilities.IsAbsolute(fullPath));
                using (TextReader reader = CreateTextFileReader(fullPath))
                {
                    string str;
                    while ((str = reader.ReadLine()) != null)
                    {
                        lines.Add(str);
                    }
                }
            }
            catch (Exception)
            {
                errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.ERR_OpenResponseFile, fullPath));
                return SpecializedCollections.EmptyEnumerable<string>();
            }

            return ParseResponseLines(lines);
        }

        /// <summary>
        /// Take a string of lines from a response file, remove comments, 
        /// and split into a set of command line arguments.
        /// </summary>
        internal static IEnumerable<string> ParseResponseLines(IEnumerable<string> lines)
        {
            List<string> arguments = new List<string>();

            foreach (string line in lines)
            {
                arguments.AddRange(SplitCommandLineIntoArguments(line, removeHashComments: true));
            }

            return arguments;
        }

        private static readonly char[] s_resourceSeparators = { ',' };

        internal static void ParseResourceDescription(
            string resourceDescriptor,
            string baseDirectory,
            bool skipLeadingSeparators, //VB does this
            out string filePath,
            out string fullPath,
            out string fileName,
            out string resourceName,
            out string accessibility)
        {
            filePath = null;
            fullPath = null;
            fileName = null;
            resourceName = null;
            accessibility = null;

            // resource descriptor is: "<filePath>[,<string name>[,public|private]]"
            string[] parts = ParseSeparatedStrings(resourceDescriptor, s_resourceSeparators).ToArray();

            int offset = 0;

            int length = parts.Length;

            if (skipLeadingSeparators)
            {
                for (; offset < length && string.IsNullOrEmpty(parts[offset]); offset++)
                {
                }

                length -= offset;
            }


            if (length >= 1)
            {
                filePath = RemoveQuotesAndSlashes(parts[offset + 0]);
            }

            if (length >= 2)
            {
                resourceName = RemoveQuotesAndSlashes(parts[offset + 1]);
            }

            if (length >= 3)
            {
                accessibility = RemoveQuotesAndSlashes(parts[offset + 2]);
            }

            if (String.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            fileName = PathUtilities.GetFileName(filePath);
            fullPath = FileUtilities.ResolveRelativePath(filePath, baseDirectory);

            // The default resource name is the file name.
            // Also use the file name for the name when user specifies string like "filePath,,private"
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                resourceName = fileName;
            }
        }

        /// <summary>
        /// Split a command line by the same rules as Main would get the commands except the original
        /// state of backslashes and quotes are preserved.  For example in normal Windows command line 
        /// parsing the following command lines would produce equivalent Main arguments:
        /// 
        ///     - /r:a,b
        ///     - /r:"a,b"
        /// 
        /// This method will differ as the latter will have the quotes preserved.  The only case where 
        /// quotes are removed is when the entire argument is surrounded by quotes without any inner
        /// quotes. 
        /// </summary>
        /// <remarks>
        /// Rules for command line parsing, according to MSDN:
        /// 
        /// Arguments are delimited by white space, which is either a space or a tab.
        ///  
        /// A string surrounded by double quotation marks ("string") is interpreted 
        /// as a single argument, regardless of white space contained within. 
        /// A quoted string can be embedded in an argument.
        ///  
        /// A double quotation mark preceded by a backslash (\") is interpreted as a 
        /// literal double quotation mark character (").
        ///  
        /// Backslashes are interpreted literally, unless they immediately precede a 
        /// double quotation mark.
        ///  
        /// If an even number of backslashes is followed by a double quotation mark, 
        /// one backslash is placed in the argv array for every pair of backslashes, 
        /// and the double quotation mark is interpreted as a string delimiter.
        ///  
        /// If an odd number of backslashes is followed by a double quotation mark, 
        /// one backslash is placed in the argv array for every pair of backslashes, 
        /// and the double quotation mark is "escaped" by the remaining backslash, 
        /// causing a literal double quotation mark (") to be placed in argv.
        /// </remarks>
        public static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments)
        {
            char? unused;
            return SplitCommandLineIntoArguments(commandLine, removeHashComments, out unused);
        }

        private static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments, out char? illegalChar)
        {
            var builder = new StringBuilder(commandLine.Length);
            var list = new List<string>();
            var i = 0;

            illegalChar = null;
            while (i < commandLine.Length)
            {
                while (i < commandLine.Length && char.IsWhiteSpace(commandLine[i]))
                {
                    i++;
                }

                if (i == commandLine.Length)
                {
                    break;
                }

                if (commandLine[i] == '#' && removeHashComments)
                {
                    break;
                }

                var quoteCount = 0;
                builder.Length = 0;
                while (i < commandLine.Length && (!char.IsWhiteSpace(commandLine[i]) || (quoteCount % 2 != 0)))
                {
                    var current = commandLine[i];
                    switch (current)
                    {
                        case '\\':
                            {
                                var slashCount = 0;
                                do
                                {
                                    builder.Append(commandLine[i]);
                                    i++;
                                    slashCount++;
                                } while (i < commandLine.Length && commandLine[i] == '\\');

                                // Slashes not followed by a quote character can be ignored for now
                                if (i >= commandLine.Length || commandLine[i] != '"')
                                {
                                    break;
                                }

                                // If there is an odd number of slashes then it is escaping the quote
                                // otherwise it is just a quote.
                                if (slashCount % 2 == 0)
                                {
                                    quoteCount++;
                                }

                                builder.Append('"');
                                i++;
                                break;
                            }

                        case '"':
                            builder.Append(current);
                            quoteCount++;
                            i++;
                            break;

                        default:
                            if ((current >= 0x1 && current <= 0x1f) || current == '|')
                            {
                                if (illegalChar == null)
                                {
                                    illegalChar = current;
                                }
                            }
                            else
                            {
                                builder.Append(current);
                            }

                            i++;
                            break;
                    }
                }

                // If the quote string is surrounded by quotes with no interior quotes then 
                // remove the quotes here. 
                if (quoteCount == 2 && builder[0] == '"' && builder[builder.Length - 1] == '"')
                {
                    builder.Remove(0, length: 1);
                    builder.Remove(builder.Length - 1, length: 1);
                }

                if (builder.Length > 0)
                {
                    list.Add(builder.ToString());
                }
            }

            return list;
        }

        /// <summary>
        /// Remove the extraneous quotes and slashes from the argument.  This function is designed to have
        /// compat behavior with the native compiler.
        /// </summary>
        /// <remarks>
        /// Mimics the function RemoveQuotes from the native C# compiler.  The native VB equivalent of this 
        /// function is called RemoveQuotesAndSlashes.  It has virtually the same behavior except for a few 
        /// quirks in error cases.  
        /// </remarks>
        internal static string RemoveQuotesAndSlashes(string arg)
        {
            if (arg == null)
            {
                return arg;
            }

            var pool = PooledStringBuilder.GetInstance();
            var builder = pool.Builder;
            var i = 0;
            while (i < arg.Length)
            {
                var cur = arg[i];
                switch (cur)
                {
                    case '\\':
                        ProcessSlashes(builder, arg, ref i);
                        break;
                    case '"':
                        // Intentionally dropping quotes that don't have explicit escaping.
                        i++;
                        break;
                    default:
                        builder.Append(cur);
                        i++;
                        break;
                }
            }

            return pool.ToStringAndFree();
        }

        /// <summary>
        /// Mimic behavior of the native function by the same name.
        /// </summary>
        internal static void ProcessSlashes(StringBuilder builder, string arg, ref int i)
        {
            Debug.Assert(arg != null);
            Debug.Assert(i < arg.Length);

            var slashCount = 0;
            while (i < arg.Length && arg[i] == '\\')
            {
                slashCount++;
                i++;
            }

            if (i < arg.Length && arg[i] == '"')
            {
                // Before a quote slashes are interpretted as escape sequences for other slashes so
                // output one for every two.
                while (slashCount >= 2)
                {
                    builder.Append('\\');
                    slashCount -= 2;
                }

                Debug.Assert(slashCount >= 0);

                // If there is an odd number of slashes then the quote is escaped and hence a part
                // of the output.  Otherwise it is a normal quote and can be ignored. 
                if (slashCount == 1)
                {
                    // The quote is escaped so eat it.
                    builder.Append('"');
                }

                i++;
            }
            else
            {
                // Slashes that aren't followed by quotes are simply slashes.
                while (slashCount > 0)
                {
                    builder.Append('\\');
                    slashCount--;
                }
            }
        }

        /// <summary>
        /// Split a string, based on whether "splitHere" returned true on each character.
        /// </summary>
        private static IEnumerable<string> Split(string str, Func<char, bool> splitHere)
        {
            if (str == null)
            {
                yield break;
            }

            int nextPiece = 0;

            for (int c = 0; c < str.Length; c++)
            {
                if (splitHere(str[c]))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        private static readonly char[] s_pathSeparators = { ';', ',' };
        private static readonly char[] s_wildcards = new[] { '*', '?' };

        internal static IEnumerable<string> ParseSeparatedPaths(string str)
        {
            return ParseSeparatedStrings(str, s_pathSeparators, StringSplitOptions.RemoveEmptyEntries).Select(RemoveQuotesAndSlashes);
        }

        /// <summary>
        /// Split a string by a set of separators, taking quotes into account.
        /// </summary>
        internal static IEnumerable<string> ParseSeparatedStrings(string str, char[] separators, StringSplitOptions options = StringSplitOptions.None)
        {
            bool inQuotes = false;

            var result = Split(str,
                (c =>
                {
                    if (c == '\"')
                    {
                        inQuotes = !inQuotes;
                    }

                    return !inQuotes && separators.Contains(c);
                }));

            return (options == StringSplitOptions.RemoveEmptyEntries) ? result.Where(s => s.Length > 0) : result;
        }

        internal IEnumerable<string> ResolveRelativePaths(IEnumerable<string> paths, string baseDirectory, IList<Diagnostic> errors)
        {
            foreach (var path in paths)
            {
                string resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                if (resolvedPath == null)
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InputFileNameTooLong, path));
                }
                else
                {
                    yield return resolvedPath;
                }
            }
        }

        private CommandLineSourceFile ToCommandLineSourceFile(string resolvedPath)
        {
            string extension = PathUtilities.GetExtension(resolvedPath);

            bool isScriptFile;
            if (IsScriptRunner)
            {
                isScriptFile = !string.Equals(extension, RegularFileExtension, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //isScriptFile = string.Equals(extension, ScriptFileExtension, StringComparison.OrdinalIgnoreCase);
                isScriptFile = false;
            }

            return new CommandLineSourceFile(resolvedPath, isScriptFile);
        }

        internal IEnumerable<CommandLineSourceFile> ParseFileArgument(string arg, string baseDirectory, IList<Diagnostic> errors)
        {
            Debug.Assert(IsScriptRunner || !arg.StartsWith("-", StringComparison.Ordinal) && !arg.StartsWith("@", StringComparison.Ordinal));

            // We remove all doubles quotes from a file name. So that, for example:
            //   "Path With Spaces"\foo.cs
            // becomes
            //   Path With Spaces\foo.cs

            string path = RemoveQuotesAndSlashes(arg);

            int wildcard = path.IndexOfAny(s_wildcards);
            if (wildcard != -1)
            {
                foreach (var file in ExpandFileNamePattern(path, baseDirectory, PortableShim.SearchOption.TopDirectoryOnly, errors))
                {
                    yield return file;
                }
            }
            else
            {
                string resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                if (resolvedPath == null)
                {
                    errors.Add(Diagnostic.Create(MessageProvider, (int)MessageProvider.FTL_InputFileNameTooLong, path));
                }
                else
                {
                    yield return ToCommandLineSourceFile(resolvedPath);
                }
            }
        }

        internal IEnumerable<CommandLineSourceFile> ParseAdditionalFileArgument(string value, string baseDirectory, IList<Diagnostic> errors)
        {
            foreach (string path in ParseSeparatedPaths(value).Where((path) => !string.IsNullOrWhiteSpace(path)))
            {
                foreach (var file in ParseFileArgument(path, baseDirectory, errors))
                {
                    yield return file;
                }
            }
        }

        internal IEnumerable<CommandLineSourceFile> ParseRecurseArgument(string arg, string baseDirectory, IList<Diagnostic> errors)
        {
            return ExpandFileNamePattern(arg, baseDirectory, PortableShim.SearchOption.AllDirectories, errors);
        }

        internal Encoding TryParseEncodingName(string arg)
        {
            long codepage;
            if (!string.IsNullOrWhiteSpace(arg)
                && long.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out codepage)
                && (codepage > 0))
            {
                try
                {
                    return PortableShim.Encoding.GetEncoding((int)codepage);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        internal SourceHashAlgorithm TryParseHashAlgorithmName(string arg)
        {
            if (string.Equals("sha1", arg, StringComparison.OrdinalIgnoreCase))
            {
                return SourceHashAlgorithm.Sha1;
            }

            if (string.Equals("sha256", arg, StringComparison.OrdinalIgnoreCase))
            {
                return SourceHashAlgorithm.Sha256;
            }

            // MD5 is legacy, not supported

            return SourceHashAlgorithm.None;
        }

        private IEnumerable<CommandLineSourceFile> ExpandFileNamePattern(string path, string baseDirectory, object searchOption, IList<Diagnostic> errors)
        {
            string directory = PathUtilities.GetDirectoryName(path);
            string pattern = PathUtilities.GetFileName(path);

            var resolvedDirectoryPath = (directory.Length == 0) ? baseDirectory : FileUtilities.ResolveRelativePath(directory, baseDirectory);

            IEnumerator<string> enumerator = null;
            try
            {
                bool yielded = false;

                // NOTE: Directory.EnumerateFiles(...) surprisingly treats pattern "." the 
                //       same way as "*"; as we don't expect anything to be found by this 
                //       pattern, let's just not search in this case
                pattern = pattern.Trim(s_searchPatternTrimChars);
                bool singleDotPattern = string.Equals(pattern, ".", StringComparison.Ordinal);

                if (!singleDotPattern)
                {
                    while (true)
                    {
                        string resolvedPath = null;
                        try
                        {
                            if (enumerator == null)
                            {
                                enumerator = EnumerateFiles(resolvedDirectoryPath, pattern, searchOption).GetEnumerator();
                            }

                            if (!enumerator.MoveNext())
                            {
                                break;
                            }

                            resolvedPath = enumerator.Current;
                        }
                        catch
                        {
                            resolvedPath = null;
                        }

                        if (resolvedPath != null)
                        {
                            // just in case EnumerateFiles returned a relative path
                            resolvedPath = FileUtilities.ResolveRelativePath(resolvedPath, baseDirectory);
                        }

                        if (resolvedPath == null)
                        {
                            errors.Add(Diagnostic.Create(MessageProvider, (int)MessageProvider.FTL_InputFileNameTooLong, path));
                            break;
                        }

                        yielded = true;
                        yield return ToCommandLineSourceFile(resolvedPath);
                    }
                }

                // the pattern didn't match any files:
                if (!yielded)
                {
                    if (searchOption == PortableShim.SearchOption.AllDirectories)
                    {
                        // handling /recurse
                        GenerateErrorForNoFilesFoundInRecurse(path, errors);
                    }
                    else
                    {
                        // handling wildcard in file spec
                        errors.Add(Diagnostic.Create(MessageProvider, (int)MessageProvider.ERR_FileNotFound, path));
                    }
                }
            }
            finally
            {
                if (enumerator != null)
                {
                    enumerator.Dispose();
                }
            }
        }

        internal abstract void GenerateErrorForNoFilesFoundInRecurse(string path, IList<Diagnostic> errors);

        internal ReportDiagnostic GetDiagnosticOptionsFromRulesetFile(Dictionary<string, ReportDiagnostic> diagnosticOptions, IList<Diagnostic> diagnostics, string path, string baseDirectory)
        {
            return RuleSet.GetDiagnosticOptionsFromRulesetFile(diagnosticOptions, path, baseDirectory, diagnostics, _messageProvider);
        }

        /// <summary>
        /// Tries to parse a UInt64 from string in either decimal, octal or hex format.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="result">The result if parsing was successful.</param>
        /// <returns>true if parsing was successful, otherwise false.</returns>
        internal static bool TryParseUInt64(string value, out ulong result)
        {
            result = 0;

            if (String.IsNullOrEmpty(value))
            {
                return false;
            }

            int numBase = 10;

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                numBase = 16;
            }
            else if (value.StartsWith("0", StringComparison.OrdinalIgnoreCase))
            {
                numBase = 8;
            }

            try
            {
                result = Convert.ToUInt64(value, numBase);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to parse a UInt16 from string in either decimal, octal or hex format.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="result">The result if parsing was successful.</param>
        /// <returns>true if parsing was successful, otherwise false.</returns>
        internal static bool TryParseUInt16(string value, out ushort result)
        {
            result = 0;

            if (String.IsNullOrEmpty(value))
            {
                return false;
            }

            int numBase = 10;

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                numBase = 16;
            }
            else if (value.StartsWith("0", StringComparison.OrdinalIgnoreCase))
            {
                numBase = 8;
            }

            try
            {
                result = Convert.ToUInt16(value, numBase);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
