# Examples

## Merge from `https://github.com/dotnet/roslyn`:`dev15-rc2` to `master` where the credentials used belong to a fictional user `merge-bot`.

``` cmd
GitMergeBot.exe --repopath=C:\path\to\roslyn\repo --sourcetype=GitHub --sourcereponame=roslyn --sourceuser=merge-bot --sourcepassword=super-secret-key --sourceremote=origin --sourcebranch=dev15-rc2 --pushtodestination- --prbranchsourceremote=upstream --destinationrepoowner=dotnet --destinationremote=upstream --destinationbranch=master
```

## Merge from `https://github.com/Microsoft/visualfsharp`:`master` to `https://<internal-f#-repository>`:`microbuild` on a VSO instance where the credentials belong to a fictional user `merge-bot`.

``` cmd
GitMergeBot.exe --repopath=C:\path\to\fsharp\repo --sourcetype=GitHub --sourcereponame=visualfsharp --sourceuser=merge-bot --sourcepassword=super-secret-key --sourceremote=origin --sourcebranch=master --pushtodestination+ --prbranchsourceremote=upstream --destinationtype=VisualStudioOnline --destinationreponame=FSharp --destinationproject=DevDiv --destinationuserid=[GUID] --destinationuser= --destinationpassword=super-secret-key --destinationremote=vso --destinationbranch=microbuild
```
