// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET9_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.FileBasedPrograms;

/// <summary>
/// Can generate project file XML (in <c>.csproj</c> format) for a file-based program.
/// </summary>
/// <param name="entryPointFileFullPath">See <see cref="EntryPointFileFullPath"/>.</param>
/// <remarks>
/// This class is not thread safe.
/// </remarks>
[Experimental(RoslynExperiments.FileBasedProgramProject, UrlFormat = RoslynExperiments.FileBasedProgramProject_Url)]
public sealed class FileBasedProgramProject(string entryPointFileFullPath)
{
    /// <summary>
    /// Each list should be ordered by source location, <see cref="ConvertSourceText"/> depends on that.
    /// </summary>
    private readonly SortedDictionary<string, (SourceText, List<FileBasedProgramDirective>)> _directives = new SortedDictionary<string, (SourceText, List<FileBasedProgramDirective>)>();

    /// <summary>
    /// Path to the entry-point file of the file-based program.
    /// This is not accessed via I/O, it is only embedded as text into the project XML if necessary.
    /// </summary>
    public string EntryPointFileFullPath { get; } = entryPointFileFullPath;

    /// <param name="filePath">
    /// Used for <see cref="Diagnostic.Location"/>.
    /// </param>
    /// <param name="text">
    /// C# text which <see cref="IgnoredDirectiveTriviaSyntax"/> directives are parsed from.
    /// </param>
    /// <param name="reportAllErrors">
    /// If <see langword="true"/>, the whole <paramref name="text"/> will be parsed
    /// to find <see cref="Diagnostic"/>s about every <see cref="IgnoredDirectiveTriviaSyntax"/> directive.
    /// Otherwise, only directives up to the first C# token will be checked.
    /// The former is useful for <c>dotnet project convert</c> where we want to report all errors because it would be difficult to fix them up after the conversion.
    /// The latter is useful for <c>dotnet run file.cs</c> where if there are <see cref="IgnoredDirectiveTriviaSyntax"/> directives after the first token,
    /// compiler will report <see cref="ErrorCode.ERR_PPIgnoredFollowsToken"/> anyway, so we speed up success scenarios by not parsing the whole file up front in the SDK CLI.
    /// </param>
    /// <returns>
    /// Diagnostics about malformed <see cref="IgnoredDirectiveTriviaSyntax"/> directives.
    /// These are not the errors compiler would report (like the directive being after a token, i.e., <see cref="ErrorCode.ERR_PPIgnoredFollowsToken"/>)
    /// but rather errors from parsing directive content (like missing property value, i.e., <see cref="ErrorCode.ERR_PropertyDirectiveMissingParts"/>).
    /// </returns>
    public ImmutableArray<Diagnostic> ParseDirectives(string filePath, SourceText text, bool reportAllErrors)
#pragma warning disable RSEXPERIMENTAL003 // 'SyntaxTokenParser' is experimental
    {
        var file = new SourceFile(filePath, text);
        var directives = new List<FileBasedProgramDirective>();
        _directives.Add(filePath, (text, directives));
        var diagnosticBag = DiagnosticBag.GetInstance();
        SyntaxTokenParser tokenizer = SyntaxFactory.CreateTokenParser(text,
            CSharpParseOptions.Default.WithFeatures([new("FileBasedProgram", "true")]));

        var result = tokenizer.ParseLeadingTrivia();
        TextSpan previousWhiteSpaceSpan = default;
        foreach (var trivia in result.Token.LeadingTrivia)
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
                TextSpan span = getFullSpan(previousWhiteSpaceSpan, trivia);

                directives.Add(new FileBasedProgramDirective.Shebang { Span = span });
            }
            else if (trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                TextSpan span = getFullSpan(previousWhiteSpaceSpan, trivia);

                var message = trivia.GetStructure() is IgnoredDirectiveTriviaSyntax { Content: { RawKind: (int)SyntaxKind.StringLiteralToken } content }
                    ? content.Text.AsSpan().Trim()
                    : "";
                var parts = Patterns.Whitespace.EnumerateSplits(message, 2);
                var name = parts.MoveNext() ? message[parts.Current] : default;
                var value = parts.MoveNext() ? message[parts.Current] : default;
                Debug.Assert(!parts.MoveNext());

                var locationInfo = new LocationInfo(file, trivia.Span);
                var parsed = FileBasedProgramDirective.TryParse(locationInfo, span, name.ToString(), value.ToString(), diagnosticBag);
                if (parsed != null)
                {
                    directives.Add(parsed);
                }
            }

            previousWhiteSpaceSpan = default;
        }

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
                    reportErrorFor(file, trivia, diagnosticBag);
                }

                foreach (var trivia in result.Token.TrailingTrivia)
                {
                    reportErrorFor(file, trivia, diagnosticBag);
                }
            }
            while (!result.Token.IsKind(SyntaxKind.EndOfFileToken));
        }

        return diagnosticBag.ToReadOnlyAndFree();

        static TextSpan getFullSpan(TextSpan previousWhiteSpaceSpan, SyntaxTrivia trivia)
        {
            // Include the preceding whitespace in the span, i.e., span will be the whole line.
            return previousWhiteSpaceSpan.IsEmpty ? trivia.FullSpan : TextSpan.FromBounds(previousWhiteSpaceSpan.Start, trivia.FullSpan.End);
        }

        static void reportErrorFor(SourceFile file, SyntaxTrivia trivia, DiagnosticBag diagnosticBag)
        {
            if (trivia.ContainsDiagnostics && trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                Location location = new LocationInfo(file, trivia.Span).ToLocation();
                diagnosticBag.Add(ErrorCode.ERR_CannotConvertDirective, location);
            }
        }
    }
#pragma warning restore RSEXPERIMENTAL003 // 'SyntaxTokenParser' is experimental

    /// <summary>
    /// Generates project file text for a file-based program.
    /// </summary>
    /// <param name="artifactsPath">
    /// Path to a directory where build artifacts should be placed.
    /// Use <see cref="GetArtifactsPath"/> for the default logic.
    /// This is not accessed via I/O, it is only embedded as text into the project XML.
    /// </param>
    public void Emit(TextWriter csprojWriter, string artifactsPath)
    {
        if (string.IsNullOrWhiteSpace(artifactsPath))
        {
            throw new ArgumentException(nameof(artifactsPath));
        }

        EmitImpl(csprojWriter, artifactsPath, convert: false);
    }

    /// <summary>
    /// Generates project file text for a file-based program converted to project-based program.
    /// </summary>
    public void EmitConverted(TextWriter csprojWriter)
    {
        EmitImpl(csprojWriter, artifactsPath: null, convert: true);
    }

    /// <summary>
    /// Removes <see cref="IgnoredDirectiveTriviaSyntax"/> directives from C# file text
    /// so it can be used after conversion to a project-based program.
    /// </summary>
    /// <param name="path">
    /// Path to a file which <see cref="ParseDirectives"/> was previously called on.
    /// </param>
    /// <returns>
    /// File text with directives removed or <see langword="null"/> if no conversion is necessary.
    /// </returns>
    public SourceText? ConvertSourceText(string path)
    {
        var (text, directives) = _directives[path];

        if (directives.Count == 0)
        {
            return null;
        }

        Debug.Assert(directives.OrderBy(d => d.Span.Start).SequenceEqual(directives), "Directives should be ordered by source location.");

        for (int i = directives.Count - 1; i >= 0; i--)
        {
            var directive = directives[i];
            text = text.Replace(directive.Span, string.Empty);
        }

        return text;
    }

    private IEnumerable<FileBasedProgramDirective> AllDirectives
        => _directives.Values.SelectMany(t => t.Item2);

    private void EmitImpl(TextWriter csprojWriter, string? artifactsPath, bool convert)
    {
        int processedDirectives = 0;

        var sdkDirectives = AllDirectives.OfType<FileBasedProgramDirective.Sdk>();
        var propertyDirectives = AllDirectives.OfType<FileBasedProgramDirective.Property>();
        var packageDirectives = AllDirectives.OfType<FileBasedProgramDirective.Package>();

        string sdkValue = "Microsoft.NET.Sdk";

        if (sdkDirectives.FirstOrDefault() is { } firstSdk)
        {
            sdkValue = firstSdk.ToSlashDelimitedString();
            processedDirectives++;
        }

        if (!convert)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(artifactsPath));

            csprojWriter.WriteLine($"""
                <Project>

                  <PropertyGroup>
                    <IncludeProjectNameInArtifactsPaths>false</IncludeProjectNameInArtifactsPaths>
                    <ArtifactsPath>{escapeValue(artifactsPath)}</ArtifactsPath>
                  </PropertyGroup>

                  <!-- We need to explicitly import Sdk props/targets so we can override the targets below. -->
                  <Import Project="Sdk.props" Sdk="{escapeValue(sdkValue)}" />
                """);
        }
        else
        {
            csprojWriter.WriteLine($"""
                <Project Sdk="{escapeValue(sdkValue)}">

                """);
        }

        foreach (var sdk in sdkDirectives.Skip(1))
        {
            if (!convert)
            {
                csprojWriter.WriteLine($"""
                      <Import Project="Sdk.props" Sdk="{escapeValue(sdk.ToSlashDelimitedString())}" />
                    """);
            }
            else if (sdk.Version is null)
            {
                csprojWriter.WriteLine($"""
                      <Sdk Name="{escapeValue(sdk.Name)}" />
                    """);
            }
            else
            {
                csprojWriter.WriteLine($"""
                      <Sdk Name="{escapeValue(sdk.Name)}" Version="{escapeValue(sdk.Version)}" />
                    """);
            }

            processedDirectives++;
        }

        if (processedDirectives > 1)
        {
            csprojWriter.WriteLine();
        }

        // Kept in sync with the default `dotnet new console` project file (enforced by sdk test `DotnetProjectAddTests.SameAsTemplate`).
        csprojWriter.WriteLine($"""
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            """);

        if (!convert)
        {
            csprojWriter.WriteLine("""

                  <PropertyGroup>
                    <EnableDefaultItems>false</EnableDefaultItems>
                  </PropertyGroup>
                """);
        }

        if (propertyDirectives.Any())
        {
            csprojWriter.WriteLine("""

                  <PropertyGroup>
                """);

            foreach (var property in propertyDirectives)
            {
                csprojWriter.WriteLine($"""
                        <{property.Name}>{escapeValue(property.Value)}</{property.Name}>
                    """);

                processedDirectives++;
            }

            csprojWriter.WriteLine("  </PropertyGroup>");
        }

        if (!convert)
        {
            // After `#:property` directives so they don't override this.
            csprojWriter.WriteLine("""

                  <PropertyGroup>
                    <Features>$(Features);FileBasedProgram</Features>
                  </PropertyGroup>
                """);
        }

        if (packageDirectives.Any())
        {
            csprojWriter.WriteLine("""

                  <ItemGroup>
                """);

            foreach (var package in packageDirectives)
            {
                if (package.Version is null)
                {
                    csprojWriter.WriteLine($"""
                            <PackageReference Include="{escapeValue(package.Name)}" />
                        """);
                }
                else
                {
                    csprojWriter.WriteLine($"""
                            <PackageReference Include="{escapeValue(package.Name)}" Version="{escapeValue(package.Version)}" />
                        """);
                }

                processedDirectives++;
            }

            csprojWriter.WriteLine("  </ItemGroup>");
        }

        Debug.Assert(processedDirectives + AllDirectives.OfType<FileBasedProgramDirective.Shebang>().Count() == AllDirectives.Count());

        if (!convert)
        {
            csprojWriter.WriteLine($"""

                  <ItemGroup>
                    <Compile Include="{escapeValue(EntryPointFileFullPath)}" />
                  </ItemGroup>

                """);

            foreach (var sdk in sdkDirectives)
            {
                csprojWriter.WriteLine($"""
                      <Import Project="Sdk.targets" Sdk="{escapeValue(sdk.ToSlashDelimitedString())}" />
                    """);
            }

            if (!sdkDirectives.Any())
            {
                Debug.Assert(sdkValue == "Microsoft.NET.Sdk");
                csprojWriter.WriteLine("""
                      <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
                    """);
            }

            csprojWriter.WriteLine("""

                  <!--
                    Override targets which don't work with project files that are not present on disk.
                    See https://github.com/NuGet/Home/issues/14148.
                  -->

                  <Target Name="_FilterRestoreGraphProjectInputItems"
                          DependsOnTargets="_LoadRestoreGraphEntryPoints"
                          Returns="@(FilteredRestoreGraphProjectInputItems)">
                    <ItemGroup>
                      <FilteredRestoreGraphProjectInputItems Include="@(RestoreGraphProjectInputItems)" />
                    </ItemGroup>
                  </Target>

                  <Target Name="_GetAllRestoreProjectPathItems"
                          DependsOnTargets="_FilterRestoreGraphProjectInputItems"
                          Returns="@(_RestoreProjectPathItems)">
                    <ItemGroup>
                      <_RestoreProjectPathItems Include="@(FilteredRestoreGraphProjectInputItems)" />
                    </ItemGroup>
                  </Target>

                  <Target Name="_GenerateRestoreGraph"
                          DependsOnTargets="_FilterRestoreGraphProjectInputItems;_GetAllRestoreProjectPathItems;_GenerateRestoreGraphProjectEntry;_GenerateProjectRestoreGraph"
                          Returns="@(_RestoreGraphEntry)">
                    <!-- Output from dependency _GenerateRestoreGraphProjectEntry and _GenerateProjectRestoreGraph -->
                  </Target>
                """);
        }

        csprojWriter.WriteLine("""

            </Project>
            """);

        static string escapeValue(string value) => SecurityElement.Escape(value);
    }

    /// <summary>
    /// Computes a path to a directory where build artifacts should be placed.
    /// </summary>
    /// <remarks>
    /// The directory is not created by this method.
    /// </remarks>
    public static string GetArtifactsPath(string entryPointFilePath)
    {
        // We want a location where permissions are expected to be restricted to the current user.
        string directory = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.GetTempPath()
            : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Include entry point file name so the directory name is not completely opaque.
        string fileName = Path.GetFileNameWithoutExtension(entryPointFilePath);
        string hash = hashString(entryPointFilePath);
        string directoryName = $"{fileName}-{hash}";

        return Path.Join(directory, "dotnet", "runfile", directoryName);

        static string hashString(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            var hashBytes = CryptographicHashProvider.ComputeSourceHash(ImmutableCollectionsMarshal.AsImmutableArray(bytes));
            return Convert.ToHexStringLower(hashBytes.AsSpan());
        }
    }
}

internal static partial class Patterns
{
    [GeneratedRegex("""\s+""")]
    public static partial Regex Whitespace { get; }
}

internal readonly struct SourceFile(string path, SourceText text)
{
    public string Path { get; } = path;
    public SourceText Text { get; } = text;
}

internal readonly struct LocationInfo(SourceFile file, TextSpan span)
{
    public SourceFile File { get; } = file;
    public TextSpan Span { get; } = span;

    public Location ToLocation()
    {
        return Location.Create(File.Path, Span, File.Text.Lines.GetLinePositionSpan(Span));
    }
}

#endif
