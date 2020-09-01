## Features Opening New Windows

1. Locate a use of a generated symbol in the project, and invoke Go to Definition.
   - [ ] It works.
2. Locate a use of a generated symbol in the project, and invoke Go to Implementation.
   - [ ] It works.
3. Locate a use of a generated symbol in the project, and invoke Find References.
   - [ ] References to the generated symbol in regular code are located.
   - [ ] References to the generated symbol in generated code are located.

## Features That Are Blocked

1. Locate a use of a generated method in a project, and invoke Rename with Ctrl+R Ctrl+R.
   - [ ] It should be blocked.
2. Locate a use of a generated method in a project, change the name at the use site.
   - [ ] **[NOT WORKING YET]** Rename tracking should not trigger.

## Navigating within a Generated Document

1. Invoke Go to Definition on a generated symbol to open a source generated file.
   - [ ] **[NOT WORKING YET]** The navigation bar on the top of the file shows the project that contains the generated source
2. Invoke Find References on the symbol we navigated to.
   - [ ] **[NOT WORKING YET]** The original reference should appear in find references.
3. Click on some symbol used more than once in a generated file.
   - [ ] Highlight references should highlight all uses in the file.

## Window > New Window Support

1. Invoke Go to Definition to open a source-generated file. Then do Window > New Window to ensure that the new window is set up correctly.
   - [ ] It's read only.
   - [ ] The title and caption are correct.
2. Remove the source generator from the project.
   - [ ] Confirm the first window correctly gets an info bar showing the file is no longer valid.
   - [ ] Confirm the second window correctly gets an info bar showing the file is no longer valid.
