# Check PDBs #	
Check that PDBs support the new syntax. Use PDBTests.cs. Check that new variable declarations (including implicit) are properly declared in PDB:
1. Create code samples with new syntax.
2. Verify PDBs:
    1. Check that new explicit local variables declared within PDB \<scope\> element.
    2. Check that new explicit and implicit variable declarations are properly defined within the \<encLocalSlotMap\> element with correct kinds and offsets.

# Classify new Syntax Kinds #
Classify new Syntax Kinds. New language feature usually introduce new elements in SyntaxKind.cs aka syntax kinds.

1. Check that text spans are properly defined for new syntax kinds. Check `GetDiagnosticSpanImpl` in CSharpEditAndContinueAnalyzer.cs and add processing of new syntax kinds properly.
2. Add and classify labeling for new syntax kinds. Use StatementSyntaxComparer.cs.
    1. Add new labels to the `Label` enum (not necessary for all syntax kinds).
    2. Arrange (i.e. sort) new labels within the enum.
    3. If a label is a child label within a hierarchical construction, add a comment: "tied to parent".
    4. Add support of "tied to parent" labels within the `TiedToAncestor` method.
    5. Use the Classify method to add a mapping between syntax kinds and labels (either new or existing). Some syntax kinds could be mapped to existing labels (many-to-one relationship).
    6. Consider how to map two generations of new syntax nodes in case of editing. Maybe it requires defining a distance function between two generations. See the `TryComputeWeightedDistance` method, and if necessary add methods for calculating distances between new nodes.
3. Verify the above with RudeEditStatementTests.cs or RudeEditTopLevelTests.cs. There are two kinds of tests:
    1. Matching tests check matching pairs in before and after codes.
    2. `VerifyEdits` tests (use `GetTopEdits` for class edits or `GetMethodEdits` for method edits) check that code transformations (update, add, delete, reorder) are properly determined.
	
# Check IL #
Check that IL is properly generated for new syntax. Use EditAndContinueTests.cs and LocalSLotMappingTests.cs.
1. Check that slots for variables in new syntax are properly defined using LocalSLotMappingTests.cs.
2. Check that new syntax is covered by editing or even double editing. See that slots for variables are properly either re-used or new ones are introduced.

# Verify Symbol Matcher #
Check that symbols are either re-used or not re-used after edit. 
1. Use SymbolMatcherTests.cs
2. Update CSharpSymbolMatcher.cs if necessary. 

# Enable new features in Edit and Continue #
1. See features excluded from edit and continue within CSharpEditAndContinueAnalyzer.cs and enable necessary ones.
2. You probably get some ActiveStatementTests.cs tests failed with the change above. Correct them properly.
3. Verify edit and continue features for new syntax manually:
    1. Set VisualStudioSetup project as a startup project within Roslyn.sln. 
    2. Run the project with F5. 
    3. A new instance of Visual Studio appears.
    4. The instance uses changes made in the local version of Roslyn.
    5. Play with edit and continue scenarios around new syntax:
        1. Set breakpoints near new syntax
        2. Try adding/deleting/updating/reordering
        3. Change variables names and types
        4. See that code if properly executed
        5. See that watches are properly updated with edit and continue changes in the watch window
# Create Integration tests #
Provide integration tests.
		
		
		
			
		
