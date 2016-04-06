# Language Feature Status

This document reflects the status, and planned work, for the compiler team.  It is a live document and will be updated as work progresses, features are added / removed, and as work on feature progresses.  

## C# 7.0 and VB 15

| Feature | Branch | State | Owners | LDM Champ |
| ------- | ------ | ----- | ------ | ----- | ----- |
| Async Main | none  | Feature Specification | [tyoverby](https://github.com/tyoverby), [agocke](https://github.com/agocke) | [stephentoub](https://github.com/stephentoub) |
| Address of Static | none | Feature Specification | | [jaredpar](https://github.com/jaredpar) |
| Binary Literals | [future](https://github.com/dotnet/roslyn/tree/future)  | Finishing | | [gafter](https://github.com/gafter) |
| Digit Separators | [future](https://github.com/dotnet/roslyn/tree/future)  | Finishing | | [gafter](https://github.com/gafter) |
| [Local Functions](https://github.com/dotnet/roslyn/blob/future/docs/features/local-functions.md) | [future](https://github.com/dotnet/roslyn/tree/future)  | Finishing | [agocke](https://github.com/agocke), [jaredpar](https://github.com/jaredpar), [vsadov](https://github.com/vsadov) | [gafter](https://github.com/gafter) |
| [Pattern Matching](https://github.com/dotnet/roslyn/blob/future/docs/features/patterns.md) | [features/patterns](https://github.com/dotnet/roslyn/tree/features/patterns) | Prototyping | [gafter](https://github.com/gafter), [alekseyts](https://github.com/alekseyts), [agocke](https://github.com/agocke) | [gafter](https://github.com/gafter) |
| Records | none | Feature Specification | [jcouv](https://github.com/jcouv) | [gafter](https://github.com/gafter) |
| Ref Returns | [future](https://github.com/dotnet/roslyn/tree/future) | Finishing | [vsadov](https://github.com/vsadov), [agocke](https://github.com/agocke), [jaredpar](https://github.com/jaredpar) | [vsadov](https://github.com/vsadov) |
| Source Generation | [future](https://github.com/dotnet/roslyn/tree/features/generators) | Prototyping | [cston](https://github.com/cston), [vsadov](https://github.com/vsadov) | [mattwar](https://github.com/mattwar) |
| Throw Expr | [features/patterns](https://github.com/dotnet/roslyn/tree/features/patterns) | Prototyping | [agocke](https://github.com/agocke), [tyoverby](https://github.com/tyoverby), [gafter](https://github.com/gafter) | [gafter](https://github.com/gafter) |
| Tuples | [features/tuples](https://github.com/dotnet/roslyn/tree/features/tuples) | Prototyping | [vsadov](https://github.com/vsadov), [jcouv](https://github.com/jcouv) | [madstorgerson](https://github.com/MadsTorgersen) |
| Out var | none | Feature Specification | [alekseyts](https://github.com/alekseyts) | [gafter](https://github.com/gafter) |
| With Exprs | none | Feature Specification | [gafter](https://github.com/gafter) | [gafter](https://github.com/gafter) |

## (C# 7.0 and VB 15) + 1

| Feature | Branch | State | Owners | LDM |
| ------- | ------ | ----- | ------ | ----- |
| Protected Private | [features/privateprotected](https://github.com/dotnet/roslyn/tree/features/privateprotected) | Prototyping | | [gafter](https://github.com/gafter) |
| [Non-null Ref Types](https://github.com/dotnet/roslyn/blob/features/NullableReferenceTypes/docs/features/NullableReferenceTypes/Nullable%20reference%20types.md) | [features/NullableReferenceTypes](https://github.com/dotnet/roslyn/tree/features/NullableReferenceTypes) | Prototyping | [alekseyts](https://github.com/alekseyts) | [mattwar](https://github.com/mattwar) |
| Better Betterness | | Feature Specification | | [gafter](https://github.com/gafter) |

# FAQ

- **Is target version a guarantee?**: No.  It's explicitly not a guarantee.  This is just the planned and on going work to the best of our knowledge at this time.
- **Where are these State values defined?**: Take a look at the [Developing a Language Feature](contributing/Developing a Language Feature.md) document.
