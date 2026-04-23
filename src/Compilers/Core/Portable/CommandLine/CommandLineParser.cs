// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract class CommandLineParser
    {
        private readonly CommonMessageProvider _messageProvider;
        internal readonly bool IsScriptCommandLineParser;
        private static readonly char[] s_searchPatternTrimChars = new char[] { '\t', '\n', '\v', '\f', '\r', ' ', '\x0085', '\x00a0' };
        internal const string ErrorLogOptionFormat = "<file>[,version={1|1.0|2|2.1}]";

        internal CommandLineParser(CommonMessageProvider messageProvider, bool isScriptCommandLineParser)
        {
            RoslynDebug.Assert(messageProvider != null);
            _messageProvider = messageProvider;
            IsScriptCommandLineParser = isScriptCommandLineParser;
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
                new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read),
                               detectEncodingFromByteOrderMarks: true);
        }

        /// <summary>
        /// Enumerates files in the specified directory and subdirectories whose name matches the given pattern.
        /// </summary>
        /// <param name="directory">Full path of the directory to enumerate.</param>
        /// <param name="fileNamePattern">File name pattern. May contain wildcards '*' (matches zero or more characters) and '?' (matches any character).</param>
        /// <param name="searchOption">Specifies whether to search the specified <paramref name="directory"/> only, or all its subdirectories as well.</param>
        /// <returns>Sequence of file paths.</returns>
        internal virtual IEnumerable<string> EnumerateFiles(string? directory, string fileNamePattern, SearchOption searchOption)
        {
            if (directory is null)
            {
                return SpecializedCollections.EmptyEnumerable<string>();
            }

            Debug.Assert(PathUtilities.IsAbsolute(directory));
            return Directory.EnumerateFiles(directory, fileNamePattern, searchOption);
        }

        internal abstract CommandLineArguments CommonParse(IEnumerable<string> args, string baseDirectory, string? sdkDirectory, string? additionalReferenceDirectories);

        /// <summary>
        /// Parses a command line.
        /// </summary>
        /// <param name="args">A collection of strings representing the command line arguments.</param>
        /// <param name="baseDirectory">The base directory used for qualifying file locations.</param>
        /// <param name="sdkDirectory">The directory to search for mscorlib, or null if not available.</param>
        /// <param name="additionalReferenceDirectories">A string representing additional reference paths.</param>
        /// <returns>a <see cref="CommandLineArguments"/> object representing the parsed command line.</returns>
        public CommandLineArguments Parse(IEnumerable<string> args, string baseDirectory, string? sdkDirectory, string? additionalReferenceDirectories)
        {
            return CommonParse(args, baseDirectory, sdkDirectory, additionalReferenceDirectories);
        }

        internal static bool IsOptionName(string optionName, ReadOnlyMemory<char> value) =>
            IsOptionName(optionName, value.Span);

        internal static bool IsOptionName(string shortOptionName, string longOptionName, ReadOnlyMemory<char> value) =>
            IsOptionName(shortOptionName, value) || IsOptionName(longOptionName, value);

        /// <summary>
        /// Determines if a <see cref="ReadOnlySpan{Char}"/> is equal to the provided option name
        /// </summary>
        /// <remarks>
        /// Prefer this over the Equals methods on <see cref="ReadOnlySpan{Char}"/>. The 
        /// <see cref="StringComparison.InvariantCultureIgnoreCase"/> implementation allocates a <see cref="String"/>.
        /// The 99% case here is that we are dealing with an ASCII string that matches the input hence
        /// it's worth special casing that here and falling back to the more complicated comparison 
        /// when dealing with non-ASCII input
        /// </remarks>
        internal static bool IsOptionName(string optionName, ReadOnlySpan<char> value)
        {
            assertAllAscii(optionName.AsSpan());

            if (optionName.Length != value.Length)
                return false;

            for (int i = 0; i < optionName.Length; i++)
            {
                char ch = value[i];
                if (ch > 127)
                {
                    // If a non-ascii character is encountered, do an InvariantCultureIgnoreCase comparison
                    return optionName.AsSpan().Equals(value, StringComparison.InvariantCultureIgnoreCase);
                }

                if (optionName[i] != char.ToLowerInvariant(ch))
                {
                    return false;
                }
            }

            return true;

            [Conditional("DEBUG")]
            static void assertAllAscii(ReadOnlySpan<char> span)
            {
                foreach (char ch in span)
                {
                    if (ch > 127)
                    {
                        Debug.Assert(false);
                        break;
                    }
                }
            }
        }

        internal static bool IsOption(string arg) => IsOption(arg.AsSpan());

        internal static bool IsOption(ReadOnlySpan<char> arg) =>
            arg.Length > 0 && (arg[0] == '/' || arg[0] == '-');

        internal static bool IsOption(string optionName, string arg, out ReadOnlyMemory<char> name, out ReadOnlyMemory<char>? value) =>
            TryParseOption(arg, out name, out value) &&
            IsOptionName(optionName, name);

        internal static bool TryParseOption(string arg, [NotNullWhen(true)] out string? name, out string? value)
        {
            if (TryParseOption(arg, out ReadOnlyMemory<char> nameMemory, out ReadOnlyMemory<char>? valueMemory))
            {
                name = nameMemory.ToString().ToLowerInvariant();
                value = valueMemory?.ToString();
                return true;
            }

            name = null;
            value = null;
            return false;
        }

        internal static bool TryParseOption(string arg, out ReadOnlyMemory<char> name, out ReadOnlyMemory<char>? value)
        {
            if (!IsOption(arg))
            {
                name = default;
                value = null;
                return false;
            }

            // handle stdin operator
            if (arg == "-")
            {
                name = arg.AsMemory();
                value = null;
                return true;
            }

            int colon = arg.IndexOf(':', 1);

            // Heuristic to detect Unix-style rooted paths.
            // Patterns like "/goo/*" or "//*" are not treated as compiler options.
            // See https://github.com/dotnet/roslyn/issues/80865 for the MSBuild task
            // that relies on this heuristic.
            if (arg.Length > 1 && arg[0] != '-')
            {
                int separator = colon < 0
                    ? arg.IndexOf('/', 1)
                    : arg.IndexOf('/', 1, colon - 1);

                if (separator > 0)
                {
                    //   "/goo/
                    //   "//
                    name = default;
                    value = null;
                    return false;
                }
            }

            var argMemory = arg.AsMemory();
            if (colon >= 0)
            {
                name = argMemory.Slice(1, colon - 1);
                value = argMemory.Slice(colon + 1);
            }
            else
            {
                name = argMemory.Slice(1);
                value = null;
            }

            return true;
        }

        internal ErrorLogOptions? ParseErrorLogOptions(
            ReadOnlyMemory<char> arg,
            IList<Diagnostic> diagnostics,
            string? baseDirectory,
            out bool diagnosticAlreadyReported)
        {
            diagnosticAlreadyReported = false;

            var parts = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            try
            {
                ParseSeparatedStrings(arg, s_pathSeparators, removeEmptyEntries: true, parts);
                if (parts.Count == 0 || parts[0].Length == 0)
                {
                    return null;
                }

                string? path = ParseGenericPathToFile(parts[0].ToString(), diagnostics, baseDirectory);
                if (path is null)
                {
                    // ParseGenericPathToFile already reported the failure, so the caller should not
                    // report its own failure.
                    diagnosticAlreadyReported = true;
                    return null;
                }

                const char ParameterNameValueSeparator = '=';
                SarifVersion sarifVersion = SarifVersion.Default;
                if (parts.Count > 1 && parts[1].Length > 0)
                {
                    string part = parts[1].ToString();

                    string versionParameterDesignator = "version" + ParameterNameValueSeparator;
                    int versionParameterDesignatorLength = versionParameterDesignator.Length;

                    if (!(
                            part.Length > versionParameterDesignatorLength &&
                            part.Substring(0, versionParameterDesignatorLength).Equals(versionParameterDesignator, StringComparison.OrdinalIgnoreCase) &&
                            SarifVersionFacts.TryParse(part.Substring(versionParameterDesignatorLength), out sarifVersion)
                        ))
                    {
                        return null;
                    }
                }

                if (parts.Count > 2)
                {
                    return null;
                }

                return new ErrorLogOptions(path, sarifVersion);
            }
            finally
            {
                parts.Free();
            }
        }

        internal static void ParseAndNormalizeFile(
            string unquoted,
            string? baseDirectory,
            out string? outputFileName,
            out string? outputDirectory,
            out string invalidPath)
        {
            outputFileName = null;
            outputDirectory = null;
            invalidPath = unquoted;

            string? resolvedPath = FileUtilities.ResolveRelativePath(unquoted, baseDirectory);
            if (resolvedPath != null)
            {
                try
                {
                    // Windows 10 and earlier placed restrictions on file names that originally appeared as device 
                    // names. For example COM1, PRN, CON, AUX, etc ... Files could not be created with those names even 
                    // with extensions like .txt. When those restricted names are passed to GetFullPath the 
                    // runtime will escape them with \\.\. For example GetFullPath("aux.txt") will return "\\.\aux.txt".
                    // The compiler detects these illegal names and bails out early
                    //
                    // Windows 11 removed this restriction though and hence the names are now legal. Cannot find documentation
                    // to support this but experimentally it can be validated. 
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
        [return: NotNullIfNotNull(nameof(path))]
        internal static string? RemoveTrailingSpacesAndDots(string? path)
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
            if (pathMap.IsEmpty())
            {
                return ImmutableArray<KeyValuePair<string, string>>.Empty;
            }

            var pathMapBuilder = ArrayBuilder<KeyValuePair<string, string>>.GetInstance();

            foreach (var kEqualsV in SplitWithDoubledSeparatorEscaping(pathMap, ','))
            {
                if (kEqualsV.IsEmpty())
                {
                    continue;
                }

                var kv = SplitWithDoubledSeparatorEscaping(kEqualsV, '=');
                if (kv.Length != 2)
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.ERR_InvalidPathMap));
                    continue;
                }

                var from = kv[0];
                var to = kv[1];

                if (from.Length == 0 || to.Length == 0)
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.ERR_InvalidPathMap));
                }
                else
                {
                    from = PathUtilities.EnsureTrailingSeparator(from);
                    to = PathUtilities.EnsureTrailingSeparator(to);
                    pathMapBuilder.Add(new KeyValuePair<string, string>(from, to));
                }
            }

            return pathMapBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Splits specified <paramref name="str"/> on <paramref name="separator"/>
        /// treating two consecutive separators as if they were a single non-separating character.
        /// E.g. "a,,b,c" split on ',' yields ["a,b", "c"].
        /// </summary>
        internal static string[] SplitWithDoubledSeparatorEscaping(string str, char separator)
        {
            if (str.Length == 0)
            {
                return Array.Empty<string>();
            }

            var result = ArrayBuilder<string>.GetInstance();
            var pooledPart = PooledStringBuilder.GetInstance();
            var part = pooledPart.Builder;

            int i = 0;
            while (i < str.Length)
            {
                char c = str[i++];
                if (c == separator)
                {
                    if (i < str.Length && str[i] == separator)
                    {
                        i++;
                    }
                    else
                    {
                        result.Add(part.ToString());
                        part.Clear();
                        continue;
                    }
                }

                part.Append(c);
            }

            result.Add(part.ToString());

            pooledPart.Free();
            return result.ToArrayAndFree();
        }

        internal void ParseOutputFile(
            string value,
            IList<Diagnostic> errors,
            string? baseDirectory,
            out string? outputFileName,
            out string? outputDirectory)
        {
            string unquoted = RemoveQuotesAndSlashes(value);
            ParseAndNormalizeFile(unquoted, baseDirectory, out outputFileName, out outputDirectory, out string? invalidPath);
            if (outputFileName == null ||
                !MetadataHelpers.IsValidAssemblyOrModuleName(outputFileName))
            {
                errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InvalidInputFileName, invalidPath));
                outputFileName = null;
                outputDirectory = baseDirectory;
            }
        }

        internal string? ParsePdbPath(
            string value,
            IList<Diagnostic> errors,
            string? baseDirectory)
        {
            string? pdbPath = null;

            string unquoted = RemoveQuotesAndSlashes(value);
            ParseAndNormalizeFile(unquoted, baseDirectory, out string? outputFileName, out string? outputDirectory, out string? invalidPath);
            if (outputFileName == null ||
                PathUtilities.ChangeExtension(outputFileName, extension: null).Length == 0)
            {
                errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InvalidInputFileName, invalidPath));
            }
            else
            {
                // If outputDirectory were null, then outputFileName would be null (see ParseAndNormalizeFile)
                Debug.Assert(outputDirectory is object);
                pdbPath = Path.ChangeExtension(Path.Combine(outputDirectory, outputFileName), ".pdb");
            }

            return pdbPath;
        }

        internal string? ParseGenericPathToFile(
            string unquoted,
            IList<Diagnostic> errors,
            string? baseDirectory,
            bool generateDiagnostic = true)
        {
            string? genericPath = null;

            ParseAndNormalizeFile(unquoted, baseDirectory, out string? outputFileName, out string? outputDirectory, out string? invalidPath);
            if (string.IsNullOrWhiteSpace(outputFileName))
            {
                if (generateDiagnostic)
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InvalidInputFileName, invalidPath));
                }
            }
            else
            {
                // If outputDirectory were null, then outputFileName would be null (see ParseAndNormalizeFile)
                genericPath = Path.Combine(outputDirectory!, outputFileName);
            }

            return genericPath;
        }

        internal void FlattenArgs(
            IEnumerable<string> rawArguments,
            IList<Diagnostic> diagnostics,
            ArrayBuilder<string> processedArgs,
            List<string>? scriptArgsOpt,
            string? baseDirectory,
            List<string>? responsePaths = null)
        {
            bool parsingScriptArgs = false;
            bool sourceFileSeen = false;
            bool optionsEnded = false;

            foreach (string arg in rawArguments)
            {
                processArg(arg);
            }

            void processArg(string arg)
            {
                // EDMAURER trim off whitespace. Otherwise behavioral differences arise
                // when the strings which represent args are constructed by cmd or users.
                // cmd won't produce args with whitespace at the end.
                arg = arg.TrimEnd();

                if (parsingScriptArgs)
                {
                    scriptArgsOpt!.Add(arg);
                    return;
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
                        return;
                    }

                    if (!optionsEnded && arg == "--")
                    {
                        // csi/vbi: no argument past "--" should be treated as an option/response file
                        optionsEnded = true;
                        processedArgs.Add(arg);
                        return;
                    }
                }

                if (!optionsEnded && arg.StartsWith("@", StringComparison.Ordinal))
                {
                    // response file:
                    string path = RemoveQuotesAndSlashes(arg.Substring(1)).TrimEnd(null);
                    string? resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                    if (resolvedPath != null)
                    {
                        parseResponseFile(resolvedPath);

                        if (responsePaths != null)
                        {
                            string? directory = PathUtilities.GetDirectoryName(resolvedPath);
                            if (directory is null)
                            {
                                diagnostics.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InvalidInputFileName, path));
                            }
                            else
                            {
                                responsePaths.Add(FileUtilities.NormalizeAbsolutePath(directory));
                            }
                        }
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InvalidInputFileName, path));
                    }
                }
                else
                {
                    processedArgs.Add(arg);
                    sourceFileSeen |= optionsEnded || !IsOption(arg);
                }
            }

            void parseResponseFile(string fullPath)
            {
                var stringBuilder = PooledStringBuilder.GetInstance();
                var splitList = new List<string>();

                try
                {
                    Debug.Assert(PathUtilities.IsAbsolute(fullPath));
                    using TextReader reader = CreateTextFileReader(fullPath);
                    Span<char> lineBuffer = stackalloc char[256];
                    var lineBufferLength = 0;
                    while (true)
                    {
                        var ch = reader.Read();
                        if (ch == -1)
                        {
                            if (lineBufferLength > 0)
                            {
                                stringBuilder.Builder.Length = 0;
                                CommandLineUtilities.SplitCommandLineIntoArguments(
                                    lineBuffer.Slice(0, lineBufferLength),
                                    removeHashComments: true,
                                    stringBuilder.Builder,
                                    splitList,
                                    out _);
                            }
                            break;
                        }

                        if (ch is '\r' or '\n')
                        {
                            if (ch is '\r' && reader.Peek() == '\n')
                            {
                                reader.Read();
                            }

                            stringBuilder.Builder.Length = 0;
                            CommandLineUtilities.SplitCommandLineIntoArguments(
                                lineBuffer.Slice(0, lineBufferLength),
                                removeHashComments: true,
                                stringBuilder.Builder,
                                splitList,
                                out _);
                            lineBufferLength = 0;
                        }
                        else
                        {
                            if (lineBufferLength >= lineBuffer.Length)
                            {
                                var temp = new char[lineBuffer.Length * 2];
                                lineBuffer.CopyTo(temp.AsSpan());
                                lineBuffer = temp;
                            }

                            lineBuffer[lineBufferLength] = (char)ch;
                            lineBufferLength++;
                        }
                    }
                }
                catch (Exception)
                {
                    diagnostics.Add(Diagnostic.Create(_messageProvider, _messageProvider.ERR_OpenResponseFile, fullPath));
                    return;
                }

                foreach (var newArg in splitList)
                {
                    // Ignores /noconfig option specified in a response file
                    if (!string.Equals(newArg, "/noconfig", StringComparison.OrdinalIgnoreCase) && !string.Equals(newArg, "-noconfig", StringComparison.OrdinalIgnoreCase))
                    {
                        processArg(newArg);
                    }
                    else
                    {
                        diagnostics.Add(Diagnostic.Create(_messageProvider, _messageProvider.WRN_NoConfigNotOnCommandLine));
                    }
                }

                stringBuilder.Free();
            }
        }

        internal static IEnumerable<string> ParseResponseLines(IEnumerable<string> lines)
        {
            var arguments = new List<string>();
            foreach (string line in lines)
            {
                arguments.AddRange(CommandLineUtilities.SplitCommandLineIntoArguments(line, removeHashComments: true));
            }

            return arguments;
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
        /// <param name="pipeName">
        /// Only specified if <paramref name="containsShared"/> is true and the session key
        /// was provided.  Can be null
        /// </param>
        internal static bool TryParseClientArgs(
            IEnumerable<string> args,
            [NotNullWhen(true)] out List<string>? parsedArgs,
            out bool containsShared,
            out string? keepAliveValue,
            out string? pipeName,
            [NotNullWhen(false)] out string? errorMessage)
        {
            containsShared = false;
            keepAliveValue = null;
            errorMessage = null;
            parsedArgs = null;
            pipeName = null;
            var newArgs = new List<string>();
            foreach (var arg in args)
            {
                if (isClientArgsOption(arg, "keepalive", out bool hasValue, out string? value))
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        errorMessage = CodeAnalysisResources.MissingKeepAlive;
                        return false;
                    }

                    if (int.TryParse(value, out int intValue))
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

                if (isClientArgsOption(arg, "shared", out hasValue, out value))
                {
                    if (hasValue)
                    {
                        if (string.IsNullOrEmpty(value))
                        {
                            errorMessage = CodeAnalysisResources.SharedArgumentMissing;
                            return false;
                        }

                        pipeName = value;
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

            static bool isClientArgsOption(string arg, string optionName, out bool hasValue, out string? optionValue)
            {
                hasValue = false;
                optionValue = null;

                if (arg.Length == 0 || !(arg[0] == '/' || arg[0] == '-'))
                {
                    return false;
                }

                arg = arg.Substring(1);
                if (!arg.StartsWith(optionName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (arg.Length > optionName.Length)
                {
                    if (!(arg[optionName.Length] == ':' || arg[optionName.Length] == '='))
                    {
                        return false;
                    }

                    hasValue = true;
                    optionValue = arg.Substring(optionName.Length + 1).Trim('"');
                }

                return true;
            }
        }

        internal static string MismatchedVersionErrorText => CodeAnalysisResources.MismatchedVersion;

        private static readonly char[] s_resourceSeparators = { ',' };

        internal static bool TryParseResourceDescription(
            ReadOnlyMemory<char> resourceDescriptor,
            string? baseDirectory,
            bool skipLeadingSeparators,   // VB does this
            bool allowEmptyAccessibility, // VB does this
            [NotNullWhen(true)] out string? filePath,
            [NotNullWhen(true)] out string? fullPath,
            [NotNullWhen(true)] out string? fileName,
            [NotNullWhen(true)] out string? resourceName,
            [NotNullWhen(true)] out bool? isPublic,
            out string? rawAccessibility)
        {
            filePath = null;
            fullPath = null;
            fileName = null;
            resourceName = null;
            isPublic = null;
            rawAccessibility = null;

            // resource descriptor is: "<filePath>[,<string name>[,public|private]]"
            var parts = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            ParseSeparatedStrings(resourceDescriptor, s_resourceSeparators, removeEmptyEntries: false, parts);

            int offset = 0;

            int length = parts.Count;

            if (skipLeadingSeparators)
            {
                for (; offset < length && parts[offset].Length == 0; offset++)
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
                rawAccessibility = RemoveQuotesAndSlashes(parts[offset + 2]);
            }

            if (rawAccessibility == null || rawAccessibility == "" && allowEmptyAccessibility)
            {
                // If no accessibility is given, we default to "public".
                // NOTE: Dev10 distinguishes between null and empty.
                isPublic = true;
            }
            else if (string.Equals(rawAccessibility, "public", StringComparison.OrdinalIgnoreCase))
            {
                isPublic = true;
            }
            else if (string.Equals(rawAccessibility, "private", StringComparison.OrdinalIgnoreCase))
            {
                isPublic = false;
            }
            else
            {
                isPublic = null;
            }

            parts.Free();

            if (isPublic == null || RoslynString.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            fileName = PathUtilities.GetFileName(filePath);
            fullPath = FileUtilities.ResolveRelativePath(filePath, baseDirectory);
            if (!PathUtilities.IsValidFilePath(fullPath))
            {
                return false;
            }

            // The default resource name is the file name.
            // Also use the file name for the name when user specifies string like "filePath,,private"
            if (RoslynString.IsNullOrWhiteSpace(resourceName))
            {
                resourceName = fileName;
            }

            return true;
        }

        /// <summary>
        /// See <see cref="CommandLineUtilities.SplitCommandLineIntoArguments(string, bool)"/> 
        /// </summary>
        public static IEnumerable<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments)
        {
            return CommandLineUtilities.SplitCommandLineIntoArguments(commandLine, removeHashComments);
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
        [return: NotNullIfNotNull(parameterName: nameof(arg))]
        internal static string? RemoveQuotesAndSlashes(string? arg) =>
            arg is not null
                ? RemoveQuotesAndSlashes(arg.AsMemory())
                : null;

        internal static string RemoveQuotesAndSlashes(ReadOnlyMemory<char> argMemory) =>
            RemoveQuotesAndSlashesEx(argMemory).ToString();

        internal static string? RemoveQuotesAndSlashes(ReadOnlyMemory<char>? argMemory) =>
            argMemory is { } m
                ? RemoveQuotesAndSlashesEx(m).ToString()
                : null;

        internal static ReadOnlyMemory<char>? RemoveQuotesAndSlashesEx(ReadOnlyMemory<char>? argMemory) =>
            argMemory is { } m
                ? RemoveQuotesAndSlashesEx(m)
                : null;

        internal static ReadOnlyMemory<char> RemoveQuotesAndSlashesEx(ReadOnlyMemory<char> argMemory)
        {
            if (removeFastPath(argMemory) is { } m)
            {
                return m;
            }

            var pool = PooledStringBuilder.GetInstance();
            var builder = pool.Builder;
            var arg = argMemory.Span;
            var i = 0;
            while (i < arg.Length)
            {
                var cur = arg[i];
                switch (cur)
                {
                    case '\\':
                        processSlashes(builder, arg, ref i);
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

            return pool.ToStringAndFree().AsMemory();

            // Mimic behavior of the native function by the same name.
            static void processSlashes(StringBuilder builder, ReadOnlySpan<char> arg, ref int i)
            {
                RoslynDebug.Assert(arg != null);
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

            // The 99% case when using MSBuild is that at worst a path has quotes at the start and 
            // end of the string but no where else. When that happens there is no need to allocate 
            // a new string here and instead we can just do a simple Slice on the existing 
            // ReadOnlyMemory object.
            //
            // This removes one of the largest allocation paths during command line parsing
            static ReadOnlyMemory<char>? removeFastPath(ReadOnlyMemory<char> arg)
            {
                int start = 0;
                int end = arg.Length;
                var span = arg.Span;

                while (end > 0 && span[end - 1] == '"')
                {
                    end--;
                }

                while (start < end && span[start] == '"')
                {
                    start++;
                }

                for (int i = start; i < end; i++)
                {
                    if (span[i] == '"')
                    {
                        return null;
                    }
                }

                return arg.Slice(start, end - start);
            }
        }

        private static readonly char[] s_pathSeparators = { ';', ',' };
        private static readonly char[] s_wildcards = new[] { '*', '?' };

        internal static IEnumerable<string> ParseSeparatedPaths(string arg)
        {
            var builder = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            ParseSeparatedPathsEx(arg.AsMemory(), builder);
            return builder.ToArrayAndFree().Select(static x => x.ToString());
        }

        internal static void ParseSeparatedPathsEx(ReadOnlyMemory<char>? str, ArrayBuilder<ReadOnlyMemory<char>> builder)
        {
            ParseSeparatedStrings(str, s_pathSeparators, removeEmptyEntries: true, builder);
            for (var i = 0; i < builder.Count; i++)
            {
                builder[i] = RemoveQuotesAndSlashesEx(builder[i]);
            }
        }

        /// <summary>
        /// Split a string by a set of separators, taking quotes into account.
        /// </summary>
        internal static void ParseSeparatedStrings(ReadOnlyMemory<char>? strMemory, char[] separators, bool removeEmptyEntries, ArrayBuilder<ReadOnlyMemory<char>> builder)
        {
            if (strMemory is null)
            {
                return;
            }

            int nextPiece = 0;
            var inQuotes = false;
            var memory = strMemory.Value;
            var span = memory.Span;
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes && separators.Contains(c))
                {
                    var current = memory.Slice(nextPiece, i - nextPiece);
                    if (current.Length > 0 || !removeEmptyEntries)
                    {
                        builder.Add(current);
                    }

                    nextPiece = i + 1;
                }
            }

            var last = memory.Slice(nextPiece);
            if (last.Length > 0 || !removeEmptyEntries)
            {
                builder.Add(last);
            }
        }

        internal IEnumerable<string> ResolveRelativePaths(IEnumerable<string> paths, string baseDirectory, IList<Diagnostic> errors)
        {
            foreach (var path in paths)
            {
                string? resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                if (resolvedPath == null)
                {
                    errors.Add(Diagnostic.Create(_messageProvider, _messageProvider.FTL_InvalidInputFileName, path));
                }
                else
                {
                    yield return resolvedPath;
                }
            }
        }

        private protected CommandLineSourceFile ToCommandLineSourceFile(string resolvedPath, bool isInputRedirected = false)
        {
            bool isScriptFile;
            if (IsScriptCommandLineParser)
            {
                ReadOnlyMemory<char> extension = PathUtilities.GetExtension(resolvedPath.AsMemory());
                isScriptFile = !extension.Span.Equals(RegularFileExtension.AsSpan(), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // TODO: uncomment when fixing https://github.com/dotnet/roslyn/issues/5325
                //isScriptFile = string.Equals(extension, ScriptFileExtension, StringComparison.OrdinalIgnoreCase);
                isScriptFile = false;
            }

            return new CommandLineSourceFile(resolvedPath, isScriptFile, isInputRedirected);
        }

        internal void ParseFileArgument(ReadOnlyMemory<char> arg, string? baseDirectory, ArrayBuilder<string> filePathBuilder, IList<Diagnostic> errors)
        {
            Debug.Assert(IsScriptCommandLineParser || !arg.StartsWith('-') && !arg.StartsWith('@'));

            // We remove all doubles quotes from a file name. So that, for example:
            //   "Path With Spaces"\goo.cs
            // becomes
            //   Path With Spaces\goo.cs

            string path = RemoveQuotesAndSlashes(arg);
            int wildcard = path.IndexOfAny(s_wildcards);
            if (wildcard != -1)
            {
                foreach (var file in ExpandFileNamePattern(path, baseDirectory, SearchOption.TopDirectoryOnly, errors))
                {
                    filePathBuilder.Add(file);
                }
            }
            else
            {
                string? resolvedPath = FileUtilities.ResolveRelativePath(path, baseDirectory);
                if (resolvedPath == null)
                {
                    errors.Add(Diagnostic.Create(MessageProvider, (int)MessageProvider.FTL_InvalidInputFileName, path));
                }
                else
                {
                    filePathBuilder.Add(resolvedPath);
                }
            }
        }

        private protected void ParseSeparatedFileArgument(ReadOnlyMemory<char> value, string? baseDirectory, ArrayBuilder<string> filePathBuilder, IList<Diagnostic> errors)
        {
            var pathBuilder = ArrayBuilder<ReadOnlyMemory<char>>.GetInstance();
            ParseSeparatedPathsEx(value, pathBuilder);
            foreach (ReadOnlyMemory<char> path in pathBuilder)
            {
                if (path.IsWhiteSpace())
                {
                    continue;
                }

                ParseFileArgument(path, baseDirectory, filePathBuilder, errors);
            }
            pathBuilder.Free();
        }

        private protected IEnumerable<string> ParseSeparatedFileArgument(string value, string? baseDirectory, IList<Diagnostic> errors)
        {
            var builder = ArrayBuilder<string>.GetInstance();
            ParseSeparatedFileArgument(value.AsMemory(), baseDirectory, builder, errors);
            foreach (var filePath in builder)
            {
                yield return filePath;
            }
            builder.Free();
        }

        internal IEnumerable<CommandLineSourceFile> ParseRecurseArgument(string arg, string? baseDirectory, IList<Diagnostic> errors)
        {
            foreach (var path in ExpandFileNamePattern(arg, baseDirectory, SearchOption.AllDirectories, errors))
            {
                yield return ToCommandLineSourceFile(path);
            }
        }

        internal static Encoding? TryParseEncodingName(string arg)
            => int.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out var codepage) && codepage > 0
                ? EncodedStringText.TryGetCodePageEncoding(codepage)
                : null;

        private IEnumerable<string> ExpandFileNamePattern(
            string path,
            string? baseDirectory,
            SearchOption searchOption,
            IList<Diagnostic> errors)
        {
            string? directory = PathUtilities.GetDirectoryName(path);
            string pattern = PathUtilities.GetFileName(path);

            var resolvedDirectoryPath = string.IsNullOrEmpty(directory) ?
                baseDirectory :
                FileUtilities.ResolveRelativePath(directory, baseDirectory);

            IEnumerator<string>? enumerator = null;
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
                        string? resolvedPath = null;
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
                            errors.Add(Diagnostic.Create(MessageProvider, (int)MessageProvider.FTL_InvalidInputFileName, path));
                            break;
                        }

                        yielded = true;
                        yield return resolvedPath;
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

        internal ReportDiagnostic GetDiagnosticOptionsFromRulesetFile(string? fullPath, out Dictionary<string, ReportDiagnostic> diagnosticOptions, IList<Diagnostic> diagnostics)
        {
            return RuleSet.GetDiagnosticOptionsFromRulesetFile(fullPath, out diagnosticOptions, diagnostics, _messageProvider);
        }

        /// <summary>
        /// Tries to parse a UInt64 from string in either decimal, octal or hex format.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="result">The result if parsing was successful.</param>
        /// <returns>true if parsing was successful, otherwise false.</returns>
        internal static bool TryParseUInt64(string? value, out ulong result)
        {
            result = 0;

            if (RoslynString.IsNullOrEmpty(value))
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
        internal static bool TryParseUInt16(string? value, out ushort result)
        {
            result = 0;

            if (RoslynString.IsNullOrEmpty(value))
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

        internal static ImmutableDictionary<string, string> ParseFeatures(List<string> features)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            CompilerOptionParseUtilities.ParseFeatures(builder, features);
            return builder.ToImmutable();
        }

        /// <summary>
        /// Sort so that more specific keys precede less specific.
        /// When mapping a path we find the first key in the array that is a prefix of the path.
        /// If multiple keys are prefixes of the path we want to use the longest (more specific) one for the mapping.
        /// </summary>
        internal static ImmutableArray<KeyValuePair<string, string>> SortPathMap(ImmutableArray<KeyValuePair<string, string>> pathMap)
            => pathMap.Sort((x, y) => -x.Key.Length.CompareTo(y.Key.Length));
    }
}
