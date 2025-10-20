// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Largely copied from https://github.com/dotnet/sdk/blob/ce691a53dad7f2b6057c85c1e7fd7800dfe0788f/src/Cli/dotnet/Commands/Run/VirtualProjectBuildingCommand.cs#L1410
// TODO: we'd like to extract a source package which makes this copy of the source unnecessary.

#if false // TODO2: delete this file in favor of the source package.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FileBasedPrograms;

internal static class AppDirectiveHelpers
{

#pragma warning disable RSEXPERIMENTAL003 // 'SyntaxTokenParser' is experimental
    public static SyntaxTokenParser CreateTokenizer(SourceText text)
    {
        return SyntaxFactory.CreateTokenParser(text,
            CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]));
    }
#pragma warning restore RSEXPERIMENTAL003 // 'SyntaxTokenParser' is experimental

    // TODO2: I don't think analyzer would use this method, should we expose it in the source package?
    /// <param name="reportAllErrors">
    /// If <see langword="true"/>, the whole <paramref name="sourceFile"/> is parsed to find diagnostics about every app directive.
    /// Otherwise, only directives up to the first C# token is checked.
    /// The former is useful for <c>dotnet project convert</c> where we want to report all errors because it would be difficult to fix them up after the conversion.
    /// The latter is useful for <c>dotnet run file.cs</c> where if there are app directives after the first token,
    /// compiler reports <see cref="ErrorCode.ERR_PPIgnoredFollowsToken"/> anyway, so we speed up success scenarios by not parsing the whole file up front in the SDK CLI.
    /// </param>
    public static ImmutableArray<CSharpDirective> FindDirectives(SourceFile sourceFile, bool reportAllErrors, DiagnosticBag diagnostics)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpDirective>();
        var tokenizer = CreateTokenizer(sourceFile.Text);

        var result = tokenizer.ParseLeadingTrivia();
        var triviaList = result.Token.LeadingTrivia;

        FindDirectives(sourceFile, triviaList, diagnostics, builder);

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
                diagnostics.AddError(sourceFile, trivia.Span, CliCommandStrings.CannotConvertDirective);
            }
        }

        // The result should be ordered by source location, RemoveDirectivesFromFile depends on that.
        return builder.ToImmutable();
    }

    public static void FindDirectives(
        SourceFile sourceFile,
        SyntaxTriviaList triviaList,
        DiagnosticBag diagnostics,
        ImmutableArray<CSharpDirective>.Builder builder)
    {
        // TODO2: netstandard HashSet doesn't have TryGetValue or any other great way of "getting the already present value".
        var deduplicated = new Dictionary<CSharpDirective.Named, CSharpDirective.Named>(NamedDirectiveComparer.Instance);
        TextSpan previousWhiteSpaceSpan = default;

        foreach (var (index, trivia) in triviaList.Index())
        {
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
                var span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                var whiteSpace = GetWhiteSpaceInfo(triviaList, index);
                var info = new CSharpDirective.ParseInfo
                {
                    Span = span,
                    LeadingWhiteSpace = whiteSpace.Leading,
                    TrailingWhiteSpace = whiteSpace.Trailing,
                };
                builder.Add(new CSharpDirective.Shebang(info));
            }
            else if (trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                var span = GetFullSpan(previousWhiteSpaceSpan, trivia);

                var message = trivia.GetStructure() is IgnoredDirectiveTriviaSyntax { Content: { RawKind: (int)SyntaxKind.StringLiteralToken } content }
                    ? content.Text.AsSpan().Trim()
                    : "";
                // TODO: original impl was using more recent span-oriented APIs. Optimal sharing may be tricky.
                // var parts = Patterns.Whitespace.EnumerateSplits(message, 2);
                var parts = Patterns.Whitespace.Split(message.ToString(), 2);
                var name = parts.Length > 0 ? parts[0] : null;
                var value = parts.Length > 1 ? parts[1] : null;
                Debug.Assert(!(parts.Length > 2));

                var whiteSpace = GetWhiteSpaceInfo(triviaList, index);
                var context = new CSharpDirective.ParseContext
                {
                    Info = new()
                    {
                        Span = span,
                        LeadingWhiteSpace = whiteSpace.Leading,
                        TrailingWhiteSpace = whiteSpace.Trailing,
                    },
                    Diagnostics = diagnostics,
                    SourceFile = sourceFile,
                    DirectiveKind = name ?? "",
                    DirectiveText = value ?? ""
                };
                if (CSharpDirective.Parse(context) is { } directive)
                {
                    // If the directive is already present, report an error.
                    if (deduplicated.ContainsKey(directive))
                    {
                        var existingDirective = deduplicated[directive];
                        var typeAndName = $"#:{existingDirective.GetType().Name.ToLowerInvariant()} {existingDirective.Name}";
                        diagnostics.AddError(sourceFile, directive.Info.Span, string.Format(CliCommandStrings.DuplicateDirective, typeAndName));
                    }
                    else
                    {
                        deduplicated.Add(directive, directive);
                    }

                    builder.Add(directive);
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

            for (var i = index - 1; i >= 0; i--)
            {
                if (!Fill(ref result.Leading, triviaList, i))
                    break;
            }

            for (var i = index + 1; i < triviaList.Count; i++)
            {
                if (!Fill(ref result.Trailing, triviaList, i))
                    break;
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
        return new SourceFile(filePath, SourceText.From(stream, Encoding.UTF8));
    }

    public SourceFile WithText(SourceText newText)
    {
        return new SourceFile(Path, newText);
    }

    public void Save()
    {
        using var stream = File.Open(Path, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        Text.Write(writer);
    }

    public FileLinePositionSpan GetFileLinePositionSpan(TextSpan span)
    {
        return new FileLinePositionSpan(Path, Text.Lines.GetLinePositionSpan(span));
    }

    public string GetLocationString(TextSpan span)
    {
        var positionSpan = GetFileLinePositionSpan(span);
        return $"{positionSpan.Path}({positionSpan.StartLinePosition.Line + 1})";
    }
}

internal static partial class Patterns
{
    // TODO: GeneratedRegex portability
    public static Regex Whitespace { get; } = new Regex("""\s+""");

    public static Regex DisallowedNameCharacters { get; } = new Regex("""[\s@=/]""");

    public static Regex EscapedCompilerOption { get; } = new Regex("""^/\w+:".*"$""", RegexOptions.Singleline);
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
        public required DiagnosticBag Diagnostics { get; init; }
        public required SourceFile SourceFile { get; init; }
        public required string DirectiveKind { get; init; }
        public required string DirectiveText { get; init; }
    }

    public static Named? Parse(in ParseContext context)
    {
        return context.DirectiveKind switch
        {
            "sdk" => Sdk.Parse(context),
            "property" => Property.Parse(context),
            "package" => Package.Parse(context),
            "project" => Project.Parse(context),
            var other => context.Diagnostics.AddError<Named>(context.SourceFile, context.Info.Span, string.Format(CliCommandStrings.UnrecognizedDirective, other)),
        };
    }

    private static (string, string?)? ParseOptionalTwoParts(in ParseContext context, char separator)
    {
        // TODO2: passing Ordinal to IndexOf is not supported on netstandard and doesn't look too necessary
        // TODO2: no AsSpan(Range) on netstandard
        var i = context.DirectiveText.IndexOf(separator);
        var firstPart = (i < 0 ? context.DirectiveText : context.DirectiveText.AsSpan(0, i)).TrimEnd();

        string directiveKind = context.DirectiveKind;
        if (firstPart.IsWhiteSpace())
        {
            return context.Diagnostics.AddError<(string, string?)?>(context.SourceFile, context.Info.Span, string.Format(CliCommandStrings.MissingDirectiveName, directiveKind));
        }

        // If the name contains characters that resemble separators, report an error to avoid any confusion.
        // TODO2: netstandard no IsMatch(ReadOnlySpan)
        if (Patterns.DisallowedNameCharacters.IsMatch(firstPart.ToString()))
        {
            return context.Diagnostics.AddError<(string, string?)?>(context.SourceFile, context.Info.Span, string.Format(CliCommandStrings.InvalidDirectiveName, directiveKind, separator));
        }

        var secondPart = i < 0 ? [] : context.DirectiveText.AsSpan((i + 1)).TrimStart();
        if (i < 0 || secondPart.IsWhiteSpace())
        {
            return (firstPart.ToString(), null);
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
                return context.Diagnostics.AddError<Property?>(context.SourceFile, context.Info.Span, CliCommandStrings.PropertyDirectiveMissingParts);
            }

            try
            {
                propertyName = XmlConvert.VerifyName(propertyName);
            }
            catch (XmlException ex)
            {
                return context.Diagnostics.AddError<Property?>(context.SourceFile, context.Info.Span, string.Format(CliCommandStrings.PropertyDirectiveInvalidName, ex.Message), ex);
            }

            if (propertyName.Equals("RestoreUseStaticGraphEvaluation", StringComparison.OrdinalIgnoreCase) &&
                MSBuildUtilities.ConvertStringToBool(propertyValue))
            {
                context.Diagnostics.AddError(context.SourceFile, context.Info.Span, CliCommandStrings.StaticGraphRestoreNotSupported);
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
    public sealed class Project(in ParseInfo info) : Named(info)
    {
        public static new Project? Parse(in ParseContext context)
        {
            var directiveText = context.DirectiveText;
            if (directiveText.IsWhiteSpace())
            {
                string directiveKind = context.DirectiveKind;
                return context.Diagnostics.AddError<Project?>(context.SourceFile, context.Info.Span, string.Format(CliCommandStrings.MissingDirectiveName, directiveKind));
            }

            try
            {
                // If the path is a directory like '../lib', transform it to a project file path like '../lib/lib.csproj'.
                // Also normalize blackslashes to forward slashes to ensure the directive works on all platforms.
                var sourceDirectory = Path.GetDirectoryName(context.SourceFile.Path) ?? ".";
                var resolvedProjectPath = Path.Combine(sourceDirectory, directiveText.Replace('\\', '/'));
                if (Directory.Exists(resolvedProjectPath))
                {
                    var fullFilePath = GetProjectFileFromDirectory(resolvedProjectPath).FullName;
                    // TODO2: no Path.GetRelativePath on netstandard
                    directiveText = Path.GetRelativePath(sourceDirectory, fullFilePath);
                }
                else if (!File.Exists(resolvedProjectPath))
                {
                    throw new GracefulException(CliStrings.CouldNotFindProjectOrDirectory, resolvedProjectPath);
                }
            }
            catch (GracefulException e)
            {
                context.Diagnostics.AddError(context.SourceFile, context.Info.Span, string.Format(CliCommandStrings.InvalidProjectDirective, e.Message), e);
            }

            return new Project(context.Info)
            {
                Name = directiveText,
            };
        }

        public static FileInfo GetProjectFileFromDirectory(string projectDirectory)
        {
            DirectoryInfo dir;
            try
            {
                dir = new DirectoryInfo(projectDirectory);
            }
            catch (ArgumentException)
            {
                throw new GracefulException(CliStrings.CouldNotFindProjectOrDirectory, projectDirectory);
            }

            if (!dir.Exists)
            {
                throw new GracefulException(CliStrings.CouldNotFindProjectOrDirectory, projectDirectory);
            }

            FileInfo[] files = dir.GetFiles("*proj");
            if (files.Length == 0)
            {
                throw new GracefulException(
                    CliStrings.CouldNotFindAnyProjectInDirectory,
                    projectDirectory);
            }

            if (files.Length > 1)
            {
                throw new GracefulException(CliStrings.MoreThanOneProjectInDirectory, projectDirectory);
            }

            return files.First();
        }

        public override string ToString() => $"#:project {Name}";
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
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return x.GetType() == y.GetType() &&
            string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode(CSharpDirective.Named obj)
    {
        return Hash.Combine(
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
    public readonly struct Position
    {
        public required SourceFile SourceFile { get; init; }
        public required TextSpan TextSpan { get; init; }
        public string Path => SourceFile.Path;
        public LinePositionSpan Span => SourceFile.GetFileLinePositionSpan(TextSpan).Span;
    }
}

internal readonly struct DiagnosticBag
{
    public bool IgnoreDiagnostics { get; private init; }

    /// <summary>
    /// If <see langword="null"/> and <see cref="IgnoreDiagnostics"/> is <see langword="false"/>, the first diagnostic is thrown as <see cref="GracefulException"/>.
    /// </summary>
    public ImmutableArray<SimpleDiagnostic>.Builder? Builder { get; private init; }

    public static DiagnosticBag ThrowOnFirst() => default;
    public static DiagnosticBag Collect(out ImmutableArray<SimpleDiagnostic>.Builder builder) => new() { Builder = builder = ImmutableArray.CreateBuilder<SimpleDiagnostic>() };
    public static DiagnosticBag Ignore() => new() { IgnoreDiagnostics = true, Builder = null };

    public void AddError(SourceFile sourceFile, TextSpan span, string message, Exception? inner = null)
    {
        if (Builder != null)
        {
            Debug.Assert(!IgnoreDiagnostics);
            Builder.Add(new SimpleDiagnostic { Location = new SimpleDiagnostic.Position { SourceFile = sourceFile, TextSpan = span }, Message = message });
        }
        else if (!IgnoreDiagnostics)
        {
            throw new GracefulException($"{sourceFile.GetLocationString(span)}: {CliCommandStrings.DirectiveError}: {message}", inner);
        }
    }

    public T? AddError<T>(SourceFile sourceFile, TextSpan span, string message, Exception? inner = null)
    {
        AddError(sourceFile, span, message, inner);
        return default;
    }
}

internal class GracefulException : Exception
{
    public GracefulException()
    {
    }

    public GracefulException(string? message) : base(message)
    {
    }

    public GracefulException(string? message, string? todo2) : base(message)
    {
    }

    public GracefulException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Internal utilities copied from microsoft/MSBuild repo.
/// </summary>
internal static class MSBuildUtilities
{
    /// <summary>
    /// Converts a string to a bool.  We consider "true/false", "on/off", and 
    /// "yes/no" to be valid boolean representations in the XML.
    /// Modified from its original version to not throw, but return a default value.
    /// </summary>
    /// <param name="parameterValue">The string to convert.</param>
    /// <returns>Boolean true or false, corresponding to the string.</returns>
    internal static bool ConvertStringToBool(string? parameterValue, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(parameterValue))
        {
            return defaultValue;
        }
        else if (ValidBooleanTrue(parameterValue))
        {
            return true;
        }
        else if (ValidBooleanFalse(parameterValue))
        {
            return false;
        }
        else
        {
            // Unsupported boolean representation.
            return defaultValue;
        }
    }

    /// <summary>
    /// Returns true if the string represents a valid MSBuild boolean true value,
    /// such as "on", "!false", "yes"
    /// </summary>
    private static bool ValidBooleanTrue(string? parameterValue)
    {
        return ((string.Compare(parameterValue, "true", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "on", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "yes", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "!false", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "!off", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "!no", StringComparison.OrdinalIgnoreCase) == 0));
    }

    /// <summary>
    /// Returns true if the string represents a valid MSBuild boolean false value,
    /// such as "!on" "off" "no" "!true"
    /// </summary>
    private static bool ValidBooleanFalse(string? parameterValue)
    {
        return ((string.Compare(parameterValue, "false", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "off", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "no", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "!true", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "!on", StringComparison.OrdinalIgnoreCase) == 0) ||
                (string.Compare(parameterValue, "!yes", StringComparison.OrdinalIgnoreCase) == 0));
    }
}

internal static class CliStrings
{
    public static string CouldNotFindAnyProjectInDirectory => CSharpAnalyzersResources.CouldNotFindAnyProjectInDirectory;
    public static string MoreThanOneProjectInDirectory => CSharpAnalyzersResources.MoreThanOneProjectInDirectory;
    public static string CouldNotFindProjectOrDirectory => CSharpAnalyzersResources.CouldNotFindProjectOrDirectory;
}
internal static class CliCommandStrings
{
    public static string DirectiveError => CSharpAnalyzersResources.DirectiveError;
    public static string InvalidProjectDirective => CSharpAnalyzersResources.InvalidProjectDirective;
    public static string MissingDirectiveName => CSharpAnalyzersResources.MissingDirectiveName;
    public static string PropertyDirectiveInvalidName => CSharpAnalyzersResources.PropertyDirectiveInvalidName;
    public static string StaticGraphRestoreNotSupported => CSharpAnalyzersResources.StaticGraphRestoreNotSupported;
    public static string PropertyDirectiveMissingParts => CSharpAnalyzersResources.PropertyDirectiveMissingParts;
    public static string InvalidDirectiveName => CSharpAnalyzersResources.InvalidDirectiveName;
    public static string UnrecognizedDirective => CSharpAnalyzersResources.UnrecognizedDirective;
    public static string DuplicateDirective => CSharpAnalyzersResources.DuplicateDirective;
    public static string CannotConvertDirective => CSharpAnalyzersResources.CannotConvertDirective;
}

#endif