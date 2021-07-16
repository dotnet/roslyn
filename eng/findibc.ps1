[CmdletBinding(PositionalBinding=$false)]
param (
  [string]$intermediateAssembly,
  [string]$ibcRootFolder)

try {
  $assemblyName = [System.IO.Path]::GetFileName($intermediateAssembly)
  $fullPath = [System.IO.Path]::GetFullPath($ibcRootFolder)
  if(![System.IO.Directory]::Exists($fullPath)){
    # There is no product data directory return
    return ""
  }
  $root = (New-Object -TypeName System.IO.DirectoryInfo -ArgumentList $fullPath)
  $dllEntry = [System.Linq.Enumerable]::SingleOrDefault($root.EnumerateFiles($assemblyName,[System.IO.SearchOption]::AllDirectories))
  if (!$dllEntry)
  {
    return "";
  }

  $ibcFileInfos = $dllEntry.Directory.EnumerateFiles("*.ibc")
  $strings = (New-Object "System.Collections.Generic.List[System.String]")
  foreach ($ibcFileInfo in $ibcFileInfos)
  {
    $name = $ibcFileInfo.FullName
    $strings.Add($name)
  }
  $ibcFiles = $strings -join ' '

  return $ibcFiles
}
catch {
  Write-Host $_
  Write-Host $_.Exception
  Write-Host $_.ScriptStackTrace
  exit 1
}
finally {
  Pop-Location
}
