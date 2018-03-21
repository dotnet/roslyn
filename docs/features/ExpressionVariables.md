Expression Variables
=========================

The *expression variables* feature extends the features introduced in C# 7 to permit expressions 
containing expression variables (out variable declarations and declaration patterns) in field 
initializers, property initializers, ctor-initializers, and query clauses.

See https://github.com/dotnet/csharplang/issues/32 and 
https://github.com/dotnet/csharplang/blob/master/proposals/expression-variables-in-initializers.md 
for more information.

Current state of the feature:

[X] Permit in field initializers
[X] Permit in property initializers
[ ] Permit in ctor-initializers
[X] Permit in query clauses