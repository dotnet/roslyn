REM Sort the PublicAPI.Unshipped.txt files

powershell -Command "Get-ChildItem -Path src\ -Recurse -Filter PublicAPI.Unshipped.txt | ForEach { Get-Content $($_.FullName) | Sort-Object | Set-Content $($_.FullName) }"
