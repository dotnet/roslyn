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

## Which changes will result in what update types

|Change Type|Ryan's guess of Version update type|Agreed Version update type|
|---|---|---|
|Addition to Interface|Major|?|
|Addition of Class/Interface|Minor|?|
|New dependency|Major|?|
|Change of signature|Major|?|
|Changing implementation|Patch (unless it has big effect on output)|?|
