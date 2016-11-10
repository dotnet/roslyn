# Example

Example merge from `dotnet:dev15-rc2` to `dotnet:master` where the credentials used belong to a fictional user `merge-bot`.

``` cmd
set username=merge-bot
set password=[password-or-auth-token]
set sourcebranch=dev15-rc2
set destbranch=master
GitMergeBot.exe --repopath=C:\path\to\repo --sourcetype=GitHub --sourcereponame=roslyn --sourceuser=%username% --sourcepassword=%password% --sourceremote=origin --sourcebranch=%sourcebranch% --pushtodestination- --prbranchsourceremote=upstream --destinationrepoowner=dotnet --destinationremote=upstream --destinationbranch=%destbranch%
```
