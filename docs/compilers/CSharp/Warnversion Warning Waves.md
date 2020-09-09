# /warn warning "waves"

The C# compiler flag `/warn` controls optional warnings.
When we introduce new warnings that can be reported on existing code,
we do so under an opt-in system so that programmers do not see new warnings
without taking action to enable them.
For that purpose, we have the compiler flag "`/warn:n`"
where `n` is a whole number.

The compiler shipped with dotnet 5 (the C# 9 compiler) contains some warnings, documented below, that
are reported only under `/warn:5` or higher.

The default warning level when the command-line compiler is used is `4`.

If you want the compiler to produce all applicable warnings, you can specify
`/warn:9999`.

The table below describes all of the warnings controlled by warning levels `5` or greater.

| Warning ID | warning level | Description |
|------------|---------|-------------|
| CS7023 | 5 | [A static type is used in an 'is' or 'as' expression](https://github.com/dotnet/roslyn/issues/30198) |
| CS8073 | 5 | [Expression always true (or false) when comparing a struct to null](https://github.com/dotnet/roslyn/issues/45744) |
| CS8848 | 5 | [Diagnose precedence error with query expression](https://github.com/dotnet/roslyn/issues/30231) |
| CS8880 | 5 | [Struct constructor does not assign auto property (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8881 | 5 | [Struct constructor does not assign field (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8882 | 5 | [Out parameter not assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8883 | 5 | [Auto-property used before assigned in struct constructor (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8884 | 5 | [Field used before assigned in struct constructor (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8885 | 5 | [Struct constructor reads 'this' before assigning all fields (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8886 | 5 | [Out parameter used before being assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8887 | 5 | [Local variable used before being assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8892 | 5 | [Multiple entry points](https://github.com/dotnet/roslyn/issues/46831) |
| CS8897 | 5 | [Static class used as the parameter type of a method in an interface type](https://github.com/dotnet/roslyn/issues/38256) |
| CS8898 | 5 | [Static class used as the return type of a method in an interface type](https://github.com/dotnet/roslyn/issues/38256) |
