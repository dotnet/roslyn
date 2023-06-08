# This document lists known breaking changes in Roslyn after .NET 6 all the way to .NET 7.

## `RequiredMembersAttribute` cannot be manually applied

**Introduced in VS 17.6**

As part of implementing VB support for consuming `required` APIs, it is now an error to manually apply `RequiredMembersAttribute` in
source. VB will now correctly interpret these attributes in metadata and allow instantiating types with required members.
