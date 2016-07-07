<#
.SYNOPSIS
Runs signtool on all shipping binaries and fails the build if any of them aren't signed.

.PARAMETER SignToolPath
The full path to signtool.exe that should be used to verify signatures.

.PARAMETER BinariesDirectory
The root directory where the build outputs are written.

.EXAMPLE
C:\PS> .\RunSignCop.ps1 -SignToolPath D:\Tools\SignTool\SignTool.exe -SignCheckToolPath D:\Tools\SignCheck.exe -BinariesDirectory D:\Roslyn\Binaries\Debug
#>

Param (
    [Parameter(Mandatory = $true)]
    [String]$SignToolPath,
    [Parameter(Mandatory = $true)]
    [String]$SignCheckToolPath,
    [Parameter(Mandatory = $true)]
    [String]$BinariesDirectory
)

# MAIN BODY
try {
    $exitCode = 0
    
    if (!(Test-Path $SignToolPath)) {
        Write-Error "Unable to find sign tool: $SignToolPath"
        $exitCode++
    }
    
    if (!(Test-Path $SignCheckToolPath)) {
        Write-Error "Unable to find sign check tool: $SignCheckToolPath"
        $exitCode++
    }

    if (!(Test-Path $BinariesDirectory)) {
        Write-Error "Unable to find binaries root directory: $BinariesDirectory"
        $exitCode++
    }

    $apiPath = Join-Path -Path $BinariesDirectory -ChildPath "DevDivInsertionFiles\ExternalApis\"
    if (!(Test-Path $apiPath)) {
        Write-Error "Unable to find api directory: $apiPath"
        $exitCode++
    }
    
    if ($exitCode -eq 0) {
        
        # Recurse through ExternalApis directory and collect all shipping binaries
        $apiDir = Get-Item $apiPath
        $binaries = Get-ChildItem $apiDir\* -Recurse -Include *.dll, *.exe | where {$_.FullName -notlike "*test*"}
        
        # Foreach shipping binary, invoke signtool and verify all signatures are present
        $result = $binaries | foreach {& $SignToolPath verify /pa /all $_.FullName 2>&1}
        $errors = $result -like "*error*"
        if($errors.Count -gt 0)
        {
            $errorFile = Join-Path $BinariesDirectory "SignVerificationErrors.txt"
            if (Test-Path $errorFile) {
                Remove-Item $errorFile
            }
            
            $result | Out-File $errorFile -force
            Write-Error "SignTool failed with errors. See $errorFile"
            $exitCode++
        }
    }

    if ($exitCode -eq 0) {

        # Dig into nuget and vsix packages to verify signing on all dlls inside of them
        $errorFile = Join-Path $BinariesDirectory "SignCheck.log"

        & $SignCheckToolPath $BinariesDirectory -verifyrelease -logfile:$errorFile
        if ($LASTEXITCODE -ne 0)
        {
            Write-Error "SignCheck failed with errors. See $errorFile"
            $exitCode++
        }
    }
    
    exit $exitCode
}
catch [exception] 
{
    Write-Error -Exception $_.Exception
    exit 1
}