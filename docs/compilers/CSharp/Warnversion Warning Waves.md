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
| CS8073 | 5 | [Expression always true (or false) when comparing a struct to null](https://github.com/dotnet/roslyn/issues/45744) |
| CS8822 | 5 | [Struct constructor does not assign auto property (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8823 | 5 | [Struct constructor does not assign field (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8824 | 5 | [Out parameter not assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8825 | 5 | [Auto-property used before assigned in struct constructor (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8826 | 5 | [Field used before assigned in struct constructor (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8827 | 5 | [Struct constructor reads 'this' before assigning all fields (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8828 | 5 | [Out parameter used before being assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
| CS8829 | 5 | [Local variable used before being assigned (imported struct type with private fields)](https://github.com/dotnet/roslyn/issues/30194) |
