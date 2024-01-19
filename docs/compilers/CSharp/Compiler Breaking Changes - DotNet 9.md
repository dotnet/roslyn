# This document lists known breaking changes in Roslyn after .NET 8 all the way to .NET 9.

## .editorconfig values no longer support trailing comments

***Introduced in Visual Studio 2022 version 17.[TBD]***

The compiler is updated based on the EditorConfig specification clarification in
[editorconfig/specification#31](https://github.com/editorconfig/specification/pull/31). Following this change, comments
in **.editorconfig** and **.globalconfig** files must now appear on their own line. Comments which appear at the end of
a property value are now treated as part of the property value itself. This changes the way values are passed to
analyzers for lines with the following form:

```ini
[*.cs]
key = value # text
key2 = value2 ; text2
```

The following table shows how this change affects values passed to analyzers:

| EditorConfig line       | New compiler interpretation | Old interpretation |
| ----------------------- | --------------------------- | ------------------ |
| `key = value # text`    | `value # text`              | `value`            |
| `key2 = value2 ; text2` | `value2 ; text2`            | `value2`           |
