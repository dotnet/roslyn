Microsoft.AspNetCore.Razor.Utilities.Shared is used across the compiiler and tooling layers and is not exposed outside of Razor.
To ensure that nothing from the assembly is shared externally, please follow these rules:

1. Only add internal APIs to this project.
2. Do not add InternalsVisibleTo for an assembly that is external to Razor compiler or tooling.
