# Breaking changes in Roslyn after .NET 11.0.100 through .NET 12.0.100

This document lists known breaking changes in Roslyn after .NET 11 general release (.NET SDK version 11.0.100) through .NET 12 general release (.NET SDK version 12.0.100).

## `safe` requires `/unsafe`

***Introduced in Visual Studio 2026 version 18.13***

Using the `safe` modifier requires `/unsafe` (`AllowUnsafeBlocks` in MSBuild), just like `unsafe`.
