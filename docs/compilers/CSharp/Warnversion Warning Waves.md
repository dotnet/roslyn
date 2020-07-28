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
