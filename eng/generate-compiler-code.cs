#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#:project ../src/Tools/Source/CompilerGeneratorTools/Source/BoundTreeGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/IOperationGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/CSharpSyntaxGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/CSharpErrorFactsGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/VisualBasicSyntaxGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/VisualBasicErrorFactsGenerator/

using System.Runtime.CompilerServices;

var (test, configuration, repoRoot) = ParseArgs(args);

// Folders for test artifacts when running in -test mode
var generationTempDirRoot = Path.Join(repoRoot, "artifacts", "log", configuration, "Generated");
var retVal = 0;

// Generate C# source
Console.WriteLine("Generating C# files...");

var csharpLocation = GetLanguageDirStructure(repoRoot, "CSharp", generationTempDirRoot, test);
retVal = GenerateLanguage(
    BoundTreeGenerator.TargetLanguage.CSharp,
    csharpLocation.LanguageDir,
    test ? csharpLocation.GeneratedSourceTempDir : csharpLocation.GeneratedSourceDir,
    (test ? csharpLocation.GeneratedTestTempDir : csharpLocation.GeneratedTestDir)!,
    (inputFile, outputFile, writeSource, writeTests, writeGrammar)
        => CSharpSyntaxGenerator.Program.Generate(inputFile, outputFile, writeSource, writeTests, writeGrammar, writeSignatures: false),
    Microsoft.CodeAnalysis.CSharp.Internal.CSharpErrorFactsGenerator.Program.Generate);

// Generate VB source
Console.WriteLine("Generating VB files...");

var vbLocation = GetLanguageDirStructure(repoRoot, "VisualBasic", generationTempDirRoot, test);
retVal |= GenerateLanguage(
    BoundTreeGenerator.TargetLanguage.VB,
    vbLocation.LanguageDir,
    test ? vbLocation.GeneratedSourceTempDir : vbLocation.GeneratedSourceDir,
    (test ? vbLocation.GeneratedTestTempDir : vbLocation.GeneratedTestDir)!,
    Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Program.Generate,
    Microsoft.CodeAnalysis.VisualBasic.Internal.VBErrorFactsGenerator.Program.Generate);

// Generate VB GetText source
Console.WriteLine("Generating VB GetText files...");

var getTextLocation = GetGetTextLocation(vbLocation, repoRoot, generationTempDirRoot, test);
Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Program.Generate(
    Path.Join(vbLocation.LanguageDir, "Syntax", "Syntax.xml"),
    Path.Join(test ? getTextLocation.GeneratedSourceTempDir : getTextLocation.GeneratedSourceDir, "Syntax.xml.GetText.Generated.vb"),
    writeSource: false, writeTests: false, writeGrammar: false);

// Generate IOperation source
Console.WriteLine("Generating IOperation files...");

var ioperationLocation = GetIOperationLocation(repoRoot, generationTempDirRoot, test);
retVal |= IOperationGenerator.Program.Generate(Path.Join(ioperationLocation.LanguageDir, "Operations", "OperationInterfaces.xml"), test ? ioperationLocation.GeneratedSourceTempDir : ioperationLocation.GeneratedSourceDir);

if (test)
{
    Console.WriteLine("Verifying generated files...");
    retVal |= TestGenerationResults(repoRoot, csharpLocation, vbLocation, getTextLocation, ioperationLocation);
}

Console.WriteLine(retVal == 0 ? "Generation succeeded." : "Generation failed.");
return retVal;

static (bool test, string configuration, string roslynDirectory) ParseArgs(string[] args, [CallerFilePath] string sourceFilePath = "")
{
    var test = false;
    var configuration = "Debug";

    for (var i = 0; i < args.Length; i++)
    {
        var a = args[i];
        switch (a)
        {
            case "-test":
                test = true;
                break;
            case "-configuration" when i + 1 < args.Length:
                configuration = args[i++];
                break;
            default:
                Console.WriteLine("Unknown argument: " + a);
                Environment.Exit(1);
                break;
        }
    }

    if (Path.GetDirectoryName(sourceFilePath) is not string engDir || Path.GetDirectoryName(engDir) is not string roslynRoot || !File.Exists(Path.Join(roslynRoot, "eng", Path.GetFileName(sourceFilePath))))
    {
        Console.WriteLine("Could not determine source file path. This script must be located in the 'eng' directory of the Roslyn repo.");
        Environment.Exit(1);
        throw null!;
    }

    return (test, configuration, roslynRoot);
}

static GenerationLocation GetLanguageDirStructure(string repoRoot, string languageName, string generationTempDirRoot, bool isTest)
{
    var languageDir = Path.Join(repoRoot, "src", "Compilers", languageName, "Portable");
    var loc = new GenerationLocation(
        languageDir,
        Path.Join(languageDir, "Generated"),
        Path.Join(repoRoot, "src", "Compilers", languageName, "Test", "Syntax", "Generated"),
        Path.Join(generationTempDirRoot, languageName, "Src"),
        Path.Join(generationTempDirRoot, languageName, "Test")
    );

    if (isTest)
    {
        Directory.CreateDirectory(loc.GeneratedSourceTempDir);
        Directory.CreateDirectory(loc.GeneratedTestTempDir!);
    }

    return loc;
}

static GenerationLocation GetGetTextLocation(GenerationLocation vbLocation, string repoRoot, string generationTempDirRoot, bool test)
{
    var getTextDir = new GenerationLocation(
        vbLocation.LanguageDir,
        Path.Join(repoRoot, "src", "ExpressionEvaluator", "VisualBasic", "Source", "ResultProvider", "Generated"),
        GeneratedTestDir: null,
        Path.Join(generationTempDirRoot, "VisualBasic", "GetText"),
        GeneratedTestTempDir: null
    );

    if (test)
    {
        Directory.CreateDirectory(getTextDir.GeneratedSourceTempDir);
    }

    return getTextDir;
}

static GenerationLocation GetIOperationLocation(string repoRoot, string generationTempDirRoot, bool test)
{
    var languageDir = Path.Join(repoRoot, "src", "Compilers", "Core", "Portable");
    var loc = new GenerationLocation(
        languageDir,
        Path.Join(languageDir, "Generated"),
        GeneratedTestDir: null,
        Path.Join(generationTempDirRoot, "Core", "Src"),
        GeneratedTestTempDir: null
    );

    if (test)
    {
        Directory.CreateDirectory(loc.GeneratedSourceTempDir);
    }

    return loc;
}

static int GenerateLanguage(BoundTreeGenerator.TargetLanguage language, string languageDir, string generatedDir, string generatedTestDir, SyntaxGeneratorAction syntaxGenerator, ErrorFactsGeneratorAction errorFactsGenerator)
{
    var extension = language == BoundTreeGenerator.TargetLanguage.CSharp ? "cs" : "vb";
    var generatedTestFile = Path.Join(generatedTestDir, $"Syntax.Test.xml.Generated.{extension}");
    var errorsPath = Path.Join(languageDir, "Errors", language == BoundTreeGenerator.TargetLanguage.CSharp ? "ErrorCode.cs" : "Errors.vb");
    var errorFactsFilePath = Path.Join(generatedDir, $"ErrorFacts.Generated.{extension}");

    var syntaxFile = Path.Join(languageDir, "Syntax", $"Syntax.xml");

    int retVal = 0;

    if (language != BoundTreeGenerator.TargetLanguage.CSharp)
    {
        retVal |= syntaxGenerator(syntaxFile, generatedDir, writeSource: true, writeTests: false, writeGrammar: false);
    }

    retVal |= syntaxGenerator(syntaxFile, generatedDir, writeSource: false, writeTests: false, writeGrammar: true);
    retVal |= syntaxGenerator(syntaxFile, generatedTestFile, writeSource: false, writeTests: true, writeGrammar: false);

    retVal |= BoundTreeGenerator.Program.Generate(
        language,
        Path.Join(languageDir, "BoundTree", "BoundNodes.xml"),
        Path.Join(generatedDir, $"BoundNodes.xml.Generated.{extension}"));

    retVal |= errorFactsGenerator(errorsPath, errorFactsFilePath);

    return retVal;
}

static int TestGenerationResults(string repoRoot, GenerationLocation csharpLocation, GenerationLocation vbLocation, GenerationLocation getTextLocation, GenerationLocation ioperationLocation)
{
    string[] actualDirs = [.. csharpLocation.GetActualDirs(), .. vbLocation.GetActualDirs(), getTextLocation.GeneratedSourceDir, ioperationLocation.GeneratedSourceDir];
    string[] tempDirs = [.. csharpLocation.GetTempDirs(), .. vbLocation.GetTempDirs(), getTextLocation.GeneratedSourceTempDir, ioperationLocation.GeneratedSourceTempDir];

    var retVal = 0;
    foreach (var (actualDir, tempDir) in actualDirs.Zip(tempDirs))
    {
        var tempFiles = Directory.GetFiles(tempDir).Select(Path.GetFileName);
        foreach (var fileName in tempFiles)
        {
            Console.WriteLine($"Verifying {fileName}");
            var actualFile = Path.Join(actualDir, fileName);
            var tempFile = Path.Join(tempDir, fileName);

            if (!File.Exists(actualFile))
            {
                Console.WriteLine($"Generated file '{actualFile}' does not exist.");
                Console.WriteLine($"Run {Path.Join(repoRoot, "eng", "generate-compiler-code.cmd")} to update it.");
                retVal = 1;
            }
            else if (!File.ReadAllText(actualFile).Equals(File.ReadAllText(tempFile), StringComparison.Ordinal))
            {
                Console.WriteLine($"Generated file '{actualFile}' is out of date.");
                Console.WriteLine($"Run {Path.Join(repoRoot, "eng", "generate-compiler-code.cmd")} to update it.");
                retVal = 1;
            }
        }
    }

    return retVal;
}

delegate int SyntaxGeneratorAction(string inputFile, string outputFile, bool writeSource, bool writeTests, bool writeGrammar);
delegate int ErrorFactsGeneratorAction(string inputPath, string outputPath);

readonly record struct GenerationLocation(string LanguageDir, string GeneratedSourceDir, string? GeneratedTestDir, string GeneratedSourceTempDir, string? GeneratedTestTempDir)
{
    public readonly IEnumerable<string> GetActualDirs()
    {
        yield return GeneratedSourceDir;
        if (GeneratedTestDir is not null)
            yield return GeneratedTestDir;
    }

    public readonly IEnumerable<string> GetTempDirs()
    {
        yield return GeneratedSourceTempDir;
        if (GeneratedTestTempDir is not null)
            yield return GeneratedTestTempDir;
    }
}
