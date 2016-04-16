Conflict Between Name From Metadata And Name In Source
======================================================

When the compiler tries to bind a type name, and the same fully-qualified name designates both a name in the current (source) assembly and also a name imported from metadata, the compiler emits a warning (see ErrorCode.WRM_SameFullName*) and resolves to the one in source.
