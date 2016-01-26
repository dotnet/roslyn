### High Level Language Proposal:  
Allow 'readonly' to be used on local variables and on parameters. 

'readonly' on local variables and 'parameters' will work similarly as it does for 'fields' (with important differences listed in the next paragraph).  Namely, 'readonly' local variables and parameters cannot be written to in any way (i.e. through assignment, use of ```++/+=```, passing as a 'ref/out', etc.). 

Important differences:
unlike 'readonly' fields, 'readonly' locals must have an initializer.
unlike 'readonly' fields (which can be written to multiple times in constructors), 'readonly' locals can only have the initializer and can never be written to again.
unlike 'readonly' fields, the 'readonly'ness is not visible at the IL level.  'readonly' is a source-level-only attribute which indicates how the local and parameter can be treated inside a method body.  IN this way it is similar to 'async'.  The modifier appears on the signature, but it only affects the code of the body.

Allowed locations:
1. Method parameters (non-abstract/extern)  
2. Constructor parameters  
3. Indexer parameters  
4. Operator parameters  
5. Lambda parameters  
6. Anonymous method parameters  
7. Local function parameters  


Disallowed locations:  
1. Delegate parameters  
2. Methods/indexers witout bodies (i.e. extern/abstract/interface)


Interesting questions:  
1. Allow "readonly this" (for extension methods)?  Current implementation allows it.  I don't see why not to allow it.  
2. Allow "readonly params"?  Current implementation allows it.  I don't see why not to allow it.  
3. Allow "readonly ref"?  Currently no.  My understanding is that there are design/perf concerns here.  So i'm just disallowing it for now.  
4. Allow "(readonly i) => ..." (i.e. a readonly *implicit* lambda parameter).  Current implementation allows it.  


#### Grammar changes  
To support this proposal this we first change the grammar in the following ways.  ```+``` is used to indicate a grammar additions.  ```strike-through``` is used to indicate a grammer removal:

declaration-statement:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;local-variable-declaration   ;  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;local-constant-declaration   ;  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;+ local-readonly-declaration   ;  


&nbsp;+ local-readonly-declaration:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;readonly   type   readonly-declarators


&nbsp;+ readonly-declarators:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;readonly-declarator  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;readonly-declarators   ,   readonly-declarator


&nbsp;+ readonly-declarator:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;identifier   =   constant-expression


fixed-parameter:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;attributesopt   parameter-modifieropt   type   identifier default-argumentopt    
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;attributesopt   parameter-modifieropt   readonly   type   identifier default-argumentopt  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;attributesopt   readonly   parameter-modifieropt   type   identifier default-argumentopt


#### Semantic Changes  
