## RoslynEx

RoslynEx is a fork of [Roslyn](https://github.com/dotnet/roslyn) (the C# compiler) which allows you to execute "source transformers". Source transformers are similar to [source generators](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/), except that they allow any changes to the source code, not just additions.

[![CI badge](https://github.com/postsharp/RoslynEx/workflows/Full%20Pipeline/badge.svg)](https://github.com/postsharp/RoslynEx/actions?query=workflow%3A%22Full+Pipeline%22)

Existing code transformers that use RoslynEx include:

* [RoslynEx.Virtuosity](https://github.com/postsharp/RoslynEx.Virtuosity): makes all possible methods in a project `virtual`.
* [RoslynEx.Cancellation](https://github.com/postsharp/RoslynEx.Cancellation): automatically propagtes `CancellationToken` parameter
* [RoslynEx.Costura](https://github.com/postsharp/RoslynEx.Costura): bundles .NET Framework applications into a single executable file

See the above projects to learn how to use RoslynEx to write your own source transformers.