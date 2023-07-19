# External Access Policies

## API Tracking

We track the APIs that we've exposed with a roslyn analyzer, which will fail the build if APIs are removed. Unlike our public APIs, we are allowed to have breaking changes here, but these will often cause insertion failures. Any removal of an existing API (whether the API is fully removed, or the parameters/return changed) needs to have sign off from the current infrastructure rotation, and will potentially require a test insertion before merging to ensure VS isn't broken, or coordinated insertion with the affected partner team.

Because "shipping" for EA projects occurs every time a change is checked in, we don't bother updating the `InternalAPI.Shipped.txt` file for these projects.

Every EA project has an `Internal` namespace that is considered Roslyn implementation details. For example, the OmniSharp namespace is `Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Internal`. We do not track the APIs under these namespaces, and it's on the dependent projects to not reference anything from these namespaces. Not every EA project actually uses this namespace. If an EA project that doesn't currently use their `Internal` namespace wants to start, modify `/src/Tools/ExternalAccess/.editorconfig` to set the `dotnet_public_api_analyzer.skip_namespaces` key to that namespace for the affected files. Multiple namespaces can be included by using a `,` separator.

## OmniSharp

When a change needs to be made to an API in the ExternalAccess.OmniSharp or ExternalAccess.OmniSharp.CSharp packages, ping @333fred, @JoeRobich, @filipw, or @david-driscoll as a heads up. Breaking changes are allowed, but please wait for acknowledgement and followup questions to ensure that we don't completely break OmniSharp scenarios.
