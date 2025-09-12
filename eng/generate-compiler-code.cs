#!/usr/bin/env dotnet
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Need this fix to delete the static graph disable: https://github.com/dotnet/sdk/pull/50532
#:property RestoreUseStaticGraphEvaluation=false
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/BoundTreeGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/IOperationGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/CSharpSyntaxGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/CSharpErrorFactsGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/VisualBasicSyntaxGenerator/
#:project ../src/Tools/Source/CompilerGeneratorTools/Source/VisualBasicErrorFactsGenerator/

using System.Runtime.CompilerServices;

var repoRoot = GetRoslynDirectory();

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
            return 1;
    }
}

var csharpDir = Path.Join(repoRoot, "src", "Compilers", "CSharp", "Portable");
var vbDir = Path.Join(repoRoot, "src", "Compilers", "VisualBasic", "Portable");

// Folders for test artifacts when running in -test mode
var generationTempDirRoot = Path.Join(repoRoot, "artifacts", "log", configuration, "Generated");

var generatedCSharpDir = Path.Join(csharpDir, "Generated");
var generatedCSharpTestDir = Path.Join(repoRoot, "src", "Compilers", "CSharp", "Test", "Syntax", "Generated");
var generatedTempCSharpDir = Path.Join(generationTempDirRoot, "CSharp", "Src");
var generatedTempCSharpTestDir = Path.Join(generationTempDirRoot, "CSharp", "Test");

var retVal = 0;

retVal = GenerateLanguage(
    BoundTreeGenerator.TargetLanguage.CSharp,
    csharpDir,
    test ? generatedTempCSharpDir : generatedCSharpDir,
    test ? generatedTempCSharpTestDir : generatedCSharpTestDir,
    (inputFile, outputFile, writeSource, writeTests, writeGrammar)
        => CSharpSyntaxGenerator.Program.Generate(inputFile, outputFile, writeSource, writeTests, writeGrammar, writeSignatures: false),
    Microsoft.CodeAnalysis.CSharp.Internal.CSharpErrorFactsGenerator.Program.Generate);

var generatedVisualBasicDir = Path.Join(vbDir, "Generated");
var generatedVisualBasicTestDir = Path.Join(repoRoot, "src", "Compilers", "VisualBasic", "Test", "Syntax", "Generated");
var generatedTempVisualBasicDir = Path.Join(generationTempDirRoot, "VisualBasic", "Src");
var generatedTempVisualBasicTestDir = Path.Join(generationTempDirRoot, "VisualBasic", "Test");

retVal |= GenerateLanguage(
    BoundTreeGenerator.TargetLanguage.VB,
    vbDir,
    test ? generatedTempVisualBasicDir : generatedVisualBasicDir,
    test ? generatedTempVisualBasicTestDir : generatedVisualBasicTestDir,
    Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Program.Generate,
    Microsoft.CodeAnalysis.VisualBasic.Internal.VBErrorFactsGenerator.Program.Generate);

var basicXmlFile = Path.Join(vbDir, "Syntax", "Syntax.xml");
var generatedGetTextDirectory = Path.Join(repoRoot, "src", "ExpressionEvaluator", "VisualBasic", "Source", "ResultProvider", "Generated");
var generatedGetTextFile = Path.Join(generatedGetTextDirectory, "Syntax.xml.GetText.Generated.vb");
var generatedTempGetTextDirectory = Path.Join(generationTempDirRoot, "VisualBasic", "GetText");
var generatedTempGetTextFile = Path.Join(generatedTempGetTextDirectory, "Syntax.xml.GetText.Generated.vb");
if (test)
{
    Directory.CreateDirectory(generatedTempGetTextDirectory);
}
Microsoft.CodeAnalysis.VisualBasic.Internal.VBSyntaxGenerator.Program.Generate(basicXmlFile, test ? generatedTempGetTextFile : generatedGetTextFile, writeSource: false, writeTests: false, writeGrammar: false);

var coreDir = Path.Join(repoRoot, "src", "Compilers", "Core", "Portable");
var operationsXml = Path.Join(coreDir, "Operations", "OperationInterfaces.xml");
var generatedOperationsDir = Path.Join(coreDir, "Generated");
var generatedTempOperationsDir = Path.Join(generationTempDirRoot, "Core", "Src");
if (test)
{
    Directory.CreateDirectory(generatedTempOperationsDir);
}
retVal |= IOperationGenerator.Program.Generate(operationsXml, test ? generatedTempOperationsDir : generatedOperationsDir);

if (test)
{
    string[] actualDirs = [generatedCSharpDir, generatedCSharpTestDir, generatedVisualBasicDir, generatedVisualBasicTestDir, generatedOperationsDir, generatedGetTextDirectory];
    string[] tempDirs = [generatedTempCSharpDir, generatedTempCSharpTestDir, generatedTempVisualBasicDir, generatedTempVisualBasicTestDir, generatedTempOperationsDir, generatedTempGetTextDirectory];

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
}

return retVal;

static string GetRoslynDirectory([CallerFilePath] string sourceFilePath = "")
{
    if (Path.GetDirectoryName(sourceFilePath) is not string engDir || Path.GetDirectoryName(engDir) is not string roslynRoot || !File.Exists(Path.Join(roslynRoot, "eng", Path.GetFileName(sourceFilePath))))
    {
        throw new InvalidOperationException("Could not determine source file path. This script must be located in the 'eng' directory of the Roslyn repo.");
    }

    return roslynRoot;
}

static int GenerateLanguage(BoundTreeGenerator.TargetLanguage language, string languageDir, string generatedDir, string generatedTestDir, SyntaxGeneratorAction syntaxGenerator, ErrorFactsGeneratorAction errorFactsGenerator)
{
    Directory.CreateDirectory(generatedDir);
    Directory.CreateDirectory(generatedTestDir);
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

delegate int SyntaxGeneratorAction(string inputFile, string outputFile, bool writeSource, bool writeTests, bool writeGrammar);
delegate int ErrorFactsGeneratorAction(string inputPath, string outputPath);
