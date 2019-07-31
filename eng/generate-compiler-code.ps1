# The compiler source base has a large body of generated code.  This script is responsible
# for both generating this code and verifying the generated code is always up to date with 
# the generator source files. 
[CmdletBinding(PositionalBinding=$false)]
param ([string]$configuration = "Debug", 
       [switch]$test = $false)

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

function Run-Tool($projectFilePath, $toolArgs) {
  $toolName = Split-Path -leaf $projectFilePath
  Write-Host "Running $toolName $toolArgs"
  Exec-Console $dotnet "run -p $projectFilePath $toolArgs"
}

function Run-LanguageCore($language, $languageSuffix, $languageDir, $syntaxProject, $errorFactsProject, $generatedDir, $generatedTestDir) {
  $syntaxFilePath = Join-Path $languageDir "Syntax\Syntax.xml"
  $syntaxTestFilePath = Join-Path $generatedTestDir "Syntax.Test.xml.Generated.$($languageSuffix)"
  $boundFilePath = Join-Path $languageDir "BoundTree\BoundNodes.xml"
  $boundGeneratedFilePath = Join-Path $generatedDir "BoundNodes.xml.Generated.$($languageSuffix)"
  $errorFileName = if ($language -eq "CSharp") { "ErrorCode.cs" } else { "Errors.vb" }
  $errorFilePath = Join-Path $languageDir "Errors\$errorFileName"
  $errorGeneratedFilePath = Join-Path $generatedDir "ErrorFacts.Generated.$($languageSuffix)"

  Create-Directory $generatedDir
  Create-Directory $generatedTestDir
  Run-Tool $syntaxProject "`"$syntaxFilePath`" `"$generatedDir`""
  Run-Tool $syntaxProject "`"$syntaxFilePath`" `"$syntaxTestFilePath`" /test"
  Run-Tool $boundTreeGenProject "$language `"$boundFilePath`" `"$boundGeneratedFilePath`""
  Run-Tool $errorFactsProject "`"$errorFilePath`" `"$errorGeneratedFilePath`""
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

function Run-IOperation($coreDir, $ioperationProject) {
  $operationsDir = Join-Path $coreDir "Operations"
  $operationsXml = Join-Path $operationsDir "OperationInterfaces.xml"

  if (-not $test) {
    Run-Tool $ioperationProject "`"$operationsXml`" `"$operationsDir`""
  } else {
    $scratchDir = Join-Path $generationTempDir "Core\Operations"
    Run-Tool $ioperationProject "`"$operationsXml`" `"$scratchDir`""
    # Test-GeneratedContent $operationsDir $scratchDir
  }
}

function Run-GetTextCore($generatedDir) {
  $syntaxFilePath = Join-Path $basicDir "Syntax\Syntax.xml"
  $syntaxTextFilePath = Join-Path $generatedDir "Syntax.xml.GetText.Generated.vb"

  Create-Directory $generatedDir
  Run-Tool $basicSyntaxProject "`"$syntaxFilePath`" `"$syntaxTextFilePath`" /gettext"
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

function Get-ToolPath($projectRelativePath) {
  $p = Join-Path 'src\Tools\Source\CompilerGeneratorTools\Source' $projectRelativePath
  $p = Join-Path $RepoRoot $p
  return $p
}

try {
  . (Join-Path $PSScriptRoot "build-utils.ps1")
  Push-Location $RepoRoot

  $dotnet = Ensure-DotnetSdk
  $boundTreeGenProject = Get-ToolPath 'BoundTreeGenerator\CompilersBoundTreeGenerator.csproj'

  $coreDir = Join-Path $RepoRoot "src\Compilers\Core\Portable"
  $operationsProject = Get-ToolPath "IOperationGenerator\CompilersIOperationGenerator.csproj"
  $csharpDir = Join-Path $RepoRoot "src\Compilers\CSharp\Portable"
  $csharpTestDir = Join-Path $RepoRoot "src\Compilers\CSharp\Test\Syntax"
  $csharpSyntaxProject = Get-ToolPath 'CSharpSyntaxGenerator\CSharpSyntaxGenerator.csproj'
  $csharpErrorFactsProject = Get-ToolPath 'CSharpErrorFactsGenerator\CSharpErrorFactsGenerator.csproj'
  $basicDir = Join-Path $RepoRoot "src\Compilers\VisualBasic\Portable"
  $basicTestDir = Join-Path $RepoRoot "src\Compilers\VisualBasic\Test\Syntax"
  $basicSyntaxProject = Get-ToolPath 'VisualBasicSyntaxGenerator\VisualBasicSyntaxGenerator.vbproj'
  $basicErrorFactsProject = Get-ToolPath 'VisualBasicErrorFactsGenerator\VisualBasicErrorFactsGenerator.vbproj'
  $generationTempDir = Join-Path $RepoRoot "artifacts\log\$configuration\Generated"

  Run-Language "CSharp" "cs" $csharpDir $csharpTestDir $csharpSyntaxProject $csharpErrorFactsProject
  Run-Language "VB" "vb" $basicDir $basicTestDir $basicSyntaxProject $basicErrorFactsProject 
  Run-IOperation $coreDir $operationsProject
  Run-GetText

  exit 0
}
catch {
  Write-Host $_
  exit 1
}
finally {
  Pop-Location
}
