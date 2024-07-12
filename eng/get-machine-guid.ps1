Function checkGuid {
    Param([Guid] $guidCheck)

}

Function HashSha1($textToHash) {
    $hasher = new-object System.Security.Cryptography.SHA1Managed
    $toHash = [System.Text.Encoding]::UTF8.GetBytes($textToHash)
    $hashByteArray = $hasher.ComputeHash($toHash)

    foreach ($byte in $hashByteArray) {
        $res += $byte.ToString("x2")
    }
    return $res;
}
Function ScrubMachineGuid($machineGuid) {
    $temp = HashSha1($machineGuid)
    $scrubbed = "PII_" + $temp.ToLower()
    return $scrubbed
}

Function Get-MachineGuid() {
    try {
        Write-Host "Getting machine guid"
        $guid = Get-ItemProperty -Path HKLM:\SOFTWARE\Microsoft\RemovalTools\MRT -Name "GUID"
        $guid = $guid.GUID.tolower()
        checkGuid($guid)
        $scrubbedGuid = ScrubMachineGuid($guid)
        $stringGuid = [string]$scrubbedGuid

        Write-Host "Found machine guid: "
        Write-Host $guid
        Write-Host $stringGuid
    }
    catch {
        Write-Error $_.Exception.Message
    }
}

Get-MachineGuid