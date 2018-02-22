
Here is the rough testplan for the stackalloc initializers feature.

Some tests may already exist or be part of other tests. 
In such case we can just check them off. 


## Evaluation semantics ##

### correctness ###
- [ ] check that partial result is not visible. - if element throws and exception caught, whole thing is not reassigned to the new value. (try spans and ordinary stackallocs)
- [ ] if we refer to the elements of outer local in the initializers we still see the old values.
- [ ] put `await` in the size or in the middle of element initializers
- [ ] initializer uses a local that is captured by a lambda expression. See that if lambda changes the value of the local, then we see the updated value when initializing

### errors ##
- [x] invalid array shapes - multidimensional, nested, size mismatches, ... 
- [x] bad conversions
- [x] bad local initialization contexts - using, fixed. 
- [ ] should it work in `for(int* x = stackalloc...)`, probably the same ways as before... ?  
- [X] missing various span parts.
- [ ] outer expression is not usable in elements (not assigned yet)
- [ ] in the array type inference case the outer expression cannot be used to determine the type - [ ] should be an error and should not be some kind of crash due to circular dependency
- [ ] make it to infer strange types. 
	- [ ] dynamic
	- [ ] ref-struct
	- [ ] void   (use a void method to initialize an element)
	- [X] lambda expression   (formally typeless)
	- [X] null
	- [ ] discard  `_` - is not even a value. 

## API ##

- [ ] GetSymbolInfo
	- [X] on the whole node (checked for ordinary int* and Span)
	- [ ] on size expression (including cases when implicit conversions happen)
	- [ ] on element expressions (including cases when implicit conversions happen)
	
- [ ] GetDeclaredSymbol
	- [ ] the expression does not declare anything, try with whole node, elements, just in case..


	
## Manual IDE testing ##
The more detailed plan is here https://github.com/dotnet/roslyn/wiki/Manual-Testing although many items may end up N/A. 
We will have to go through the list before merging.

This may not be possible to be done with just public bits - I am not sure. Will check myself


Preliminary - the scenarios of concern are not many, since this is an expression and fairly simple too.

- [ ] extract whole thing to a method (regardless whether that works for stackallocks, it should behave rationally, even if resulting code has errors due to escape rules, etc...)
- [ ] extract array size and elements to a method, especially if there are implicit conversions. (should just work if semantic APIs work correctly)
- [ ] general typing of the construct 
	- [ ] IDE should not autocorrect into something meaningless
	- [ ] inside the initializer the dropdowns shoudl be as expected - i.e. variables in scope
	- [ ] autocompletion of parens and braces - [] { }  
 


