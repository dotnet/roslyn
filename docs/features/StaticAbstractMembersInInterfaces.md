Static Abstract Members In Interfaces
=====================================

An interface is allowed to specify abstract static members that implementing classes and structs are then
required to provide an explicit or implicit implementation of. The members can be accessed off of type
parameters that are constrained by the interface.

Proposal: 
- https://github.com/dotnet/csharplang/issues/4436
- https://github.com/dotnet/csharplang/blob/main/proposals/static-abstracts-in-interfaces.md

Feature branch: https://github.com/dotnet/roslyn/tree/features/StaticAbstractMembersInInterfaces

Test plan: https://github.com/dotnet/roslyn/issues/52221