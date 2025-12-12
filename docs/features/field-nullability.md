# `field` keyword nullable analysis implementation

See also [specification](https://github.com/dotnet/csharplang/blob/94205582d0f5c73e5765cb5888311c2f14890b95/proposals/field-keyword.md#nullability).

## Symbol API behavior

In order to decide the NullableAnnotation of a SynthesizedBackingFieldSymbol we need to:
1. Decide if the getter associated with this field requires a null-resilience analysis.
2. If it *doesn't* require such an analysis, then the nullable annotation of the property is used.
3. If it *does*, then a binding+nullable analysis of the getter must be performed to decide the nullability of the backing field.

There are some significant problems with directly exposing the nullable annotation on this field symbol.
1. Cost. It's not clear if it's OK for reading the NullableAnnotation off a field to cause us to bind and nullable analyze something. If some tooling is traversing member symbols for indexing or some such, it may be a problem if that causes unexpected method binding to occur where it didn't before.
2. Stack cycles. In the process of binding+flow analyzing, we may want to access the nullable annotation of the field. If we are *already* in the process of determining that field's nullable annotation, then we would need to "short-circuit" and return some "placeholder" value. We would need to take care to isolate the "internal" implementation from any public implementation, in order to ensure that a consistent answer is given to users of the public API.
...

It's tempting to avoid exposing the inferred nullable annotation in the public symbol model. This might be problematic for automated tooling which is trying to reason about nullable initialization. For example, if a diagnostic suppressor wants to suppress `CS8618` nullable initialization warnings under certain conditions. How should it decide whether/why a property like `string Prop => field ??= GetValue();` requires initialization in constructors? It's unclear to me whether this matters.

In the interests of *simplicity* (first make it correct, then make it fast), I'd like to move forward by *not* exposing the inferred annotation in the symbol model. Rather, we introduce a new internal API `SynthesizedBackingFieldSymbol.GetInferredNullableAnnotation()`, which `NullableWalker` will use to decide initial nullable state, report warnings, and so on. The implementation simply does an on-demand binding of the get accessor, then nullable analyzes and decides the nullable annotation. Care needs to be taken to avoid using this API in a re-entrant manner--so, the `NullableWalker` passes which are used to infer the nullability annotation must avoid calling it.

This means that the ordinary `FieldSymbol.TypeWithAnnotations` API would not expose the inferred nullability, and neither would `IFieldSymbol.Type` or `IFieldSymbol.NullableAnnotation`. Instead the field's nullability would always match the property's. This is something I would actually like to fix, maybe before merging to main. It feels like Quick Info, etc., should expose the inferred nullability through the ordinary APIs.

Once we get a handle on the behaviors of the getter null resilience, we can start talking about how to reduce cost associated with it. For example, we could try to reduce redundant binding, by forcing nullable annotations to be inferred prior to analyzing certain methods. Before processing a constructor in `MethodCompiler`, for example, maybe we would identify the getters that need to be compiled for purposes of inference, and force those to compile first.