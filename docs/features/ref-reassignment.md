
Ref Local and Parameter Reassignment
====================================

In C# 7.2, `ref` locals and parameters cannot be assigned new references,
only assigned values which reassign the underlying storage to the given value.

C# 7.3 introduces a new type of expression, a ref-assignment expression.

The syntax is as follows

```
ref_assignment
    : identifier '=' 'ref' expression
```

The identifier on the left-hand side of a *ref_assignment* must be a `ref` or
`ref readonly` local variable, or an `in`, `ref`, or `out` parameter. Ref
assignments allow ref local variables or parameters, which represent
references to storage locations, to be assigned different references. This does
not include the `this` reference, even in structs.

The expression on the right-hand side must have a storage location, i.e. it must
be an "l-value." This l-value must be "ref-compatible" with the variable being
ref-assigned, where ref-compatible means:

* The type of left-hand side and the type of the right-hand side must have an 
  identity conversion between them.
* Writeable ref variables cannot be assigned read-only l-values. Readonly ref
  variables can be assigned either read-only or writeable l-values.
  
The result of the ref-assignment expression is itself an "l-value" and the
resulting storage location is read-only if and only if the left-hand side of
the assignment is a read-only ref variable.

The storage location for the right-hand side must be definitely assigned
before it can be used in a ref-assignment expression. In addition, the
storage location referenced by an `out` parameter must be definitely assigned
before the `out` parameter is ref-reassigned to a new reference.

The lifetime for a `ref` local or parameter is fixed on declaration and
cannot be reassigned with ref-reassignment. The lifetime requirements for the
right-hand expression are the same as for the right-hand expression in a
variable assignment, identical to the lifetime for Span&lt;T> assignments.
