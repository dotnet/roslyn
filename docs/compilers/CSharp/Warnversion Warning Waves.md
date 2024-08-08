# /warn warning "waves"

The C# compiler flag `/warn` controls optional warnings.
When we introduce new warnings that can be reported on existing code,
we do so under an opt-in system so that programmers do not see new warnings
without taking action to enable them.
For that purpose, we have the compiler flag "`/warn:n`"
where `n` is a whole number.

The default warning level when the command-line compiler is used is `4`. If you want the compiler to produce all applicable warnings, you can specify `/warn:9999`.

In a typical project, this setting is controlled by the `AnalysisLevel` property,
which determines the `WarningLevel` property (passed to the `Csc` task).
For more information on `AnalysisLevel`, see https://devblogs.microsoft.com/dotnet/automatically-find-latent-bugs-in-your-code-with-net-5/

## Warning level 8

The compiler shipped with .NET 8 (the C# 12 compiler) contains the following warnings which are reported only under `/warn:8` or higher.

| Warning ID | Description |
|------------|-------------|
| CS9123 | [Taking address of local or parameter in async method can create a GC hole](https://github.com/dotnet/roslyn/issues/63100) |
| EnableGenerateDocumentationFile | [Helper diagnostic for enforcing IDE0005 on build](https://github.com/dotnet/roslyn/issues/70460) |

## Warning level 7

The compiler shipped with .NET 7 (the C# 11 compiler) contains the following warnings which are reported only under `/warn:7` or higher.

| Warning ID | Description |
|------------|-------------|
| CS8981 | [Type names only containing lower-cased ascii characters may become reserved for the language](https://github.com/dotnet/roslyn/issues/56653) |

## Warning level 6

The compiler shipped with .NET 6 (the C# 10 compiler) contains the following warnings which are reported only under `/warn:6` or higher.

| Warning ID | Description |
|------------|-------------|
| CS8826 | [Partial method declarations have signature differences](https://github.com/dotnet/roslyn/issues/47838) |

## Warning level 5

The compiler shipped with .NET 5 (the C# 9 compiler) contains the following warnings which are reported only under `/warn:5` or higher.

| Warning ID | Description |
|------------|-------------|
| CS7023 | [A static type is used in an 'is' or 'as' expression](https://github.com/dotnet/roslyn/issues/30198) |
| CS8073 | [Expression always true (or false) when comparing a struct to null](https://github.com/dotnet/roslyn/issues/45744) |
| CS8848 | [Diagnose precedence error with query expression](https://github.com/dotnet/roslyn/issues/30231) |
| CS8880 | [Struct constructor does not assign auto property (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8881 | [Struct constructor does not assign field (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8882 | [Out parameter not assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8883 | [Auto-property used before assigned in struct constructor (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8884 | [Field used before assigned in struct constructor (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8885 | [Struct constructor reads 'this' before assigning all fields (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8886 | [Out parameter used before being assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8887 | [Local variable used before being assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8892 | [Multiple entry points](https://github.com/dotnet/roslyn/issues/46831) |
| CS8897 | [Static class used as the parameter type of a method in an interface type](https://github.com/dotnet/roslyn/issues/38256) |
| CS8898 | [Static class used as the return type of a method in an interface type](https://github.com/dotnet/roslyn/issues/38256) |
