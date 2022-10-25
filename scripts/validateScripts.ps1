[CmdletBinding(PositionalBinding=$false)]
param ([switch] $ci)

. (Join-Path $PSScriptRoot "../eng/build-utils.ps1")

$PSScriptAnalyzerDir = Join-Path $ToolsDir "PSScriptAnalyzer"
$PSScriptAnalyzerPkg = Join-Path $PSScriptAnalyzerDir "PSScriptAnalyzer.nupkg"

if (!(Test-Path $PSScriptAnalyzerDir)) {
  Create-Directory $PSScriptAnalyzerDir
  Write-Host 'Downloading PSScriptAnalyzer'
  Retry({
    Invoke-WebRequest "https://github.com/PowerShell/PSScriptAnalyzer/releases/download/1.21.0/PSScriptAnalyzer.1.21.0.nupkg" -UseBasicParsing -OutFile $PSScriptAnalyzerPkg
  })
  Unzip $PSScriptAnalyzerPkg $PSScriptAnalyzerDir
}

Import-Module (Join-Path $PSScriptAnalyzerDir "PSScriptAnalyzer.psd1")
$Settings = @{
  IncludeRules=@(
    'PSAvoidAssignmentToAutomaticVariable', # We shouldn't be assigning to automatic variables except in special cases
    'PSAvoidDefaultValueForMandatoryParameter', # Mandatory parameters should not have a default values because there is no scenario where the default can be used
    'PSAvoidDefaultValueSwitchParameter', # it is unusual to see a switch parameter with a default value of $true
    'PSAvoidGlobalAliases', # Globally scoped aliases override existing aliases within the sessions with matching names
    'PSAvoidGlobalFunctions', # Globally scoped functions override existing functions within the sessions with matching names
    'PSAvoidGlobalVars', # Globally scoped variables should be used sparingly
    'PSAvoidInvokingEmptyMembers', # invoking empty members is a no-op and is likely a typo
    'PSAvoidOverwritingBuiltInCmdlets', # We should not be re-defining built-in cmdlets
    'PSAvoidSemicolonsAsLineTerminators', # Lines should not end with a semicolon.
    'PSAvoidUsingDoubleQuotesForConstantString', # Single quotes should be used when the value of a string is constant so you don't include variables by mistake
    'PSMisleadingBacktick', # Checks that lines don't end with a backtick followed by whitespace.
    'PSPossibleIncorrectComparisonWithNull', # To ensure that PowerShell performs comparisons correctly, the $null element should be on the left side of the operator.
    'PSPossibleIncorrectUsageOfAssignmentOperator', # catches use of == or =, instead of -eq
    'PSPossibleIncorrectUsageOfRedirectionOperator', # catches use of > or >= instead of -gt
    'AvoidUsingPositionalParameters', # Positional parameters make the is easier to break consuming scripts when a new parameter is added
    'PSReservedParams', # You cannot use reserved common parameters in an advanced function
    'PSReviewUnusedParameter', # finds unused parameters in a scope (but not child scopes). We should be passing parameters to child scopes for clarity
    'PSUseCmdletCorrectly', # finds some runtime errors for knwon cmdlets
    'PSUseCompatibleSyntax', # identifies syntax elements that are incompatible with targeted PowerShell versions
    'PSUseDeclaredVarsMoreThanAssignments' # detects unused variables (unless they are intended to be used in a child scope)
  )
  Rules = @{
    'PSAvoidOverwritingBuiltInCmdlets' = @{
      Enable = $true
      PowerShellVersion = @('desktop-5.1.14393.206-windows', 'core-6.1.0-windows')
    }
    'PSUseCompatibleTypes' = @{
      Enable = $true
      TargetProfiles = @('win-48_x64_10.0.17763.0_5.1.17763.316_x64_4.0.30319.42000_framework', 'ubuntu_x64_18.04_7.0.0_x64_3.1.2_core')
    }
    'PSUseCompatibleSyntax' = @{
      Enable = $true
      TargetVersions = @('5.1', '6.0', '7.0')
    }
  }
}

$engAnalysisResults = Invoke-ScriptAnalyzer -Path $EngRoot -Settings $Settings # don't use -Recurse because we don't want to analyze the common directory
$scriptAnalysisResults = Invoke-ScriptAnalyzer -Path (Join-Path $RepoRoot 'scripts') -Recurse -Settings $Settings
$srcAnalysisResults = Invoke-ScriptAnalyzer -Path (Join-Path $RepoRoot 'src') -Recurse -Settings $Settings

$allAnalysisResults = @($engAnalysisResults) + $scriptAnalysisResults
$allAnalysisResults = @($allAnalysisResults) + $srcAnalysisResults

if ($allAnalysisResults.Count -gt 0) {
  if($true -eq $ci){
    foreach ($analysisResult in $allAnalysisResults) {
      $name = $analysisResult.RuleName
      $message = $analysisResult.Message
      Write-PipelineTelemetryError -Category 'Analysis' -Message "$name : $message" -SourcePath $analysisResult.ScriptPath -Line $analysisResult.Line
    }
  }

  else{
    Write-Host ($allAnalysisResults | Format-Table ScriptPath,Line,RuleName,Message | Out-String)
  }
  
  Write-Host "Found $($allAnalysisResults.Count) issues in scripts."
  ExitWithExitCode 1
}

Write-Host "No issues found in scripts."
ExitWithExitCode 0