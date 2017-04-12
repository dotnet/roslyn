# Copied from http://jeffhandley.com/archive/2012/12/13/Bulk-Publishing-NuGet-Packages.aspx

$defaultGalleryUrl = "https://ms-nuget.cloudapp.net"

function Submit-Package {
  <#
  .SYNOPSIS
  Submits a NuGet package (or set of packages) to the gallery, but as hidden (unlisted).
  .DESCRIPTION
  Uploads the specified package (or all packages from a packages.config file) to the gallery and then immediately marks the package(s) as unlisted by running the nuget delete command.
  .EXAMPLE
  Submit-Package -packageId MyAwesomePackage -packageVersion 2.0.0.0 -packageFile MyAwesomePackage.2.0.0.nupkg -apiKey 00000000-0000-0000-0000-000000000000 -galleryUrl https://nuget.org
  .EXAMPLE
  Submit-Package -packagesConfig packages.config -apiKey 00000000-0000-0000-0000-000000000000 -galleryUrl https://nuget.org
  .PARAMETER packageId
  The Id of the package to hide/show
  .PARAMETER packageVersion
  The Version of the package to hide/show
  .PARAMETER packageFile
  The nupkg file to upload for the NuGet package
  .PARAMETER packagesConfig
  The XML config file that lists the packages to be hidden/shown
  .PARAMETER galleryUrl
  The NuGet gallery Url to connect to.  By default, https://nuget.org
  #>
  param(
    [Parameter(ParameterSetName="package")] $packageId,
    [Parameter(ParameterSetName="package")] $packageVersion,
    [Parameter(ParameterSetName="package")] $packageFile,
    [Parameter(ParameterSetName="config")]  $packagesConfig,
    $apiKey,
    $galleryUrl = $defaultGalleryUrl
  )

  If ($apiKey -eq $null) { throw "Parameter 'apiKey' was not specified" }
  If ($galleryUrl -eq $null) { throw "Parameter 'galleryUrl' was not specified" }

  If ($PSCmdlet.ParameterSetName -match "package") {
    If ($packageId -eq $null) { throw "Parameter 'packageId' was not specified" }
    If ($packageVersion -eq $null) { throw "Parameter 'packageVersion' was not specified" }
    If ($packageFile -eq $null) { throw "Parameter 'packageFile' was not specified" }

    $exists = Test-Path $packageFile
    if ($exists -eq $false)
    {
      throw "File not found: $packageFile"
    }

    PushDelete -packageId $packageId -packageVersion $packageVersion -packageFile $packageFile -apiKey $apiKey -galleryUrl $galleryUrl
  }
  ElseIf ($PSCmdlet.ParameterSetName -match "config") {
    If ($packagesConfig -eq $null) { throw "Parameter 'packagesConfig' was not specified" }
    If (!(Test-Path $packagesConfig)) { throw "File '$packagesConfig' was not found" }

    [xml]$packages = Get-Content $packagesConfig

    foreach ($package in $packages.packages.package) {
      $path = ".\" + $package.culture + "\" + $package.id + "." + $package.version + ".nupkg"
      $path = $path.Replace("\\", "\")

      $exists = Test-Path $path
      if ($exists -eq $false)
      {
        throw "File not found: $path"
      }

      PushDelete -packageId $package.id -packageVersion $package.version -packageFile $path -apiKey $apiKey -galleryUrl $galleryUrl
    }
  }
}

function Set-NuGetPackageVisibility {
  <#
  .SYNOPSIS
  Sets a package's visibility within the NuGet gallery
  .DESCRIPTION
  Hide (unlist) a package from the gallery or show (list) a package on the gallery.
  .EXAMPLE
  Set-PackageVisibility -action hide -packageId MyAwesomePackage -packageVersion 2.0.0.0 -apiKey 00000000-0000-0000-0000-000000000000 -galleryUrl https://nuget.org
  .EXAMPLE
  Set-PackageVisibility -action show -packageId MyAwesomePackage -packageVersion 2.0.0.0 -apiKey 00000000-0000-0000-0000-000000000000 -galleryUrl https://preview.nuget.org
  .PARAMETER action
  The action to take: hide or show
  .PARAMETER packageId
  The Id of the package to hide/show
  .PARAMETER packageVersion
  The Version of the package to hide/show
  .PARAMETER packagesConfig
  The XML config file that lists the packages to be hidden/shown
  .PARAMETER galleryUrl
  The NuGet gallery Url to connect to.  By default, https://nuget.org
  #>

  [CmdletBinding(DefaultParameterSetName='package')]
  param(
    $action,
    [Parameter(ParameterSetName="package")] $packageId,
    [Parameter(ParameterSetName="package")] $packageVersion,
    [Parameter(ParameterSetName="config")]  $packagesConfig,
    $apiKey,
    $galleryUrl = $defaultGalleryUrl
  )

  If ($action -eq $null) { throw "Parameter 'action' was not specified" }
  If ($apiKey -eq $null) { throw "Parameter 'apiKey' was not specified" }
  If ($galleryUrl -eq $null) { throw "Parameter 'galleryUrl' was not specified" }

  If ($PSCmdlet.ParameterSetName -match "package") {
    If ($packageId -eq $null) { throw "Parameter 'packageId' was not specified" }
    If ($packageVersion -eq $null) { throw "Parameter 'packageVersion' was not specified" }

    SetVisibility -action $action -packageId $packageId -packageVersion $packageVersion -apiKey $apiKey -galleryUrl $galleryUrl
  }
  ElseIf ($PSCmdlet.ParameterSetName -match "config") {
    If ($packagesConfig -eq $null) { throw "Parameter 'packagesConfig' was not specified" }
    If (!(Test-Path $packagesConfig)) { throw "File '$packagesConfig' was not found" }

    [xml]$packages = Get-Content $packagesConfig

    foreach ($package in $packages.packages.package) {
      SetVisibility -action $action -packageId $package.id -packageVersion $package.version -apiKey $apiKey -galleryUrl $galleryUrl
    }
  }
}

function Hide-NuGetPackage {
  <#
  .SYNOPSIS
  Hides a package from the NuGet gallery
  .DESCRIPTION
  Marks the specified NuGet package as unlisted, hiding it from the gallery.
  .EXAMPLE
  Hide-NuGetPackage -packageId MyAwesomePackage -packageVersion 2.0.0.0 -apiKey 00000000-0000-0000-0000-000000000000 -galleryUrl https://preview.nuget.org
  .PARAMETER packageId
  The Id of the package to hide
  .PARAMETER packageVersion
  The Version of the package to hide
  .PARAMETER packagesConfig
  The XML config file that lists the packages to be hidden/shown
  .PARAMETER galleryUrl
  The NuGet gallery Url to connect to.  By default, https://nuget.org
  #>

  param(
    [Parameter(ParameterSetName="package")] $packageId,
    [Parameter(ParameterSetName="package")] $packageVersion,
    [Parameter(ParameterSetName="config")]  $packagesConfig,
    $apiKey,
    $galleryUrl = $defaultGalleryUrl
  )

  If ($PSCmdlet.ParameterSetName -match "config") {
    Set-NuGetPackageVisibility -action hide -packagesConfig $packagesConfig -apiKey $apiKey -galleryUrl $galleryUrl
  }
  Else {
    Set-NuGetPackageVisibility -action hide -packageId $packageId -packageVersion $packageVersion -apiKey $apiKey -galleryUrl $galleryUrl
  }
}

function Show-NuGetPackage {
  <#
  .SYNOPSIS
  Shows a package on the NuGet gallery, listing an already-published but unlisted package.
  .DESCRIPTION
  Marks the specified NuGet package as listed, showing it on the gallery.
  .EXAMPLE
  Show-NuGetPackage -packageId MyAwesomePackage -packageVersion 2.0.0.0 -apiKey 00000000-0000-0000-0000-000000000000 -galleryUrl https://preview.nuget.org
  .PARAMETER packageId
  The Id of the package to show
  .PARAMETER packageVersion
  The Version of the package to show
  .PARAMETER packagesConfig
  The XML config file that lists the packages to be hidden/shown
  .PARAMETER galleryUrl
  The NuGet gallery Url to connect to.  By default, https://nuget.org
  #>

  param(
    [Parameter(ParameterSetName="package")] $packageId,
    [Parameter(ParameterSetName="package")] $packageVersion,
    [Parameter(ParameterSetName="config")]  $packagesConfig,
    $apiKey,
    $galleryUrl = $defaultGalleryUrl
  )

  If ($PSCmdlet.ParameterSetName -match "config") {
    Set-NuGetPackageVisibility -action show -packagesConfig $packagesConfig -apiKey $apiKey -galleryUrl $galleryUrl
  }
  Else {
    Set-NuGetPackageVisibility -action show -packageId $packageId -packageVersion $packageVersion -apiKey $apiKey -galleryUrl $galleryUrl
  }
}

function PushDelete {
  param(
    $packageId,
    $packageVersion,
    $packageFile,
    $apiKey,
    $galleryUrl
  )

  nuget push $packageFile -source $galleryUrl -apiKey $apiKey
  nuget delete $packageId $packageVersion -source $galleryUrl -noninteractive -apiKey $apiKey
}

function SetVisibility {
  param(
    $action,
    $packageId,
    $packageVersion,
    $apiKey,
    $galleryUrl
  )
  If ($action -match "hide") {
    $method = "DELETE"
    $message = "hidden (unlisted)"
  }
  ElseIf ($action -match "show") {
    $method = "POST"
    $message = "shown (listed)"
  }
  Else {
    throw "Invalid 'action' parameter value.  Valid values are 'hide' and 'show'."
  }

  $url = "$galleryUrl/api/v2/Package/$packageId/$packageVersion"
  $web = [System.Net.WebRequest]::Create($url)

  $web.Method = $method
  $web.Headers.Add("X-NuGet-ApiKey", "$apiKey")
  $web.ContentLength = 0

  Write-Host ""
  Write-Host "Submitting the $method request to $url..." -foregroundColor Cyan
  Write-Host ""

  $response = $web.GetResponse()

  If ($response.StatusCode -match "OK") {
    Write-Host "Package '$packageId' Version '$packageVersion' has been $message." -foregroundColor Green -backgroundColor Black
    Write-Host ""
  }
  Else {
    Write-Host $response.StatusCode
  }
}

Export-ModuleMember *-*

