# Updating NuGet ZIP file

The CI restore works by downloading the contents of the packages directory from Azure directly.  Hence if a package is updated  this zip will need to be rebuilt.  

This is done by executing the following on a Windows box.  

<!-- XXX: Does the following mean something?  If so, explain it better:
- Change to the root of the enlistment.
-->

- Delete or rename `~\.nuget`
- Run `Restore.cmd`
- Create the archive: either
  zip the `~\.nuget` directory using explorer
  or use some other archiver to make a boring-old ZIP file where the paths all start with `.nuget`.
  <small>(Nitpickers corner: yes, and the appropriate directory separator.)</small>.
  Name it <code>nuget.*N*.zip</code> (where *`N`* is one higher than the previous number).
- Use [azcopy](https://azure.microsoft.com/en-us/documentation/articles/storage-use-azcopy) to upload to https://dotnetci.blob.core.windows.net/roslyn
- Change `cibuild.sh` and `cibuild.cmd` to reference the new package. 
