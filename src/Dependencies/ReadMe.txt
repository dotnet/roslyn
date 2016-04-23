To prevent projects across the tree from having different dependencies, we unify 
external package dependencies by creating empty C# projects that represent them.
We then make use of project.json package inheritance (where a project inherits
all packages dependencies from their dependencies) to unify it across the tree.

No projects outside of this tree should be directly referencing versions of 
these dependencies manually.