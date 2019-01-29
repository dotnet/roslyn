# The compiler source base has a large body of generated code.  This script is responsible
# for both generating this code and verifying the generated code is always up to date with 
# the generator source files. 
[CmdletBinding(PositionalBinding=$false)]
param ([string]$configuration = "Debug", 
       [switch]$test = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Run-Tool($tool, $toolArgs) {
    $toolName = Split-Path -leaf $tool
    Write-Host "Running $toolName"
    Exec-Console $tool $toolArgs
}

function Run-LanguageCore($language, $languageSuffix, $languageDir, $syntaxTool, $errorFactsTool, $generatedDir, $generatedTestDir) {
    $syntaxFilePath = Join-Path $languageDir "Syntax\Syntax.xml"
    $syntaxTestFilePath = Join-Path $generatedTestDir "Syntax.Test.xml.Generated.$($languageSuffix)"
    $boundFilePath = Join-Path $languageDir "BoundTree\BoundNodes.xml"
    $boundGeneratedFilePath = Join-Path $generatedDir "BoundNodes.xml.Generated.$($languageSuffix)"
    $errorFileName = if ($language -eq "CSharp") { "ErrorCode.cs" } else { "Errors.vb" }
    $errorFilePath = Join-Path $languageDir "Errors\$errorFileName"
    $errorGeneratedFilePath = Join-Path $generatedDir "ErrorFacts.Generated.$($languageSuffix)"

    Create-Directory $generatedDir
    Create-Directory $generatedTestDir
    Run-Tool $syntaxTool "`"$syntaxFilePath`" `"$generatedDir`""
    Run-Tool $syntaxTool "`"$syntaxFilePath`" `"$syntaxTestFilePath`" /test"
    Run-Tool $boundTreeGenerator "$language `"$boundFilePath`" `"$boundGeneratedFilePath`""
    Run-Tool $errorFactsTool "`"$errorFilePath`" `"$errorGeneratedFilePath`""
}

# Test the contents of our generated files to ensure they are equal.  Compares our checked
# in code with the freshly generated code. 
function Test-GeneratedContent($generatedDir, $scratchDir) {
    $algo = "MD5"
    foreach ($fileName in (Get-ChildItem $scratchDir)) { 
        Write-Host "Checking $fileName"
        $realFilePath = Join-Path $generatedDir $fileName
        $scratchFilePath = Join-Path $scratchDir $fileName
        $scratchHash = (Get-FileHash $scratchFilePath -algorithm $algo).Hash
        $realHash = (Get-FileHash $realFilePath -algorithm $algo).Hash
        if ($scratchHash -ne $realHash) { 
            Write-Host "Files are out of date"
            Write-Host "Run $(Join-Path $PSScriptRoot generate-compiler-code.ps1) to refresh"
            throw "Files are out of date"
        }
    }
}

function Run-Language($language, $languageSuffix, $languageDir, $languageTestDir, $syntaxTool, $errorFactsTool) {
    $generatedDir = Join-Path $languageDir "Generated"
    $generatedTestDir = Join-Path $languageTestDir "Generated"
    if (-not $test) { 
        Run-LanguageCore $language $languageSuffix $languageDir $syntaxTool $errorFactsTool $generatedDir $generatedTestDir
    }
    else {
        $scratchDir = Join-Path $generationTempDir "$language\Src"
        $scratchTestDir = Join-Path $generationTempDir "$language\Test"
        Run-LanguageCore $language $languageSuffix $languageDir $syntaxTool $errorFactsTool $scratchDir $scratchTestDir
        Test-GeneratedContent $generatedDir $scratchDir
        Test-GeneratedContent $generatedTestDir $scratchTestDir
    }
}

function Run-GetTextCore($generatedDir) {
    $syntaxFilePath = Join-Path $basicDir "Syntax\Syntax.xml"
    $syntaxTextFilePath = Join-Path $generatedDir "Syntax.xml.GetText.Generated.vb"

    Create-Directory $generatedDir
    Run-Tool $basicSyntaxGenerator "`"$syntaxFilePath`" `"$syntaxTextFilePath`" /gettext"
}

function Run-GetText() {
    $generatedDir = Join-Path $RepoRoot "src\ExpressionEvaluator\VisualBasic\Source\ResultProvider\Generated"
    if (-not $test) { 
        Run-GetTextCore $generatedDir
    }
    else {
        $scratchDir = Join-Path $generationTempDir "VB\GetText"
        Run-GetTextCore $scratchDir
        Test-GeneratedContent $generatedDir $scratchDir
    }
}

# Build all of the tools that we need to generate the syntax trees and ensure
# they are in a published / runnable state.
function Build-Tools() {
    $list = @(
        'boundTreeGenerator;BoundTreeGenerator;BoundTreeGenerator\CompilersBoundTreeGenerator.csproj',
        'csharpErrorFactsGenerator;CSharpErrorFactsGenerator;CSharpErrorFactsGenerator\CSharpErrorFactsGenerator.csproj',
        'csharpSyntaxGenerator;CSharpSyntaxGenerator;CSharpSyntaxGenerator\CSharpSyntaxGenerator.csproj',
        'basicErrorFactsGenerator;VBErrorFactsGenerator;VisualBasicErrorFactsGenerator\VisualBasicErrorFactsGenerator.vbproj',
        'basicSyntaxGenerator;VBSyntaxGenerator;VisualBasicSyntaxGenerator\VisualBasicSyntaxGenerator.vbproj')

    $dotnet = Ensure-DotnetSdk
    $tfm = "netcoreapp2.1"
    $rid = "win-x64"

    $sourceDir = Join-Path $RepoRoot 'src\Tools\Source\CompilerGeneratorTools\Source'
    foreach ($item in $list) { 
        $all = $item.Split(';')
        $varName = $all[0]
        $exeName = $all[1]
        $proj = Join-Path $sourceDir $all[2]
        $fileName = [IO.Path]::GetFileNameWithoutExtension($proj)
        Write-Host "Building $fileName"

        Exec-Command $dotnet "publish /p:Configuration=$configuration /p:RuntimeIdentifier=$rid /v:m `"$proj`"" | Out-Null

        $exePath = GetProjectOutputBinary "$exeName.exe" -configuration:$configuration -projectName:$fileName -tfm:$tfm -rid:$rid -published:$true
        if (-not (Test-Path $exePath)) {
            throw "Did not find exe after build: $exePath"
        }

        Set-Variable -Name $varName -Value $exePath -Scope Script
    }
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")

    Build-Tools

    $csharpDir = Join-Path $RepoRoot "src\Compilers\CSharp\Portable"
    $csharpTestDir = Join-Path $RepoRoot "src\Compilers\CSharp\Test\Syntax"
    $basicDir = Join-Path $RepoRoot "src\Compilers\VisualBasic\Portable"
    $basicTestDir = Join-Path $RepoRoot "src\Compilers\VisualBasic\Test\Syntax"
    $generationTempDir = Join-Path $TempDir "Generated"

    Run-Language "CSharp" "cs" $csharpDir $csharpTestDir $csharpSyntaxGenerator $csharpErrorFactsGenerator
    Run-Language "VB" "vb" $basicDir $basicTestDir $basicSyntaxGenerator $basicErrorFactsGenerator
    Run-GetText

    exit 0
}
catch {
    Write-Host $_
    exit 1
}
