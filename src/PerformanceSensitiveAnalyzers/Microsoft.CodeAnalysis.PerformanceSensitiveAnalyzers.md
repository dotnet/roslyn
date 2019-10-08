
Sr. No. | Rule ID | Title | Category | Enabled | CodeFix | Description |
--------|---------|-------|----------|---------|---------|--------------------------------------------------------------------------------------------------------------|
1 | HAA0101 | Array allocation for params parameter | Performance | True | False | This call site is calling into a function with a 'params' parameter. This results in an array allocation even if no parameter is passed in for the params parameter |
2 | HAA0102 | Non-overridden virtual method call on value type | Performance | True | False | Non-overridden virtual method call on a value type adds a boxing or constrained instruction |
3 | [HAA0201](http://msdn.microsoft.com/en-us/library/2839d5h5(v=vs.110).aspx) | Implicit string concatenation allocation | Performance | True | False | Considering using StringBuilder |
4 | [HAA0202](http://msdn.microsoft.com/en-us/library/yz2be5wk.aspx) | Value type to reference type conversion allocation for string concatenation | Performance | True | False | Value type ({0}) is being boxed to a reference type for a string concatenation. |
5 | HAA0301 | Closure Allocation Source | Performance | True | False | Heap allocation of closure Captures: {0} |
6 | HAA0302 | Display class allocation to capture closure | Performance | True | False | The compiler will emit a class that will hold this as a field to allow capturing of this closure |
7 | HAA0303 | Lambda or anonymous method in a generic method allocates a delegate instance | Performance | True | False | Considering moving this out of the generic method |
8 | HAA0401 | Possible allocation of reference type enumerator | Performance | True | False | Non-ValueType enumerator may result in a heap allocation |
9 | HAA0501 | Explicit new array type allocation | Performance | True | False | Explicit new array type allocation |
10 | HAA0502 | Explicit new reference type allocation | Performance | True | False | Explicit new reference type allocation |
11 | [HAA0503](http://msdn.microsoft.com/en-us/library/bb397696.aspx) | Explicit new anonymous object allocation | Performance | True | False | Explicit new anonymous object allocation |
12 | HAA0506 | Let clause induced allocation | Performance | True | False | Let clause induced allocation |
13 | HAA0601 | Value type to reference type conversion causing boxing allocation | Performance | True | False | Value type to reference type conversion causes boxing at call site (here), and unboxing at the callee-site. Consider using generics if applicable |
14 | HAA0602 | Delegate on struct instance caused a boxing allocation | Performance | True | False | Struct instance method being used for delegate creation, this will result in a boxing instruction |
15 | HAA0603 | Delegate allocation from a method group | Performance | True | False | This will allocate a delegate instance |
16 | HeapAnalyzerReadonlyMethodGroupAllocationRule | Delegate allocation from a method group | Performance | True | False | This will allocate a delegate instance |
