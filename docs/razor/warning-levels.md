# Razor warning levels

Razor diagnostics introduced in warning waves are assigned a non-zero warning level. A diagnostic
is reported when its level is less than or equal to the configured `RazorWarningLevel`. See the
[warning levels proposal](WarningWavesProposal.md) for the design and configuration details.

## Warnings

| Diagnostic | Warning level | Message | When reported |
|------------|---------------|---------|---------------|
| `RZ3907` | 11 | `The '@model' directive is not applied to the generated base class because the '@inherits' directive does not contain '<TModel>'.` | An MVC view has an explicit `@model` directive and an `@inherits` directive without the literal `<TModel>` placeholder. |
| `RZ10025` | 11 | `The component '{0}' does not have a parameter named '{1}'.` | An explicit attribute on a resolved component does not bind to a known component parameter, and no valid capture-unmatched-values parameter can accept it. |
| `RZ10026` | 11 | `The bind attribute '{0}' does not match any parameter on component '{1}'.` | A component `@bind-*` attribute has a statically known target name that does not match a component parameter. |
| `RZ10027` | 11 | `The bind attribute '{0}' requires a matching change parameter named '{1}' on component '{2}'.` | A component bind target exists, but its statically known companion change parameter does not. |
| `RZ10028` | 11 | `The attribute '{0}' could not be bound to any directive attribute.` | A parser-recognized directive attribute in a component document reaches tag-helper resolution without any semantic bound-attribute match. |
