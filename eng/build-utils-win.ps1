
# Collection of powershell build utility functions that we use across our scripts.

Set-StrictMode -version 2.0
$ErrorActionPreference="Stop"

Add-Type -AssemblyName 'System.Drawing'
Add-Type -AssemblyName 'System.Windows.Forms'
function Capture-Screenshot($path) {
  $width = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width
  $height = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Height

  $bitmap = New-Object System.Drawing.Bitmap $width, $height
  try {
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
      $graphics.CopyFromScreen( `
        [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.X, `
        [System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Y, `
        0, `
        0, `
        $bitmap.Size, `
        [System.Drawing.CopyPixelOperation]::SourceCopy)
    } finally {
      $graphics.Dispose()
    }

    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  } finally {
    $bitmap.Dispose()
  }
}
