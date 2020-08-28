. (Join-Path $PSScriptRoot "build-utils.ps1")

ShouldRunCI -AsOutput
if ($_ShouldRunCI) {
  exit 0;
}
else {
  exit 1;
}
