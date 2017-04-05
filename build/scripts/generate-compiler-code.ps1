# The compiler source base has a large body of generated code.  This script is responsible
# for both generating this code and verifying the generated code is always up to date with 
# the generator source files. 
[CmdletBinding(PositionalBinding=$false)]
param ([switch]$test = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Run-Tool($tool, $toolArgs) {
    $toolName = Split-path -leaf $tool
    Write-Host "Running $toolName"
    if (-not (Test-Path $tool)) {
        throw "$tool does not exist"
    }

    Invoke-Expression "& `"$coreRun`" `"$tool`" $toolArgs"
    if ((-not $?) -or ($lastexitcode -ne 0)) {
        throw "Failed"
    }
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
        $scratchDir = Join-Path $binariesDir "Generated\$language\Src"
        $scratchTestDir = Join-Path $binariesDir "Generated\$language\Test"
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
    $generatedDir = Join-Path $repoDir "src\ExpressionEvaluator\VisualBasic\Source\ResultProvider\Generated"
    if (-not $test) { 
        Run-GetTextCore $generatedDir
    }
    else {
        $scratchDir = Join-Path $binariesDir "Generated\VB\GetText"
        Run-GetTextCore $scratchDir
        Test-GeneratedContent $generatedDir $scratchDir
    }
}

try {
    . (Join-Path $PSScriptRoot "build-utils.ps1")

    $compilerToolsDir = Join-Path $binariesDir "Debug\Exes\DeployCompilerGeneratorToolsRuntime"
    $csharpSyntaxGenerator = Join-Path $compilerToolsDir "CSharpSyntaxGenerator.exe"
    $csharpErrorFactsGenerator = Join-Path $compilerToolsDir "CSharpErrorFactsGenerator.exe"
    $csharpDir = Join-Path $repoDir "src\Compilers\CSharp\Portable"
    $csharpTestDir = Join-Path $repoDir "src\Compilers\CSharp\Test\Syntax"
    $basicSyntaxGenerator = Join-Path $compilerToolsDir "VBSyntaxGenerator.exe"
    $basicErrorFactsGenerator = Join-Path $compilerToolsDir "VBErrorFactsGenerator.exe"
    $basicDir = Join-Path $repoDir "src\Compilers\VisualBasic\Portable"
    $basicTestDir = Join-Path $repoDir "src\Compilers\VisualBasic\Test\Syntax"
    $boundTreeGenerator = Join-Path $compilerToolsDir "BoundTreeGenerator.exe"
    $coreRun = Join-Path $compilerToolsDir "CoreRun.exe"

    Run-Language "CSharp" "cs" $csharpDir $csharpTestDir $csharpSyntaxGenerator $csharpErrorFactsGenerator
    Run-Language "VB" "vb" $basicDir $basicTestDir $basicSyntaxGenerator $basicErrorFactsGenerator
    Run-GetText

    exit 0
}
catch {
  Write-Host $_
  exit 1
}
