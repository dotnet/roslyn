// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract class CommandLineParser
    {
        private readonly CommonMessageProvider messageProvider;
        private readonly bool isInteractive;
        private static readonly char[] SearchPatterTrimChars = new char[] { '\t', '\n', '\v', '\f', '\r', ' ', '\x0085', '\x00a0' };

        internal CommandLineParser(CommonMessageProvider messageProvider, bool isInteractive)
        {
            Debug.Assert(messageProvider != null);
            this.messageProvider = messageProvider;
            this.isInteractive = isInteractive;
        }

        internal CommonMessageProvider MessageProvider
        {
            get { return messageProvider; }
        }

        public bool IsInteractive
        {
            get { return isInteractive; }
        }

        protected abstract string RegularFileExtension { get; }
        protected abstract string ScriptFileExtension { get; }

        // internal for testing
        internal virtual TextReader CreateTextFileReader(string fullPath)
        {
            return new StreamReader(fullPath, detectEncodingFromByteOrderMarks: true);
        }

        /// <summary>
        /// Enumerates files in the specified directory and subdirectories whose name matches the given pattern.
        /// </summary>
        /// <param name="directory">Full path of the directory to enumerate.</param>
        /// <param name="fileNamePattern">File name pattern. May contain wildcards '*' (matches zero or more characters) and '?' (matches any character).</param>
        /// <param name="searchOption">Specifies whether to search the specified <paramref name="directory"/> only, or all its subdirectories as well.</param>
        /// <returns>Sequence of file paths.</returns>
        internal virtual IEnumerable<string> EnumerateFiles(string directory, string fileNamePattern, SearchOption searchOption)
        {
            Debug.Assert(PathUtilities.IsAbsolute(directory));
            return Directory.EnumerateFiles(directory, fileNamePattern, searchOption);
        }

        internal abstract CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string additionalReferencePaths);

        public CommandLineArguments Parse(IEnumerable<string> args, string baseDirectory, string additionalReferencePaths)
        {
            return CommonParse(args, baseDirectory, additionalReferencePaths);
        }

        internal static bool TryParseOption(string arg, out string name, out string value)
        {
            if (string.IsNullOrEmpty(arg) || (arg[0] != '/' && arg[0] != '-'))
            {
                name = null;
                value = null;
                return false;
            }

            int colon = arg.IndexOf(':');
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
                    resolvedPath = Path.GetFullPath(resolvedPath);
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
                    outputFileName = PathUtilities.RemoveTrailingSpacesAndDots(outputFileName);
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

            string unquoted = RemoveAllQuotes(value);
            ParseAndNormalizeFile(unquoted, baseDirectory, out outputFileName, out outputDirectory, out invalidPath);
            if (outputFileName == null ||
                !MetadataHelpers.IsValidAssemblyOrModuleName(outputFileName))
            {
                errors.Add(Diagnostic.Create(messageProvider, messageProvider.FTL_InputFileNameTooLong, invalidPath));
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

            string unquoted = RemoveAllQuotes(value);
            ParseAndNormalizeFile(unquoted, baseDirectory, out outputFileName, out outputDirectory, out invalidPath);
            if (outputFileName == null ||
                PathUtilities.RemoveExtension(outputFileName).Length == 0)
            {
                errors.Add(Diagnostic.Create(messageProvider, messageProvider.FTL_InputFileNameTooLong, invalidPath));
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
                    errors.Add(Diagnostic.Create(messageProvider, messageProvider.FTL_InputFileNameTooLong, invalidPath));
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
            List<string> scriptArgs,
            string baseDirectory,
            List<string> responsePaths = null)
        {
            bool parsingScriptArgs = false;

            Stack<string> args = new Stack<string>(rawArguments.Reverse());
            while (args.Count > 0)
            {
                //EDMAURER trim off whitespace. Otherwise behavioral differences arise
                //when the strings which represent args are constructed by cmd or users.
                //cmd won't produce args with whitespace at the end.
                string arg = args.Pop().TrimEnd();

                if (parsingScriptArgs)
                {
                    scriptArgs.Add(arg);
                    continue;
                }

                if (arg.StartsWith("@", StringComparison.Ordinal))
                {
                    // response file:
                    string path = RemoveAllQuotes(arg.Substring(1)).TrimEnd();
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
                                diagnostics.Add(Diagnostic.Create(messageProvider, messageProvider.WRN_NoConfigNotOnCommandLine));
                            }
                        }

                        if (responsePaths != null)
                        {
                            responsePaths.Add(FileUtilities.NormalizeAbsolutePath(PathUtilities.GetDirectoryName(resolvedPath)));
                        }
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(messageProvider, messageProvider.FTL_InputFileNameTooLong, path));
                    }
                }
                else if (arg == "--" && scriptArgs != null)
                {
                    parsingScriptArgs = true;
                }
                else
                {
                    processedArgs.Add(arg);
                }
            }
        }

        /// <summary>
        /// Parse a response file into a set of arguments. Errors openening the response file are output into "errors".
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
                errors.Add(Diagnostic.Create(messageProvider, messageProvider.ERR_OpenResponseFile, fullPath));
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

        private static readonly char[] resourceSeparators = { ',' };

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
            string[] parts = ParseSeparatedStrings(resourceDescriptor, resourceSeparators).ToArray();

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
                filePath = RemoveAllQuotes(parts[offset + 0]);
            }

            if (length >= 2)
            {
                resourceName = RemoveAllQuotes(parts[offset + 1]);
            }

            if (length >= 3)
            {
                accessibility = RemoveAllQuotes(parts[offset + 2]);
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
        /// Remove all double quote characters from the given string.
        /// </summary>
        internal static string RemoveAllQuotes(string arg)
        {
            return arg != null ? arg.Replace("\"", "") : null;
        }

        /// <summary>
        /// Remove one set of leading and trailing double quote characters, if both are present.
        /// </summary>
        internal static string Unquote(string arg)
        {
            bool quoted;
            return Unquote(arg, out quoted);
        }

        internal static string Unquote(string arg, out bool quoted)
        {
            if (arg.Length > 1 && arg[0] == '"' && arg[arg.Length - 1] == '"')
            {
                quoted = true;
                return arg.Substring(1, arg.Length - 2);
            }
            else
            {
                quoted = false;
                return arg;
            }
        }

        /// <summary>
        /// Split a command line by the same rules as Main would get the commands.
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
            bool inQuotes = false;
            int backslashCount = 0;

            return Split(commandLine,
                (c =>
                {
                    if (c == '\\')
                    {
                        backslashCount += 1;
                    }
                    else if (c == '\"')
                    {
                        if ((backslashCount & 1) != 1)
                        {
                            inQuotes = !inQuotes;
                        }
                        backslashCount = 0;
                    }
                    else
                    {
                        backslashCount = 0;
                    }

                    return !inQuotes && IsCommandLineDelimiter(c);
                }))
            .Select(arg => arg.Trim())                                                                  // Trim whitespace
            .TakeWhile(arg => (!removeHashComments || !arg.StartsWith("#", StringComparison.Ordinal)))  // If removeHashComments is true, skip all arguments after one that starts with '#'
            .Select(arg => Unquote(CondenseDoubledBackslashes(arg)))                                    // Remove quotes and handle backslashes.
            .Where(arg => !string.IsNullOrEmpty(arg));                        							// Don't produce empty strings.
        }

        private static bool IsCommandLineDelimiter(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
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

        /// <summary>
        /// Condense double backslashes that precede a quotation mark to single backslashes.
        /// </summary>
        private static string CondenseDoubledBackslashes(string input)
        {
            // Simple case -- no backslashes.
            if (!input.Contains('\\'))
                return input;

            StringBuilder builder = new StringBuilder();
            int backslashCount = 0;

            foreach (char c in input)
            {
                if (c == '\\')
                {
                    ++backslashCount;
                }
                else
                {
                    // Add right amount of pending backslashes.
                    if (c == '\"')
                    {
                        AddBackslashes(builder, backslashCount / 2);
                    }
                    else
                    {
                        AddBackslashes(builder, backslashCount);
                    }

                    builder.Append(c);
                    backslashCount = 0;
                }
            }

            AddBackslashes(builder, backslashCount);
            return builder.ToString();
        }

        /// <summary>
        /// Add "count" backslashes to a StringBuilder. 
        /// </summary>
        private static void AddBackslashes(StringBuilder builder, int count)
        {
            builder.Append('\\', count);
        }

        private static readonly char[] pathSeparators = { ';', ',' };
        private static readonly char[] Wildcards = new[] { '*', '?' };

        internal static IEnumerable<string> ParseSeparatedPaths(string str)
        {
            return ParseSeparatedStrings(str, pathSeparators, StringSplitOptions.RemoveEmptyEntries).Select(path => RemoveAllQuotes(path));
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
                    errors.Add(Diagnostic.Create(messageProvider, messageProvider.FTL_InputFileNameTooLong, path));
                }
                else
                {
                    yield return resolvedPath;
                }
            }
        }

        internal CommandLineSourceFile ToCommandLineSourceFile(string resolvedPath)
        {
            string extension = PathUtilities.GetExtension(resolvedPath);

            bool isScriptFile;
            if (IsInteractive)
            {
                isScriptFile = !string.Equals(extension, RegularFileExtension, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isScriptFile = string.Equals(extension, ScriptFileExtension, StringComparison.OrdinalIgnoreCase);
            }

            return new CommandLineSourceFile(resolvedPath, isScriptFile);
        }

        internal IEnumerable<CommandLineSourceFile> ParseFileArgument(string arg, string baseDirectory, IList<Diagnostic> errors)
        {
            Debug.Assert(!arg.StartsWith("/") && !arg.StartsWith("-") && !arg.StartsWith("@"));

            // We remove all doubles quotes from a file name. So that, for example:
            //   "Path With Spaces"\foo.cs
            // becomes
            //   Path With Spaces\foo.cs

            string path = RemoveAllQuotes(arg);

            int wildcard = path.IndexOfAny(Wildcards);
            if (wildcard != -1)
            {
                foreach (var file in ExpandFileNamePattern(path, baseDirectory, SearchOption.TopDirectoryOnly, errors))
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

        internal IEnumerable<CommandLineSourceFile> ParseRecurseArgument(string arg, string baseDirectory, IList<Diagnostic> errors)
        {
            return ExpandFileNamePattern(arg, baseDirectory, SearchOption.AllDirectories, errors);
        }

        internal Encoding ParseCodepage(string arg)
        {
            long codepage;
            if (!string.IsNullOrWhiteSpace(arg)
                && long.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out codepage) 
                && (codepage > 0))
            {
                try
                {
                    return Encoding.GetEncoding((int)codepage);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }

        private IEnumerable<CommandLineSourceFile> ExpandFileNamePattern(string path, string baseDirectory, SearchOption searchOption, IList<Diagnostic> errors)
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
                pattern = pattern.Trim(SearchPatterTrimChars);
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
                    if (searchOption == SearchOption.AllDirectories)
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
            return RuleSet.GetDiagnosticOptionsFromRulesetFile(diagnosticOptions, path, baseDirectory, diagnostics, this.messageProvider);
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
    internal static class CommandLineSplitter
    {
        public static bool IsDelimiter(char c)
        {
            return c == ' ' || c == '\t';
        }

        public static bool IsQuote(char c)
        {
            // Only double quotes are respected, according to MSDN.
            return c == '\"';
        }

        private const char Backslash = '\\';

        // Split a command line by the same rules as Main would get the commands.
        public static string[] SplitCommandLine(string commandLine)
        {
            bool inQuotes = false;
            int backslashCount = 0;

            return commandLine.Split(c =>
            {
                if (c == Backslash)
                {
                    backslashCount += 1;
                }
                else if (IsQuote(c))
                {
                    if ((backslashCount & 1) != 1)
                    {
                        inQuotes = !inQuotes;
                    }
                    backslashCount = 0;
                }
                else
                {
                    backslashCount = 0;
                }

                return !inQuotes && IsDelimiter(c);
            })
            .Select(arg => arg.Trim().CondenseDoubledBackslashes().TrimMatchingQuotes())
            .Where(arg => !string.IsNullOrEmpty(arg))
            .ToArray();
        }

        // Split a string, based on whether "splitHere" returned true on each character.
        private static IEnumerable<string> Split(this string str,
                                                 Func<char, bool> splitHere)
        {
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

        // Trim leading and trailing quotes from a string, if they are there. Only trims
        // one pair.
        private static string TrimMatchingQuotes(this string input)
        {
            if ((input.Length >= 2) &&
                (IsQuote(input[0])) &&
                (IsQuote(input[input.Length - 1])))
            {
                return input.Substring(1, input.Length - 2);
            }
            else
            {
                return input;
            }
        }

        // Condense double backslashes that precede a quotation mark to single backslashes.
        private static string CondenseDoubledBackslashes(this string input)
        {
            // Simple case -- no backslashes.
            if (!input.Contains(Backslash))
                return input;

            StringBuilder builder = new StringBuilder();
            int doubleQuoteCount = 0;

            foreach (char c in input)
            {
                if (c == Backslash)
                {
                    ++doubleQuoteCount;
                }
                else
                {
                    // Add right amount of pending backslashes.
                    if (IsQuote(c))
                    {
                        AddBackslashes(builder, doubleQuoteCount / 2);
                    }
                    else
                    {
                        AddBackslashes(builder, doubleQuoteCount);
                    }

                    builder.Append(c);
                    doubleQuoteCount = 0;
                }
            }

            AddBackslashes(builder, doubleQuoteCount);
            return builder.ToString();
        }

        private static void AddBackslashes(StringBuilder builder, int count)
        {
            for (int i = 0; i < count; ++i)
                builder.Append(Backslash);
        }
    }
}
