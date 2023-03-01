# Handling breaking changes in CLaSP

## How Versions will be handled

We'll try to stick to SemVer, so breaking changes should result in an increase of the Major version. When there is an increase of the major version we will include both the previous and new version of the dll in the CLaSP VSIX for a time, during which partner teams will be expected to upgrade (to make sure we're only shipping usages of one of the dll's in VS). This might look like:

```text
\CLaSP.vsix
  \5.x.x
    \Microsoft.CommonLanguageServerProtocol.Framework.dll (version 5.something)
  \6.x.x
    \Microsoft.CommonLanguageServerProtocol.Framework.dll (version 6.something)
```

During this time we'll have Binding redirects for any 5.x.x and below to the shipped 5.x.x version, and 6.0.0 and higher will redirect to 6.x.x.

Once we have migrated all partners to the new version (or after warning and some time has passed) we will remove the old version from the VSIX (and it's accompanying redirect) leaving the shape as something like

```text
\CLaSP.vsix
  \6.x.x
    \Microsoft.CommonLanguageServerProtocol.Framework.dll (version 6.something)
```

This is the model used by the `Microsoft.VisualStudio.LanguageServer.Protocol` package and they say they've had great success with this method.

## Which changes will result in what update types

When in doubt refer back to <https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/breaking-change-rules.md>.

|Change Type|Agreed Version update type|
|---|---|
|Addition of Class/Interface/etc|Minor|
|Removal of Class/interface/etc|Major|
|Addition/Removal of Member to Interface/Abstract|Major|
|New dependency if exposed|Major|
|New dependency if hidden|Minor?|
|Dependency version change if exposed|Major|
|Dependency version if upgrade is major|Major|
|Dep... not exposed|Minor|
|Change of Method signature|Major|
|Implementation changed|Patch (unless it has big effect on output)|

Keep a log of breaking changes (a PR label may be ideal here).
