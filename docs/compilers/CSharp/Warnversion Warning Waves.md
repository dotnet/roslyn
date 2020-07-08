# /warnversion warning "waves"

The C# compiler flag `/warnversion` controls optional warnings.
When we introduce new warnings that can be reported on existing code,
we do so under an opt-in system so that programmers do not see new warnings
without taking action to enable them.
For that purpose, we have introduced the compiler flag "`/warnversion=n`"
where `n` is a whole number or a decimal number.
For a warning that was introduced in dotnet version `k`,
that warning will be produced if the warning version `n` specified is
greater than or equal to `k` and a compiler shipped with dotnet version
`k` or later is used to compile the code.

The default warning version is `0` (produce no optional warnings).
Our first warning under control of `/warnversion` was introduced in version `5`.
If you want the compiler to produce all applicable warnings, you can specify
`/warnversion=9999`.
In the project file, the property used to specify the warning version is `AnalysisLevel`.

The table below describes all of the warnings controlled by `/warnversion`.

| Warning ID | Version | Description |
|------------|---------|-------------|
| CS8073 | 5 | [Expression always true (or false) when comparing a struct to null](https://github.com/dotnet/roslyn/issues/45744) |
