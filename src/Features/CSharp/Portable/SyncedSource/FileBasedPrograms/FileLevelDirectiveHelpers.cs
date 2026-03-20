// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.ProjectTools;

namespace Microsoft.DotNet.FileBasedPrograms;

internal static class FileLevelDirectiveHelpers
{
    public static SyntaxTokenParser CreateTokenizer(SourceText text)
    {
        return SyntaxFactory.CreateTokenParser(text,
            CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]));
    }

    /// <param name="reportAllErrors">
    /// If <see langword="true"/>, the whole <paramref name="sourceFile"/> is parsed to find diagnostics about every app directive.
    /// Otherwise, only directives up to the first C# token is checked.
    /// The former is useful for <c>dotnet project convert</c> where we want to report all errors because it would be difficult to fix them up after the conversion.
    /// The latter is useful for <c>dotnet run file.cs</c> where if there are app directives after the first token,
    /// compiler reports <see cref="ErrorCode.ERR_PPIgnoredFollowsToken"/> anyway, so we speed up success scenarios by not parsing the whole file up front in the SDK CLI.
    /// </param>
    public static ImmutableArray<CSharpDirective> FindDirectives(SourceFile sourceFile, bool reportAllErrors, ErrorReporter errorReporter)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpDirective>();
        var tokenizer = CreateTokenizer(sourceFile.Text);

        var result = tokenizer.ParseLeadingTrivia();
        var triviaList = result.Token.LeadingTrivia;

        FindLeadingDirectives(sourceFile, triviaList, errorReporter, builder);

        // In conversion mode, we want to report errors for any invalid directives in the rest of the file
        // so users don't end up with invalid directives in the converted project.
        if (reportAllErrors)
        {
            tokenizer.ResetTo(result);

            do
            {
                result = tokenizer.ParseNextToken();

                foreach (var trivia in result.Token.LeadingTrivia)
                {
                    ReportErrorFor(trivia);
                }

                foreach (var trivia in result.Token.TrailingTrivia)
                {
                    ReportErrorFor(trivia);
                }
            }
            while (!result.Token.IsKind(SyntaxKind.EndOfFileToken));
        }

        void ReportErrorFor(SyntaxTrivia trivia)
        {
            if (trivia.ContainsDiagnostics && trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                errorReporter(sourceFile.Text, sourceFile.Path, trivia.Span, FileBasedProgramsResources.CannotConvertDirective);
            }
        }

        // The result should be ordered by source location, RemoveDirectivesFromFile depends on that.
        return builder.ToImmutable();
    }

    /// <summary>Finds file-level directives in the leading trivia list of a compilation unit and reports diagnostics on them.</summary>
    /// <param name="builder">The builder to store the parsed directives in, or null if the parsed directives are not needed.</param>
    public static void FindLeadingDirectives(
        SourceFile sourceFile,
        SyntaxTriviaList triviaList,
        ErrorReporter errorReporter,
        ImmutableArray<CSharpDirective>.Builder? builder)
    {
        var deduplicated = new Dictionary<CSharpDirective.Named, CSharpDirective.Named>(NamedDirectiveComparer.Instance);
        TextSpan previousWhiteSpaceSpan = default;

        for (var index = 0; index < triviaList.Count; index++)
        {
            var trivia = triviaList[index];
            // Stop when the trivia contains an error (e.g., because it's after #if).
            if (trivia.ContainsDiagnostics)
            {
                break;
            }

            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                Debug.Assert(previousWhiteSpaceSpan.IsEmpty);
                previousWhiteSpaceSpan = trivia.FullSpan;
                continue;
            }

            if (trivia.IsKind(SyntaxKind.ShebangDirectiveTrivia))
            {
                TextSpan span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                var whiteSpace = GetWhiteSpaceInfo(triviaList, index);
                var info = new CSharpDirective.ParseInfo
                {
                    SourceFile = sourceFile,
                    Span = span,
                    LeadingWhiteSpace = whiteSpace.Leading,
                    TrailingWhiteSpace = whiteSpace.Trailing,
                };
                builder?.Add(new CSharpDirective.Shebang(info));
            }
            else if (trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                TextSpan span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                var message = trivia.GetStructure() is IgnoredDirectiveTriviaSyntax { Content: { RawKind: (int)SyntaxKind.StringLiteralToken } content }
                    ? content.Text.AsSpan().Trim()
                    : "";
                var parts = Patterns.Whitespace.Split(message.ToString(), 2);
                var name = parts.Length > 0 ? parts[0] : "";
                var value = parts.Length > 1 ? parts[1] : "";
                Debug.Assert(!(parts.Length > 2));

                var whiteSpace = GetWhiteSpaceInfo(triviaList, index);
                var context = new CSharpDirective.ParseContext
                {
                    Info = new()
                    {
                        SourceFile = sourceFile,
                        Span = span,
                        LeadingWhiteSpace = whiteSpace.Leading,
                        TrailingWhiteSpace = whiteSpace.Trailing,
                    },
                    ErrorReporter = errorReporter,
                    DirectiveKind = name,
                    DirectiveText = value,
                };

                // Block quotes now so we can later support quoted values without a breaking change. https://github.com/dotnet/sdk/issues/49367
                if (value.Contains('"'))
                {
                    context.ReportError(FileBasedProgramsResources.QuoteInDirective);
                }

                if (CSharpDirective.Parse(context) is { } directive)
                {
                    // If the directive is already present, report an error.
                    if (deduplicated.TryGetValue(directive, out var existingDirective))
                    {
                        var typeAndName = $"#:{existingDirective.GetType().Name.ToLowerInvariant()} {existingDirective.Name}";
                        context.ReportError(directive.Info.Span, string.Format(FileBasedProgramsResources.DuplicateDirective, typeAndName));
                    }
                    else
                    {
                        deduplicated.Add(directive, directive);
                    }

                    builder?.Add(directive);
                }
            }

            previousWhiteSpaceSpan = default;
        }

        return;

        static TextSpan GetFullSpan(TextSpan previousWhiteSpaceSpan, SyntaxTrivia trivia)
        {
            // Include the preceding whitespace in the span, i.e., span will be the whole line.
            return previousWhiteSpaceSpan.IsEmpty ? trivia.FullSpan : TextSpan.FromBounds(previousWhiteSpaceSpan.Start, trivia.FullSpan.End);
        }

        static (WhiteSpaceInfo Leading, WhiteSpaceInfo Trailing) GetWhiteSpaceInfo(in SyntaxTriviaList triviaList, int index)
        {
            (WhiteSpaceInfo Leading, WhiteSpaceInfo Trailing) result = default;

            for (int i = index - 1; i >= 0; i--)
            {
                if (!Fill(ref result.Leading, triviaList, i)) break;
            }

            for (int i = index + 1; i < triviaList.Count; i++)
            {
                if (!Fill(ref result.Trailing, triviaList, i)) break;
            }

            return result;

            static bool Fill(ref WhiteSpaceInfo info, in SyntaxTriviaList triviaList, int index)
            {
                var trivia = triviaList[index];
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    info.LineBreaks += 1;
                    info.TotalLength += trivia.FullSpan.Length;
                    return true;
                }

                if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                {
                    info.TotalLength += trivia.FullSpan.Length;
                    return true;
                }

                return false;
            }
        }
    }
}

internal readonly record struct SourceFile(string Path, SourceText Text)
{
    public static SourceFile Load(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        // Let SourceText.From auto-detect the encoding (including BOM detection)
        return new SourceFile(filePath, SourceText.From(stream, encoding: null));
    }

    public void Save()
    {
        using var stream = File.Open(Path, FileMode.Create, FileAccess.Write);
        // Use the encoding from SourceText, which preserves the original BOM state
        var encoding = Text.Encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        using var writer = new StreamWriter(stream, encoding);
        Text.Write(writer);
    }

    public string GetLocationString(TextSpan span)
    {
        return $"{Path}({Text.Lines.GetLinePositionSpan(span).Start.Line + 1})";
    }
}

internal static partial class Patterns
{
    public static Regex Whitespace { get; } = new Regex("""\s+""", RegexOptions.Compiled);

    public static Regex DisallowedNameCharacters { get; } = new Regex("""[\s@=/]""", RegexOptions.Compiled);

    public static Regex EscapedCompilerOption { get; } = new Regex("""^/\w+:".*"$""", RegexOptions.Compiled | RegexOptions.Singleline);
}

internal struct WhiteSpaceInfo
{
    public int LineBreaks;
    public int TotalLength;
}

/// <summary>
/// Represents a C# directive starting with <c>#:</c> (a.k.a., "file-level directive").
/// Those are ignored by the language but recognized by us.
/// </summary>
internal abstract class CSharpDirective(in CSharpDirective.ParseInfo info)
{
    public ParseInfo Info { get; } = info;

    public readonly struct ParseInfo
    {
        public required SourceFile SourceFile { get; init; }
        /// <summary>
        /// Span of the full line including the trailing line break.
        /// </summary>
        public required TextSpan Span { get; init; }
        public required WhiteSpaceInfo LeadingWhiteSpace { get; init; }
        public required WhiteSpaceInfo TrailingWhiteSpace { get; init; }
    }

    public readonly struct ParseContext
    {
        public required ParseInfo Info { get; init; }
        public required ErrorReporter ErrorReporter { get; init; }
        public required string DirectiveKind { get; init; }
        public required string DirectiveText { get; init; }

        public void ReportError(string message)
            => ErrorReporter(Info.SourceFile.Text, Info.SourceFile.Path, Info.Span, message);

        public void ReportError(TextSpan span, string message)
            => ErrorReporter(Info.SourceFile.Text, Info.SourceFile.Path, span, message);
    }

    public static Named? Parse(in ParseContext context)
    {
        switch (context.DirectiveKind)
        {
            case "sdk": return Sdk.Parse(context);
            case "property": return Property.Parse(context);
            case "package": return Package.Parse(context);
            case "project": return Project.Parse(context);
            case "include" or "exclude": return IncludeOrExclude.Parse(context);
            default:
                context.ReportError(string.Format(FileBasedProgramsResources.UnrecognizedDirective, context.DirectiveKind));
                return null;
        }
    }

    private static (string, string?)? ParseOptionalTwoParts(in ParseContext context, char separator)
    {
        var separatorIndex = context.DirectiveText.IndexOf(separator);
        var firstPart = (separatorIndex < 0 ? context.DirectiveText : context.DirectiveText.AsSpan(0, separatorIndex)).TrimEnd();

        string directiveKind = context.DirectiveKind;
        if (firstPart.IsWhiteSpace())
        {
            context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, directiveKind));
            return null;
        }

        // If the name contains characters that resemble separators, report an error to avoid any confusion.
        if (Patterns.DisallowedNameCharacters.Match(context.DirectiveText, beginning: 0, length: firstPart.Length).Success)
        {
            context.ReportError(string.Format(FileBasedProgramsResources.InvalidDirectiveName, directiveKind, separator));
            return null;
        }

        if (separatorIndex < 0)
        {
            return (firstPart.ToString(), null);
        }

        var secondPart = context.DirectiveText.AsSpan(separatorIndex + 1).TrimStart();
        if (secondPart.IsWhiteSpace())
        {
            Debug.Assert(secondPart.Length == 0,
                "We have trimmed the second part, so if it's white space, it should be actually empty.");

            return (firstPart.ToString(), string.Empty);
        }

        return (firstPart.ToString(), secondPart.ToString());
    }

    public abstract override string ToString();

    /// <summary>
    /// <c>#!</c> directive.
    /// </summary>
    public sealed class Shebang(in ParseInfo info) : CSharpDirective(info)
    {
        public override string ToString() => "#!";
    }

    public abstract class Named(in ParseInfo info) : CSharpDirective(info)
    {
        public required string Name { get; init; }
    }

    /// <summary>
    /// <c>#:sdk</c> directive.
    /// </summary>
    public sealed class Sdk(in ParseInfo info) : Named(info)
    {
        public string? Version { get; init; }

        public static new Sdk? Parse(in ParseContext context)
        {
            if (ParseOptionalTwoParts(context, separator: '@') is not var (sdkName, sdkVersion))
            {
                return null;
            }

            return new Sdk(context.Info)
            {
                Name = sdkName,
                Version = sdkVersion,
            };
        }

        public override string ToString() => Version is null ? $"#:sdk {Name}" : $"#:sdk {Name}@{Version}";
    }

    /// <summary>
    /// <c>#:property</c> directive.
    /// </summary>
    public sealed class Property(in ParseInfo info) : Named(info)
    {
        public required string Value { get; init; }

        public static new Property? Parse(in ParseContext context)
        {
            if (ParseOptionalTwoParts(context, separator: '=') is not var (propertyName, propertyValue))
            {
                return null;
            }

            if (propertyValue is null)
            {
                context.ReportError(FileBasedProgramsResources.PropertyDirectiveMissingParts);
                return null;
            }

            try
            {
                propertyName = XmlConvert.VerifyName(propertyName);
            }
            catch (XmlException ex)
            {
                context.ReportError(string.Format(FileBasedProgramsResources.PropertyDirectiveInvalidName, ex.Message));
                return null;
            }

            if (propertyName.Equals("RestoreUseStaticGraphEvaluation", StringComparison.OrdinalIgnoreCase) &&
                MSBuildUtilities.ConvertStringToBool(propertyValue))
            {
                context.ReportError(FileBasedProgramsResources.StaticGraphRestoreNotSupported);
            }

            return new Property(context.Info)
            {
                Name = propertyName,
                Value = propertyValue,
            };
        }

        public override string ToString() => $"#:property {Name}={Value}";
    }

    /// <summary>
    /// <c>#:package</c> directive.
    /// </summary>
    public sealed class Package(in ParseInfo info) : Named(info)
    {
        public string? Version { get; init; }

        public static new Package? Parse(in ParseContext context)
        {
            if (ParseOptionalTwoParts(context, separator: '@') is not var (packageName, packageVersion))
            {
                return null;
            }

            return new Package(context.Info)
            {
                Name = packageName,
                Version = packageVersion,
            };
        }

        public override string ToString() => Version is null ? $"#:package {Name}" : $"#:package {Name}@{Version}";
    }

    /// <summary>
    /// <c>#:project</c> directive.
    /// </summary>
    public sealed class Project : Named
    {
        [SetsRequiredMembers]
        public Project(in ParseInfo info, string name) : base(info)
        {
            Name = name;
            OriginalName = name;
        }

        /// <summary>
        /// Preserved across <see cref="WithName"/> calls, i.e.,
        /// this is the original directive text as entered by the user.
        /// </summary>
        public string OriginalName { get; init; }

        /// <summary>
        /// This is the <see cref="OriginalName"/> with MSBuild <c>$(..)</c> vars expanded.
        /// E.g. The expansion might be implemented via ProjectInstance.ExpandString.
        /// </summary>
        public string? ExpandedName { get; init; }

        /// <summary>
        /// This is the <see cref="ExpandedName"/> resolved via <see cref="EnsureProjectFilePath"/>
        /// (i.e., this is a file path if the original text pointed to a directory).
        /// </summary>
        public string? ProjectFilePath { get; init; }

        public static new Project? Parse(in ParseContext context)
        {
            var directiveText = context.DirectiveText;
            if (directiveText.IsWhiteSpace())
            {
                context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, context.DirectiveKind));
                return null;
            }

            return new Project(context.Info, directiveText);
        }

        public enum NameKind
        {
            /// <summary>
            /// Change <see cref="Named.Name"/> and <see cref="ExpandedName"/>.
            /// </summary>
            Expanded = 1,

            /// <summary>
            /// Change <see cref="Named.Name"/> and <see cref="Project.ProjectFilePath"/>.
            /// </summary>
            ProjectFilePath = 2,

            /// <summary>
            /// Change only <see cref="Named.Name"/>.
            /// </summary>
            Final = 3,
        }

        public Project WithName(string name, NameKind kind)
        {
            return new Project(Info, name)
            {
                OriginalName = OriginalName,
                ExpandedName = kind == NameKind.Expanded ? name : ExpandedName,
                ProjectFilePath = kind == NameKind.ProjectFilePath ? name : ProjectFilePath,
            };
        }

        /// <summary>
        /// If the directive points to a directory, returns a new directive pointing to the corresponding project file.
        /// </summary>
        public Project EnsureProjectFilePath(ErrorReporter errorReporter)
        {
            var resolvedName = Name;
            var sourcePath = Info.SourceFile.Path;

            // If the path is a directory like '../lib', transform it to a project file path like '../lib/lib.csproj'.
            // Also normalize backslashes to forward slashes to ensure the directive works on all platforms.
            var sourceDirectory = Path.GetDirectoryName(sourcePath)
                ?? throw new InvalidOperationException($"Source file path '{sourcePath}' does not have a containing directory.");

            var resolvedProjectPath = Path.Combine(sourceDirectory, resolvedName.Replace('\\', '/'));
            if (Directory.Exists(resolvedProjectPath))
            {
                if (ProjectLocator.TryGetProjectFileFromDirectory(resolvedProjectPath, out var projectFilePath, out var error))
                {
                    // Keep a relative path only if the original directive was a relative path.
                    resolvedName = ExternalHelpers.IsPathFullyQualified(resolvedName)
                        ? projectFilePath
                        : ExternalHelpers.GetRelativePath(relativeTo: sourceDirectory, projectFilePath);
                }
                else
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidProjectDirective, error));
                }
            }
            else if (!File.Exists(resolvedProjectPath))
            {
                ReportError(string.Format(FileBasedProgramsResources.InvalidProjectDirective, string.Format(FileBasedProgramsResources.CouldNotFindProjectOrDirectory, resolvedProjectPath)));
            }

            return WithName(resolvedName, NameKind.ProjectFilePath);

            void ReportError(string message)
                => errorReporter(Info.SourceFile.Text, sourcePath, Info.Span, message);
        }

        public override string ToString() => $"#:project {Name}";
    }

    public enum IncludeOrExcludeKind
    {
        Include,
        Exclude,
    }

    /// <summary>
    /// <c>#:include</c> or <c>#:exclude</c> directive.
    /// </summary>
    public sealed class IncludeOrExclude(in ParseInfo info) : Named(info)
    {
        public const string ExperimentalFileBasedProgramEnableIncludeDirective = nameof(ExperimentalFileBasedProgramEnableIncludeDirective);
        public const string ExperimentalFileBasedProgramEnableExcludeDirective = nameof(ExperimentalFileBasedProgramEnableExcludeDirective);
        public const string ExperimentalFileBasedProgramEnableTransitiveDirectives = nameof(ExperimentalFileBasedProgramEnableTransitiveDirectives);
        public const string ExperimentalFileBasedProgramEnableItemMapping = nameof(ExperimentalFileBasedProgramEnableItemMapping);

        public const string MappingPropertyName = "FileBasedProgramsItemMapping";

        public static string DefaultMappingString => ".cs=Compile;.resx=EmbeddedResource;.json=None;.razor=Content";

        public static ImmutableArray<(string Extension, string ItemType)> DefaultMapping
        {
            get
            {
                if (field.IsDefault)
                {
                    field =
                    [
                        (".cs", "Compile"),
                        (".resx", "EmbeddedResource"),
                        (".json", "None"),
                        (".razor", "Content"),
                    ];
                }

                return field;
            }
        }

        /// <summary>
        /// Preserved across <see cref="WithName"/> calls, i.e.,
        /// this is the original directive text as entered by the user.
        /// </summary>
        public required string OriginalName { get; init; }

        public required IncludeOrExcludeKind Kind { get; init; }

        public string? ItemType { get; init; }

        public static new IncludeOrExclude? Parse(in ParseContext context)
        {
            var directiveText = context.DirectiveText;
            if (directiveText.IsWhiteSpace())
            {
                string directiveKind = context.DirectiveKind;
                context.ReportError(string.Format(FileBasedProgramsResources.MissingDirectiveName, directiveKind));
                return null;
            }

            return new IncludeOrExclude(context.Info)
            {
                OriginalName = directiveText,
                Name = directiveText,
                Kind = KindFromString(context.DirectiveKind),
            };
        }

        /// <param name="mapping">
        /// See <see cref="ParseMapping"/>.
        /// </param>
        public IncludeOrExclude WithDeterminedItemType(ErrorReporter reportError, ImmutableArray<(string Extension, string ItemType)> mapping)
        {
            Debug.Assert(ItemType is null);

            string? itemType = null;
            foreach (var entry in mapping)
            {
                if (Name.EndsWith(entry.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    itemType = entry.ItemType;
                    break;
                }
            }

            if (itemType is null)
            {
                reportError(Info.SourceFile.Text, Info.SourceFile.Path, Info.Span,
                    string.Format(FileBasedProgramsResources.IncludeOrExcludeDirectiveUnknownFileType,
                    $"#:{KindToString()}",
                    string.Join(", ", mapping.Select(static e => e.Extension))));
                return this;
            }

            return new IncludeOrExclude(Info)
            {
                OriginalName = OriginalName,
                Name = Name,
                Kind = Kind,
                ItemType = itemType,
            };
        }

        public IncludeOrExclude WithName(string name)
        {
            if (Name == name)
            {
                return this;
            }

            return new IncludeOrExclude(Info)
            {
                OriginalName = OriginalName,
                Name = name,
                Kind = Kind,
                ItemType = ItemType,
            };
        }

        private static IncludeOrExcludeKind KindFromString(string kind)
        {
            return kind switch
            {
                "include" => IncludeOrExcludeKind.Include,
                "exclude" => IncludeOrExcludeKind.Exclude,
                _ => throw new InvalidOperationException($"Unexpected include/exclude directive kind '{kind}'."),
            };
        }

        public string KindToString()
        {
            return Kind switch
            {
                IncludeOrExcludeKind.Include => "include",
                IncludeOrExcludeKind.Exclude => "exclude",
                _ => throw new InvalidOperationException($"Unexpected {nameof(IncludeOrExcludeKind)} value '{Kind}'."),
            };
        }

        public string KindToMSBuildString()
        {
            return Kind switch
            {
                IncludeOrExcludeKind.Include => "Include",
                IncludeOrExcludeKind.Exclude => "Remove",
                _ => throw new InvalidOperationException($"Unexpected {nameof(IncludeOrExcludeKind)} value '{Kind}'."),
            };
        }

        public override string ToString() => $"#:{KindToString()} {Name}";

        /// <summary>
        /// Parses a <paramref name="value"/> in the format <c>.protobuf=Protobuf;.cshtml=Content</c>.
        /// Should come from MSBuild property with name <see cref="MappingPropertyName"/>.
        /// </summary>
        public static ImmutableArray<(string Extension, string ItemType)> ParseMapping(
            string value,
            SourceFile sourceFile,
            ErrorReporter errorReporter)
        {
            var pairs = value.Split(';');

            var builder = ImmutableArray.CreateBuilder<(string Extension, string ItemType)>(pairs.Length);

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=');

                if (parts.Length != 2)
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidIncludeExcludeMappingEntry, pair));
                    continue;
                }

                var extension = parts[0].Trim();
                var itemType = parts[1].Trim();

                if (extension is not ['.', _, ..])
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidIncludeExcludeMappingExtension, extension, pair));
                    continue;
                }

                if (itemType.IsWhiteSpace())
                {
                    ReportError(string.Format(FileBasedProgramsResources.InvalidIncludeExcludeMappingItemType, itemType, pair));
                    continue;
                }

                builder.Add((extension, itemType));
            }

            return builder.DrainToImmutable();

            void ReportError(string message)
                => errorReporter(sourceFile.Text, sourceFile.Path, default, message);
        }
    }
}

/// <summary>
/// Used for deduplication - compares directives by their type and name (ignoring case).
/// </summary>
internal sealed class NamedDirectiveComparer : IEqualityComparer<CSharpDirective.Named>
{
    public static readonly NamedDirectiveComparer Instance = new();

    private NamedDirectiveComparer() { }

    public bool Equals(CSharpDirective.Named? x, CSharpDirective.Named? y)
    {
        if (ReferenceEquals(x, y)) return true;

        if (x is null || y is null) return false;

        return x.GetType() == y.GetType() &&
            StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name);
    }

    public int GetHashCode(CSharpDirective.Named obj)
    {
        return ExternalHelpers.CombineHashCodes(
            obj.GetType().GetHashCode(),
            StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name));
    }
}

internal sealed class SimpleDiagnostic
{
    public required Position Location { get; init; }
    public required string Message { get; init; }

    /// <summary>
    /// An adapter of <see cref="FileLinePositionSpan"/> that ensures we JSON-serialize only the necessary fields.
    /// </summary>
    /// <remarks>
    /// note: this type is only serialized for run-api scenarios.
    /// If/when run-api is removed, we would also want to remove the usage of System.Text.Json attributes.
    /// </remarks>
    public readonly struct Position
    {
        public required string Path { get; init; }
        public required LinePositionSpan Span { get; init; }
        [JsonIgnore]
        public TextSpan TextSpan { get; init; }
    }
}

internal delegate void ErrorReporter(SourceText text, string path, TextSpan textSpan, string message, Exception? innerException = null);

internal static partial class ErrorReporters
{
    public static readonly ErrorReporter IgnoringReporter =
        static (_, _, _, _, _) => { };

    public static ErrorReporter CreateCollectingReporter(out ImmutableArray<SimpleDiagnostic>.Builder builder)
    {
        var capturedBuilder = builder = ImmutableArray.CreateBuilder<SimpleDiagnostic>();

        return (text, path, textSpan, message, _) =>
            capturedBuilder.Add(new SimpleDiagnostic
            {
                Location = new SimpleDiagnostic.Position()
                {
                    Path = path,
                    TextSpan = textSpan,
                    Span = text.Lines.GetLinePositionSpan(textSpan)
                },
                Message = message
            });
    }
}
