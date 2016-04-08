# Language Feature Status

This document reflects the status, and planned work, for the compiler team.  It is a live document and will be updated as work progresses, features are added / removed, and as work on feature progresses.  

## C# 7.0 and VB 15

| Feature | Branch | State | Owners | LDM Champ |
| ------- | ------ | ----- | ------ | ----- | ----- |
| [Async Main](https://github.com/dotnet/roslyn/issues/7476) | none  | Feature Specification | [tyoverby](https://github.com/tyoverby), [agocke](https://github.com/agocke) | [stephentoub](https://github.com/stephentoub) |
| Address of Static | none | Feature Specification | | [jaredpar](https://github.com/jaredpar) |
| [Binary Literals](https://github.com/dotnet/roslyn/issues/215) | [future](https://github.com/dotnet/roslyn/tree/future)  | Finishing | | [gafter](https://github.com/gafter) |
| [Digit Separators](https://github.com/dotnet/roslyn/issues/216) | [future](https://github.com/dotnet/roslyn/tree/future)  | Finishing | | [gafter](https://github.com/gafter) |
| [Local Functions](https://github.com/dotnet/roslyn/blob/future/docs/features/local-functions.md) | [future](https://github.com/dotnet/roslyn/tree/future)  | Finishing | [agocke](https://github.com/agocke), [jaredpar](https://github.com/jaredpar), [vsadov](https://github.com/vsadov) | [gafter](https://github.com/gafter) |
| [Pattern Matching](https://github.com/dotnet/roslyn/blob/future/docs/features/patterns.md) | [features/patterns](https://github.com/dotnet/roslyn/tree/features/patterns) | Prototyping | [gafter](https://github.com/gafter), [alekseyts](https://github.com/alekseyts), [agocke](https://github.com/agocke) | [gafter](https://github.com/gafter) |
| [Ref Returns](https://github.com/dotnet/roslyn/issues/118) | [future](https://github.com/dotnet/roslyn/tree/future) | Finishing | [vsadov](https://github.com/vsadov), [agocke](https://github.com/agocke), [jaredpar](https://github.com/jaredpar) | [vsadov](https://github.com/vsadov) |
| Source Generation | [future](https://github.com/dotnet/roslyn/tree/features/generators) | Prototyping | [cston](https://github.com/cston), [vsadov](https://github.com/vsadov) | [mattwar](https://github.com/mattwar) |
| [Throw Expr](https://github.com/dotnet/roslyn/blob/future/docs/features/patterns.md) | [features/patterns](https://github.com/dotnet/roslyn/tree/features/patterns) | Prototyping | [agocke](https://github.com/agocke), [tyoverby](https://github.com/tyoverby), [gafter](https://github.com/gafter) | [gafter](https://github.com/gafter) |
| [Tuples](https://github.com/dotnet/roslyn/issues/347) | [features/tuples](https://github.com/dotnet/roslyn/tree/features/tuples) | Prototyping | [vsadov](https://github.com/vsadov), [jcouv](https://github.com/jcouv) | [madstorgerson](https://github.com/MadsTorgersen) |
| [Out var](https://github.com/dotnet/roslyn/issues/6183) | none | Feature Specification | [alekseyts](https://github.com/alekseyts) | [gafter](https://github.com/gafter) |

## (C# 7.0 and VB 15) + 1

| Feature | Branch | State | Owners | LDM |
| ------- | ------ | ----- | ------ | ----- |
| [private protected](https://github.com/dotnet/roslyn/blob/features/privateProtected/docs/features/private-protected.md) | [features/privateProtected](https://github.com/dotnet/roslyn/tree/features/privateProtected) | Prototyping | | [gafter](https://github.com/gafter) |
| [Non-null Ref Types](https://github.com/dotnet/roslyn/blob/features/NullableReferenceTypes/docs/features/NullableReferenceTypes/Nullable%20reference%20types.md) | [features/NullableReferenceTypes](https://github.com/dotnet/roslyn/tree/features/NullableReferenceTypes) | Prototyping | [alekseyts](https://github.com/alekseyts) | [mattwar](https://github.com/mattwar) |
| [Better Betterness](https://github.com/dotnet/roslyn/issues/250) | none | Feature Specification | | [gafter](https://github.com/gafter) |
| [Records](https://github.com/dotnet/roslyn/blob/features/records/docs/features/records.md) | [features/records](https://github.com/dotnet/roslyn/tree/features/records) | Feature Specification | [jcouv](https://github.com/jcouv) | [gafter](https://github.com/gafter) |
| [With Exprs](https://github.com/dotnet/roslyn/blob/features/records/docs/features/records.md) | [features/records](https://github.com/dotnet/roslyn/tree/features/records) | Feature Specification | [gafter](https://github.com/gafter) | [gafter](https://github.com/gafter) |

# FAQ

- **Is target version a guarantee?**: No.  It's explicitly not a guarantee.  This is just the planned and on going work to the best of our knowledge at this time.
- **Where are these State values defined?**: Take a look at the [Developing a Language Feature](contributing/Developing a Language Feature.md) document.
