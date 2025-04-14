// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET9_0_OR_GREATER

using System;
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
/// Can generate project file (XML) for a file-based program.
/// </summary>
[Experimental(RoslynExperiments.VirtualProjectGenerator, UrlFormat = RoslynExperiments.VirtualProjectGenerator_Url)]
public static class VirtualProjectGenerator
{
    /// <summary>
    /// Generates project file text for a file-based program converted to project-based program.
    /// </summary>
    /// <param name="writerFactory">
    /// Factory for creating a writer that will be used to write the generated project file XML content.
    /// This will be called after directives are checked so any conversion I/O work can be avoided in case of errors.
    /// </param>
    /// <param name="arg">
    /// Argument for <paramref name="writerFactory"/>.
    /// </param>
    /// <param name="force">
    /// Whether malformed directives should be ignored.
    /// Otherwise, the first malformed directive encountered throws <see cref="DiagnosticException"/>.
    /// </param>
    /// <returns>
    /// The entry point file content with directives removed or <see langword="null"/> if no conversion is necessary.
    /// </returns>
    public static SourceText? WriteConvertedProjectFile<TArg>(
        string entryPointFileFullPath,
        SourceText entryPointFileText,
        TArg arg,
        Func<TArg, TextWriter> writerFactory,
        bool force)
    {
        return WriteProjectFileImpl(
            entryPointFileFullPath,
            entryPointFileText,
            arg,
            writerFactory,
            artifactsPath: null,
            convert: true,
            force);
    }

    /// <summary>
    /// Generates project file text for a file-based program.
    /// </summary>
    /// <param name="writer">
    /// Destination for the generated project file XML content.
    /// </param>
    /// <param name="artifactsPath">
    /// Path to a directory where build artifacts should be placed. It is recommended to use <see cref="GetArtifactsPath(string)"/>.
    /// </param>
    public static void WriteVirtualProjectFile(
        string entryPointFileFullPath,
        SourceText entryPointFileText,
        TextWriter writer,
        string artifactsPath)
    {
        WriteProjectFileImpl(
            entryPointFileFullPath,
            entryPointFileText,
            writer,
            static (writer) => writer,
            artifactsPath,
            convert: false,
            force: false);
    }

    private static SourceText? WriteProjectFileImpl<TArg>(
        string entryPointFileFullPath,
        SourceText entryPointFileText,
        TArg arg,
        Func<TArg, TextWriter> writerFactory,
        string? artifactsPath,
        bool convert,
        bool force)
    {
        var sourceFile = new SourceFile(entryPointFileFullPath, entryPointFileText);
        var directives = FindDirectives(sourceFile, reportErrors: convert && !force);

        using var writer = writerFactory(arg);

        int processedDirectives = 0;

        var sdkDirectives = directives.OfType<VirtualProjectDirective.Sdk>();
        var propertyDirectives = directives.OfType<VirtualProjectDirective.Property>();
        var packageDirectives = directives.OfType<VirtualProjectDirective.Package>();

        string sdkValue = "Microsoft.NET.Sdk";

        if (sdkDirectives.FirstOrDefault() is { } firstSdk)
        {
            sdkValue = firstSdk.ToSlashDelimitedString();
            processedDirectives++;
        }

        if (!convert)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(artifactsPath));

            writer.WriteLine($"""
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
            writer.WriteLine($"""
                <Project Sdk="{escapeValue(sdkValue)}">

                """);
        }

        foreach (var sdk in sdkDirectives.Skip(1))
        {
            if (!convert)
            {
                writer.WriteLine($"""
                      <Import Project="Sdk.props" Sdk="{escapeValue(sdk.ToSlashDelimitedString())}" />
                    """);
            }
            else if (sdk.Version is null)
            {
                writer.WriteLine($"""
                      <Sdk Name="{escapeValue(sdk.Name)}" />
                    """);
            }
            else
            {
                writer.WriteLine($"""
                      <Sdk Name="{escapeValue(sdk.Name)}" Version="{escapeValue(sdk.Version)}" />
                    """);
            }

            processedDirectives++;
        }

        if (processedDirectives > 1)
        {
            writer.WriteLine();
        }

        // Kept in sync with the default `dotnet new console` project file (enforced by sdk test `DotnetProjectAddTests.SameAsTemplate`).
        writer.WriteLine($"""
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
            """);

        if (!convert)
        {
            writer.WriteLine("""

                  <PropertyGroup>
                    <EnableDefaultItems>false</EnableDefaultItems>
                  </PropertyGroup>
                """);
        }

        if (propertyDirectives.Any())
        {
            writer.WriteLine("""

                  <PropertyGroup>
                """);

            foreach (var property in propertyDirectives)
            {
                writer.WriteLine($"""
                        <{property.Name}>{escapeValue(property.Value)}</{property.Name}>
                    """);

                processedDirectives++;
            }

            writer.WriteLine("  </PropertyGroup>");
        }

        if (!convert)
        {
            // After `#:property` directives so they don't override this.
            writer.WriteLine("""

                  <PropertyGroup>
                    <Features>$(Features);FileBasedProgram</Features>
                  </PropertyGroup>
                """);
        }

        if (packageDirectives.Any())
        {
            writer.WriteLine("""

                  <ItemGroup>
                """);

            foreach (var package in packageDirectives)
            {
                if (package.Version is null)
                {
                    writer.WriteLine($"""
                            <PackageReference Include="{escapeValue(package.Name)}" />
                        """);
                }
                else
                {
                    writer.WriteLine($"""
                            <PackageReference Include="{escapeValue(package.Name)}" Version="{escapeValue(package.Version)}" />
                        """);
                }

                processedDirectives++;
            }

            writer.WriteLine("  </ItemGroup>");
        }

        Debug.Assert(processedDirectives + directives.OfType<VirtualProjectDirective.Shebang>().Count() == directives.Length);

        if (!convert)
        {
            Debug.Assert(entryPointFileFullPath is not null);

            writer.WriteLine($"""

                  <ItemGroup>
                    <Compile Include="{escapeValue(entryPointFileFullPath)}" />
                  </ItemGroup>

                """);

            foreach (var sdk in sdkDirectives)
            {
                writer.WriteLine($"""
                      <Import Project="Sdk.targets" Sdk="{escapeValue(sdk.ToSlashDelimitedString())}" />
                    """);
            }

            if (!sdkDirectives.Any())
            {
                Debug.Assert(sdkValue == "Microsoft.NET.Sdk");
                writer.WriteLine("""
                      <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />
                    """);
            }

            writer.WriteLine("""

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

        writer.WriteLine("""

            </Project>
            """);

        return convert ? RemoveDirectivesFromFile(directives, entryPointFileText) : null;

        static string escapeValue(string value) => SecurityElement.Escape(value);
    }

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

#pragma warning disable RSEXPERIMENTAL003 // 'SyntaxTokenParser' is experimental
    private static ImmutableArray<VirtualProjectDirective> FindDirectives(SourceFile sourceFile, bool reportErrors)
    {
        var builder = ImmutableArray.CreateBuilder<VirtualProjectDirective>();
        SyntaxTokenParser tokenizer = SyntaxFactory.CreateTokenParser(sourceFile.Text,
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

                builder.Add(new VirtualProjectDirective.Shebang { Span = span });
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
                builder.Add(VirtualProjectDirective.Parse(sourceFile, span, name.ToString(), value.ToString()));
            }

            previousWhiteSpaceSpan = default;
        }

        // In conversion mode, we want to report errors for any invalid directives in the rest of the file
        // so users don't end up with invalid directives in the converted project.
        if (reportErrors)
        {
            tokenizer.ResetTo(result);

            do
            {
                result = tokenizer.ParseNextToken();

                foreach (var trivia in result.Token.LeadingTrivia)
                {
                    reportErrorFor(sourceFile, trivia);
                }

                foreach (var trivia in result.Token.TrailingTrivia)
                {
                    reportErrorFor(sourceFile, trivia);
                }
            }
            while (!result.Token.IsKind(SyntaxKind.EndOfFileToken));
        }

        // The result should be ordered by source location, RemoveDirectivesFromFile depends on that.
        return builder.ToImmutable();

        static TextSpan getFullSpan(TextSpan previousWhiteSpaceSpan, SyntaxTrivia trivia)
        {
            // Include the preceding whitespace in the span, i.e., span will be the whole line.
            return previousWhiteSpaceSpan.IsEmpty ? trivia.FullSpan : TextSpan.FromBounds(previousWhiteSpaceSpan.Start, trivia.FullSpan.End);
        }

        static void reportErrorFor(SourceFile sourceFile, SyntaxTrivia trivia)
        {
            if (trivia.ContainsDiagnostics && trivia.IsKind(SyntaxKind.IgnoredDirectiveTrivia))
            {
                throw new DiagnosticException(CSharpResources.CannotConvertDirective, sourceFile.GetLocationString(trivia.Span));
            }
        }
    }
#pragma warning restore RSEXPERIMENTAL003 // 'SyntaxTokenParser' is experimental

    private static SourceText? RemoveDirectivesFromFile(ImmutableArray<VirtualProjectDirective> directives, SourceText text)
    {
        if (directives.Length == 0)
        {
            return null;
        }

        Debug.Assert(directives.OrderBy(d => d.Span.Start).SequenceEqual(directives), "Directives should be ordered by source location.");

        for (int i = directives.Length - 1; i >= 0; i--)
        {
            var directive = directives[i];
            text = text.Replace(directive.Span, string.Empty);
        }

        return text;
    }
}

internal static partial class Patterns
{
    [GeneratedRegex("""\s+""")]
    public static partial Regex Whitespace { get; }
}

#endif
