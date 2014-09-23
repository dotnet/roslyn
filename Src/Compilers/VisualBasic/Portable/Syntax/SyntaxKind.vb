' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Enumeration with all Visual Basic syntax node kinds.
    ''' </summary>
    Public Enum SyntaxKind As UShort
        ' ADD NEW SYNTAX TO THE END OF THIS ENUM OR YOU WILL BREAK BINARY
        ' COMPATIBILITY
        None = 0
        List = GreenNode.ListKind
        ''' <summary>
        ''' A class to represent an empty statement. This can occur when a colon is on a
        ''' line without anything else.
        ''' </summary>
        EmptyStatement = 2                       ' EmptyStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndIfStatement = 3                       ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndUsingStatement = 4                    ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndWithStatement = 5                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSelectStatement = 6                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndStructureStatement = 7                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndEnumStatement = 8                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndInterfaceStatement = 9                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndClassStatement = 10                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndModuleStatement = 11                  ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndNamespaceStatement = 12               ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSubStatement = 13                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndFunctionStatement = 14                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndGetStatement = 15                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSetStatement = 16                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndPropertyStatement = 17                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndOperatorStatement = 18                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndEventStatement = 19                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndAddHandlerStatement = 20              ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndRemoveHandlerStatement = 21           ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndRaiseEventStatement = 22              ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndWhileStatement = 23                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndTryStatement = 24                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSyncLockStatement = 25                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an entire source file of VB code.
        ''' </summary>
        CompilationUnit = 26                     ' CompilationUnitSyntax
        ''' <summary>
        ''' Represents an Option statement, such as "Option Strict On".
        ''' </summary>
        OptionStatement = 27                     ' OptionStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Imports statement, which has one or more imports clauses.
        ''' </summary>
        ImportsStatement = 28                    ' ImportsStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the clause of an Imports statement that defines an alias for a
        ''' namespace or type.
        ''' </summary>
        AliasImportsClause = 29                  ' AliasImportsClauseSyntax : ImportsClauseSyntax
        ''' <summary>
        ''' Represents the clause of an Imports statement that imports all members of a
        ''' namespace.
        ''' </summary>
        MembersImportsClause = 30                ' MembersImportsClauseSyntax : ImportsClauseSyntax
        ''' <summary>
        ''' Defines a XML namespace for XML expressions.
        ''' </summary>
        XmlNamespaceImportsClause = 31           ' XmlNamespaceImportsClauseSyntax : ImportsClauseSyntax
        ''' <summary>
        ''' Represents a Namespace statement, its contents and the End Namespace statement.
        ''' </summary>
        NamespaceBlock = 32                      ' NamespaceBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a namespace declaration. This node always
        ''' appears as the Begin of a BlockStatement with Kind=NamespaceBlock.
        ''' </summary>
        NamespaceStatement = 33                  ' NamespaceStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of Module, its contents and the End statement that
        ''' ends it.
        ''' </summary>
        ModuleBlock = 34                         ' ModuleBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of a Structure, its contents and the End statement
        ''' that ends it.
        ''' </summary>
        StructureBlock = 35                      ' StructureBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of a Interface, its contents and the End statement
        ''' that ends it.
        ''' </summary>
        InterfaceBlock = 36                      ' InterfaceBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of a Class its contents and the End statement that
        ''' ends it.
        ''' </summary>
        ClassBlock = 37                          ' ClassBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of an Enum, its contents and the End Enum statement
        ''' that ends it.
        ''' </summary>
        EnumBlock = 38                           ' EnumBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Inherits statement in a Class, Structure or Interface.
        ''' </summary>
        InheritsStatement = 39                   ' InheritsStatementSyntax : InheritsOrImplementsStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Implements statement in a Class or Structure.
        ''' </summary>
        ImplementsStatement = 40                 ' ImplementsStatementSyntax : InheritsOrImplementsStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Module declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=ModuleDeclarationBlock.
        ''' </summary>
        ModuleStatement = 41                     ' ModuleStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Structure declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=StructureDeclarationBlock.
        ''' </summary>
        StructureStatement = 42                  ' StructureStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Interface declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=InterfaceDeclarationBlock.
        ''' </summary>
        InterfaceStatement = 43                  ' InterfaceStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Class declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=ClassDeclarationBlock.
        ''' </summary>
        ClassStatement = 44                      ' ClassStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of an Enum declaration. This node always
        ''' appears as the Begin of an EnumBlock with Kind=EnumDeclarationBlock.
        ''' </summary>
        EnumStatement = 45                       ' EnumStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the type parameter list in a declaration.
        ''' </summary>
        TypeParameterList = 46                   ' TypeParameterListSyntax
        ''' <summary>
        ''' Represents a type parameter on a generic type declaration.
        ''' </summary>
        TypeParameter = 47                       ' TypeParameterSyntax
        ''' <summary>
        ''' One of the type parameter constraints clauses. This represents a constraint
        ''' clause in the form of "As Constraint".
        ''' </summary>
        TypeParameterSingleConstraintClause = 48  ' TypeParameterSingleConstraintClauseSyntax : TypeParameterConstraintClauseSyntax
        ''' <summary>
        ''' One of the type parameter constraints clauses. This represents a constraint
        ''' clause in the form of "As { Constraints }".
        ''' </summary>
        TypeParameterMultipleConstraintClause = 49  ' TypeParameterMultipleConstraintClauseSyntax : TypeParameterConstraintClauseSyntax
        ''' <summary>
        ''' One of the special type parameter constraints: New, Class or Structure. Which
        ''' kind of special constraint it is can be obtained from the Kind property and is
        ''' one of: NewConstraint, ReferenceConstraint or ValueConstraint.
        ''' </summary>
        NewConstraint = 50                       ' SpecialConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' One of the special type parameter constraints: New, Class or Structure. Which
        ''' kind of special constraint it is can be obtained from the Kind property and is
        ''' one of: NewConstraint, ReferenceConstraint or ValueConstraint.
        ''' </summary>
        ClassConstraint = 51                     ' SpecialConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' One of the special type parameter constraints: New, Class or Structure. Which
        ''' kind of special constraint it is can be obtained from the Kind property and is
        ''' one of: NewConstraint, ReferenceConstraint or ValueConstraint.
        ''' </summary>
        StructureConstraint = 52                 ' SpecialConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' Represents a type parameter constraint that is a type.
        ''' </summary>
        TypeConstraint = 53                      ' TypeConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' Represents a name and value in an EnumDeclarationBlock.
        ''' </summary>
        EnumMemberDeclaration = 54               ' EnumMemberDeclarationSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Function or Sub block declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' </summary>
        SubBlock = 55                            ' MethodBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Function or Sub block declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' </summary>
        FunctionBlock = 56                       ' MethodBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a constructor block declaration: A declaration that has a beginning
        ''' declaration, a body of executable statements and an end statement.
        ''' </summary>
        ConstructorBlock = 57                    ' ConstructorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Operator block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' </summary>
        OperatorBlock = 58                       ' OperatorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        PropertyGetBlock = 59                    ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        PropertySetBlock = 60                    ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        AddHandlerBlock = 61                     ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        RemoveHandlerBlock = 62                  ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        RaiseEventBlock = 63                     ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a block property declaration: A declaration that has a beginning
        ''' declaration, some get or set accessor blocks and an end statement.
        ''' </summary>
        PropertyBlock = 64                       ' PropertyBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a custom event declaration: A declaration that has a beginning event
        ''' declaration, some accessor blocks and an end statement.
        ''' </summary>
        EventBlock = 65                          ' EventBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the parameter list in a method declaration.
        ''' </summary>
        ParameterList = 66                       ' ParameterListSyntax
        ''' <summary>
        ''' The statement that declares a Sub or Function. If this method has a body, this
        ''' statement will be the Begin of a BlockStatement with
        ''' Kind=MethodDeclarationBlock, and the body of the method will be the Body of
        ''' that BlockStatement.
        ''' </summary>
        SubStatement = 67                        ' MethodStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The statement that declares a Sub or Function. If this method has a body, this
        ''' statement will be the Begin of a BlockStatement with
        ''' Kind=MethodDeclarationBlock, and the body of the method will be the Body of
        ''' that BlockStatement.
        ''' </summary>
        FunctionStatement = 68                   ' MethodStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares a constructor. This statement will be the Begin of a
        ''' BlockStatement with Kind=MethodDeclarationBlock, and the body of the method
        ''' will be the Body of that BlockStatement.
        ''' </summary>
        SubNewStatement = 69                     ' SubNewStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A Declare statement that declares an external DLL method.
        ''' </summary>
        DeclareSubStatement = 70                 ' DeclareStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A Declare statement that declares an external DLL method.
        ''' </summary>
        DeclareFunctionStatement = 71            ' DeclareStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares a delegate type.
        ''' </summary>
        DelegateSubStatement = 72                ' DelegateStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares a delegate type.
        ''' </summary>
        DelegateFunctionStatement = 73           ' DelegateStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares an event. If the event being declared is a custom
        ''' event, this statement will be the Begin of a PropertyOrEventBlock, and the
        ''' accessors will be part of the Accessors of that node.
        ''' </summary>
        EventStatement = 74                      ' EventStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares an operator. If this operator has a body, this
        ''' statement will be the Begin of a BlockStatement with
        ''' Kind=MethodDeclarationBlock, and the body of the method will be the Body of
        ''' that BlockStatement.
        ''' </summary>
        OperatorStatement = 75                   ' OperatorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Statement that declares a property. If this property has accessors declared,
        ''' this statement will be the Begin of a BlockNode, and the accessors will be the
        ''' Body of that node. Auto properties are property declarations without a
        ''' PropertyBlock.
        ''' </summary>
        PropertyStatement = 76                   ' PropertyStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        GetAccessorStatement = 77                ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        SetAccessorStatement = 78                ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        AddHandlerAccessorStatement = 79         ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        RemoveHandlerAccessorStatement = 80      ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        RaiseEventAccessorStatement = 81         ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the "Implements ..." clause on a type member, which describes which
        ''' interface members this member implements.
        ''' </summary>
        ImplementsClause = 82                    ' ImplementsClauseSyntax
        ''' <summary>
        ''' Represents the "Handles ..." clause on a method declaration that describes
        ''' which events this method handles.
        ''' </summary>
        HandlesClause = 83                       ' HandlesClauseSyntax
        ''' <summary>
        ''' Represents event container specified through special keywords "Me", "MyBase" or
        ''' "MyClass"..
        ''' </summary>
        KeywordEventContainer = 84               ' KeywordEventContainerSyntax : EventContainerSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents event container that refers to a WithEvents member.
        ''' </summary>
        WithEventsEventContainer = 85            ' WithEventsEventContainerSyntax : EventContainerSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents event container that refers to a WithEvents member's property.
        ''' </summary>
        WithEventsPropertyEventContainer = 86    ' WithEventsPropertyEventContainerSyntax : EventContainerSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single handled event in a "Handles ..." clause.
        ''' </summary>
        HandlesClauseItem = 87                   ' HandlesClauseItemSyntax
        ''' <summary>
        ''' Represents the beginning of a declaration. However, not enough syntax is
        ''' detected to classify this as a field, method, property or event. This is node
        ''' always represents a syntax error.
        ''' </summary>
        IncompleteMember = 88                    ' IncompleteMemberSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the declaration of one or more variables or constants, either as
        ''' local variables or as class/structure members. In the case of a constant, it is
        ''' represented by having "Const" in the Modifiers (although technically "Const" is
        ''' not a modifier, it is represented as one in the parse trees.)
        ''' </summary>
        FieldDeclaration = 89                    ' FieldDeclarationSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the part of a variable or constant declaration statement that
        ''' associated one or more variable names with a type.
        ''' </summary>
        VariableDeclarator = 90                  ' VariableDeclaratorSyntax
        ''' <summary>
        ''' Represents an "As {type-name}" clause that does not have an initializer or
        ''' "New". The type has optional attributes associated with it, although attributes
        ''' are not permitted in all possible places where this node occurs.
        ''' </summary>
        SimpleAsClause = 91                      ' SimpleAsClauseSyntax : AsClauseSyntax
        ''' <summary>
        ''' Represents an "As New {type-name} [arguments] [initializers]" clause in a
        ''' declaration. The type has optional attributes associated with it, although
        ''' attributes are not permitted in many places where this node occurs (they are
        ''' permitted, for example, on automatically implemented properties.)
        ''' </summary>
        AsNewClause = 92                         ' AsNewClauseSyntax : AsClauseSyntax
        ''' <summary>
        ''' Represents a "With {...} clause used to initialize a new object's members.
        ''' </summary>
        ObjectMemberInitializer = 93             ' ObjectMemberInitializerSyntax : ObjectCreationInitializerSyntax
        ''' <summary>
        ''' Represents a "From {...} clause used to initialize a new collection object's
        ''' elements.
        ''' </summary>
        ObjectCollectionInitializer = 94         ' ObjectCollectionInitializerSyntax : ObjectCreationInitializerSyntax
        ''' <summary>
        ''' Represent a field initializer in a With {...} initializer where the field name
        ''' is inferred from the initializer expression.
        ''' </summary>
        InferredFieldInitializer = 95            ' InferredFieldInitializerSyntax : FieldInitializerSyntax
        ''' <summary>
        ''' Represent a named field initializer in a With {...} initializer, such as ".x =
        ''' expr".
        ''' </summary>
        NamedFieldInitializer = 96               ' NamedFieldInitializerSyntax : FieldInitializerSyntax
        ''' <summary>
        ''' Represents an "= initializer" clause in a declaration for a variable,
        ''' pararameter or automatic property.
        ''' </summary>
        EqualsValue = 97                         ' EqualsValueSyntax
        ''' <summary>
        ''' Represent a parameter to a method, property, constructor, etc.
        ''' </summary>
        Parameter = 98                           ' ParameterSyntax
        ''' <summary>
        ''' Represents an identifier with optional "?" or "()" or "(,,,)" modifiers, as
        ''' used in parameter declarations and variable declarations.
        ''' </summary>
        ModifiedIdentifier = 99                  ' ModifiedIdentifierSyntax
        ''' <summary>
        ''' Represents a modifier that describes an array type, without bounds, such as
        ''' "()" or "(,)".
        ''' </summary>
        ArrayRankSpecifier = 100                 ' ArrayRankSpecifierSyntax
        ''' <summary>
        ''' Represents a group of attributes within "&lt;" and "&gt;" brackets.
        ''' </summary>
        AttributeList = 101                      ' AttributeListSyntax
        ''' <summary>
        ''' Represents a single attribute declaration within an attribute list.
        ''' </summary>
        Attribute = 102                          ' AttributeSyntax
        ''' <summary>
        ''' Represents a single attribute declaration within an attribute list.
        ''' </summary>
        AttributeTarget = 103                    ' AttributeTargetSyntax
        ''' <summary>
        ''' Represents a file-level attribute, in which the attributes have no other
        ''' syntactic element they are attached to.
        ''' </summary>
        AttributesStatement = 104                ' AttributesStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent an expression in a statement context. This may only be a invocation
        ''' or await expression in standard code but may be any expression in VB
        ''' Interactive code.
        ''' </summary>
        ExpressionStatement = 105                ' ExpressionStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent a "? expression" "Print" statement in VB Interactive code.
        ''' </summary>
        PrintStatement = 106                     ' PrintStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a While...End While statement, including the While, body and End
        ''' While.
        ''' </summary>
        WhileBlock = 107                         ' WhileBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an entire Using...End Using statement, including the Using, body and
        ''' End Using statements.
        ''' </summary>
        UsingBlock = 108                         ' UsingBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a entire SyncLock...End SyncLock block, including the SyncLock
        ''' statement, the enclosed statements, and the End SyncLock statement.
        ''' </summary>
        SyncLockBlock = 109                      ' SyncLockBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a With...End With block, include the With statement, the body of the
        ''' block and the End With statement.
        ''' </summary>
        WithBlock = 110                          ' WithBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the declaration of one or more local variables or constants.
        ''' </summary>
        LocalDeclarationStatement = 111          ' LocalDeclarationStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a label statement.
        ''' </summary>
        LabelStatement = 112                     ' LabelStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "GoTo" statement.
        ''' </summary>
        GoToStatement = 113                      ' GoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A label for a GoTo, Resume, or On Error statement. An identifier, line number,
        ''' or next keyword.
        ''' </summary>
        IdentifierLabel = 114                    ' LabelSyntax : ExpressionSyntax
        ''' <summary>
        ''' A label for a GoTo, Resume, or On Error statement. An identifier, line number,
        ''' or next keyword.
        ''' </summary>
        NumericLabel = 115                       ' LabelSyntax : ExpressionSyntax
        ''' <summary>
        ''' A label for a GoTo, Resume, or On Error statement. An identifier, line number,
        ''' or next keyword.
        ''' </summary>
        NextLabel = 116                          ' LabelSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a "Stop" or "End" statement. The Kind can be used to determine which
        ''' kind of statement this is.
        ''' </summary>
        StopStatement = 117                      ' StopOrEndStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Stop" or "End" statement. The Kind can be used to determine which
        ''' kind of statement this is.
        ''' </summary>
        EndStatement = 118                       ' StopOrEndStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitDoStatement = 119                    ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitForStatement = 120                   ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitSubStatement = 121                   ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitFunctionStatement = 122              ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitOperatorStatement = 123              ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitPropertyStatement = 124              ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitTryStatement = 125                   ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitSelectStatement = 126                ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitWhileStatement = 127                 ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Continue (block)" statement. THe kind of block referenced can be
        ''' determined by examining the Kind.
        ''' </summary>
        ContinueWhileStatement = 128             ' ContinueStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Continue (block)" statement. THe kind of block referenced can be
        ''' determined by examining the Kind.
        ''' </summary>
        ContinueDoStatement = 129                ' ContinueStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Continue (block)" statement. THe kind of block referenced can be
        ''' determined by examining the Kind.
        ''' </summary>
        ContinueForStatement = 130               ' ContinueStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Return" statement.
        ''' </summary>
        ReturnStatement = 131                    ' ReturnStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a line If-Then-Else statement.
        ''' </summary>
        SingleLineIfStatement = 132              ' SingleLineIfStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents part of a single line If statement, consisting of a beginning
        ''' if-statement, followed by a body of statement controlled by that beginning
        ''' statement. The Kind property returns if this is an SingleLineIf.
        ''' </summary>
        SingleLineIfPart = 133                   ' SingleLineIfPartSyntax
        ''' <summary>
        ''' Represents the Else part of an If statement, consisting of a Else statement,
        ''' followed by a body of statement controlled by that Else.
        ''' </summary>
        SingleLineElsePart = 134                 ' SingleLineElsePartSyntax
        ''' <summary>
        ''' Represents a block If...Then...Else...EndIf Statement. The Kind property can be
        ''' used to determine if it is a block or line If.
        ''' </summary>
        MultiLineIfBlock = 135                   ' MultiLineIfBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents part of an If statement, consisting of a beginning statement (If or
        ''' ElseIf), followed by a body of statement controlled by that beginning
        ''' statement. The Kind property returns if this is an If or ElseIf.
        ''' </summary>
        IfPart = 136                             ' IfPartSyntax
        ''' <summary>
        ''' Represents part of an If statement, consisting of a beginning statement (If or
        ''' ElseIf), followed by a body of statement controlled by that beginning
        ''' statement. The Kind property returns if this is an If or ElseIf.
        ''' </summary>
        ElseIfPart = 137                         ' IfPartSyntax
        ''' <summary>
        ''' Represents the Else part of an If statement, consisting of a Else statement,
        ''' followed by a body of statement controlled by that Else.
        ''' </summary>
        ElsePart = 138                           ' ElsePartSyntax
        ''' <summary>
        ''' Represents the If part or ElseIf part of a If...End If block (or line If). This
        ''' statement is always the Begin of a IfPart. The Kind can be examined to
        ''' determine if this is an If or an ElseIf statement.
        ''' </summary>
        IfStatement = 139                        ' IfStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the If part or ElseIf part of a If...End If block (or line If). This
        ''' statement is always the Begin of a IfPart. The Kind can be examined to
        ''' determine if this is an If or an ElseIf statement.
        ''' </summary>
        ElseIfStatement = 140                    ' IfStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the Else part of a If...End If block (or line If). This statement is
        ''' always the Begin of a ElsePart.
        ''' </summary>
        ElseStatement = 141                      ' ElseStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an entire Try...Catch...Finally...End Try statement.
        ''' </summary>
        TryBlock = 142                           ' TryBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents part of an Try...Catch...Finally...End Try statement, consisting of
        ''' a beginning statement (Try, Catch or Finally), followed by a body of statements
        ''' controlled by that beginning statement. The Kind property returns which kind of
        ''' part this is.
        ''' </summary>
        TryPart = 143                            ' TryPartSyntax
        ''' <summary>
        ''' Represents a Catch part of an Try...Catch...Finally...End Try statement,
        ''' consisting of a Catch statement, followed by a body of statements controlled by
        ''' that Catch statement. The Kind property returns which kind of part this is.
        ''' </summary>
        CatchPart = 144                          ' CatchPartSyntax
        ''' <summary>
        ''' Represents the Finally part of an Try...Catch...Finally...End Try statement,
        ''' consisting of a Finally statement, followed by a body of statements controlled
        ''' by the Finally.
        ''' </summary>
        FinallyPart = 145                        ' FinallyPartSyntax
        ''' <summary>
        ''' Represents the Try part part of a Try...Catch...Finally...End Try. This
        ''' statement is always the Begin of a TryPart.
        ''' </summary>
        TryStatement = 146                       ' TryStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the Catch part part of a Try...Catch...Finally...End Try. This
        ''' statement is always the Begin of a CatchPart.
        ''' </summary>
        CatchStatement = 147                     ' CatchStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the When/Filter clause of a Catch statement
        ''' </summary>
        CatchFilterClause = 148                  ' CatchFilterClauseSyntax
        ''' <summary>
        ''' Represents the Finally part part of a Try...Catch...Finally...End Try. This
        ''' statement is always the Begin of a FinallyPart.
        ''' </summary>
        FinallyStatement = 149                   ' FinallyStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the "Error" statement.
        ''' </summary>
        ErrorStatement = 150                     ' ErrorStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Goto statement.
        ''' </summary>
        OnErrorGoToZeroStatement = 151           ' OnErrorGoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Goto statement.
        ''' </summary>
        OnErrorGoToMinusOneStatement = 152       ' OnErrorGoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Goto statement.
        ''' </summary>
        OnErrorGoToLabelStatement = 153          ' OnErrorGoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Resume Next statement.
        ''' </summary>
        OnErrorResumeNextStatement = 154         ' OnErrorResumeNextStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Resume" statement. The Kind property can be used to determine if
        ''' this is a "Resume", "Resume Next" or "Resume label" statement.
        ''' </summary>
        ResumeStatement = 155                    ' ResumeStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Resume" statement. The Kind property can be used to determine if
        ''' this is a "Resume", "Resume Next" or "Resume label" statement.
        ''' </summary>
        ResumeLabelStatement = 156               ' ResumeStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Resume" statement. The Kind property can be used to determine if
        ''' this is a "Resume", "Resume Next" or "Resume label" statement.
        ''' </summary>
        ResumeNextStatement = 157                ' ResumeStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Select Case block, including the Select Case that begins it, the
        ''' contains Case blocks and the End Select.
        ''' </summary>
        SelectBlock = 158                        ' SelectBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Select Case statement. This statement always occurs as the Begin
        ''' of a SelectBlock.
        ''' </summary>
        SelectStatement = 159                    ' SelectStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a case statement and its subsequent block.
        ''' </summary>
        CaseBlock = 160                          ' CaseBlockSyntax
        ''' <summary>
        ''' Represents a case statement and its subsequent block.
        ''' </summary>
        CaseElseBlock = 161                      ' CaseBlockSyntax
        ''' <summary>
        ''' Represents a Case or Case Else statement. This statement is always the Begin of
        ''' a CaseBlock. If this is a Case Else statement, the Kind=CaseElse, otherwise the
        ''' Kind=Case.
        ''' </summary>
        CaseStatement = 162                      ' CaseStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Case or Case Else statement. This statement is always the Begin of
        ''' a CaseBlock. If this is a Case Else statement, the Kind=CaseElse, otherwise the
        ''' Kind=Case.
        ''' </summary>
        CaseElseStatement = 163                  ' CaseStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The "Else" part in a Case Else statement.
        ''' </summary>
        ElseCaseClause = 164                     ' ElseCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a single value in a Case.
        ''' </summary>
        SimpleCaseClause = 165                   ' SimpleCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a range "expression To expression" in a Case.
        ''' </summary>
        RangeCaseClause = 166                    ' RangeCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseEqualsClause = 167                   ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseNotEqualsClause = 168                ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseLessThanClause = 169                 ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseLessThanOrEqualClause = 170          ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseGreaterThanOrEqualClause = 171       ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseGreaterThanClause = 172              ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents the "SyncLock" statement. This statement always occurs as the Begin
        ''' of a SyncLockBlock.
        ''' </summary>
        SyncLockStatement = 173                  ' SyncLockStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Do-Loop block. The Kind property can be used to determine if this
        ''' is a top-test, bottom-test or infinite loop.
        ''' </summary>
        DoLoopTopTestBlock = 174                 ' DoLoopBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Do-Loop block. The Kind property can be used to determine if this
        ''' is a top-test, bottom-test or infinite loop.
        ''' </summary>
        DoLoopBottomTestBlock = 175              ' DoLoopBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Do-Loop block. The Kind property can be used to determine if this
        ''' is a top-test, bottom-test or infinite loop.
        ''' </summary>
        DoLoopForeverBlock = 176                 ' DoLoopBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Do statement that begins a Do-Loop block. This statement always occurs as
        ''' the Begin of a DoLoopBlock.
        ''' </summary>
        DoStatement = 177                        ' DoStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Loop statement that ends a Do-Loop block. This statement always occurs as
        ''' the End of a DoLoopBlock.
        ''' </summary>
        LoopStatement = 178                      ' LoopStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "While expression" or "Until expression" in a Do or Loop
        ''' statement. The Kind of the clause can be "WhileClause" or "UntilClause" to
        ''' indicate which kind of clause.
        ''' </summary>
        WhileClause = 179                        ' WhileUntilClauseSyntax
        ''' <summary>
        ''' Represents a "While expression" or "Until expression" in a Do or Loop
        ''' statement. The Kind of the clause can be "WhileClause" or "UntilClause" to
        ''' indicate which kind of clause.
        ''' </summary>
        UntilClause = 180                        ' WhileUntilClauseSyntax
        ''' <summary>
        ''' The While statement that begins a While...End While block. This statement
        ''' always occurs as the Begin of a WhileBlock.
        ''' </summary>
        WhileStatement = 181                     ' WhileStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a For or For Each block, including the introducting statement, the
        ''' body and the "Next" (which can be omitted if a containing For has a Next with
        ''' multiple variables).
        ''' </summary>
        ForBlock = 182                           ' ForBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a For or For Each block, including the introducting statement, the
        ''' body and the "Next" (which can be omitted if a containing For has a Next with
        ''' multiple variables).
        ''' </summary>
        ForEachBlock = 183                       ' ForBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The For statement that begins a For-Next block. This statement always occurs as
        ''' the Begin of a ForBlock. Most of the time, the End of that ForBlock is the
        ''' corresponding Next statement. However, multiple nested For statements are ended
        ''' by a single Next statement with multiple variables, then the inner For
        ''' statements will have End set to Nothing, and the Next statement is the End of
        ''' the outermost For statement that is being ended.
        ''' </summary>
        ForStatement = 184                       ' ForStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Step clause in a For Statement.
        ''' </summary>
        ForStepClause = 185                      ' ForStepClauseSyntax
        ''' <summary>
        ''' The For Each statement that begins a For Each-Next block. This statement always
        ''' occurs as the Begin of a ForBlock, and the body of the For Each-Next is the
        ''' Body of that ForBlock. Most of the time, the End of that ForBlock is the
        ''' corresponding Next statement. However, multiple nested For statements are ended
        ''' by a single Next statement with multiple variables, then the inner For
        ''' statements will have End set to Nothing, and the Next statement is the End of
        ''' the outermost For statement that is being ended.
        ''' </summary>
        ForEachStatement = 186                   ' ForEachStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Next statement that ends a For-Next or For Each-Next block. This statement
        ''' always occurs as the End of a ForBlock (with Kind=ForBlock or ForEachBlock),
        ''' and the body of the For-Next is the Body of that ForBlock. The Begin of that
        ''' ForBlock has the corresponding For or For Each statement.
        ''' </summary>
        NextStatement = 187                      ' NextStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Using statement that begins a Using block. This statement always occurs as
        ''' the Begin of a UsingBlock, and the body of the Using is the Body of that
        ''' UsingBlock.
        ''' </summary>
        UsingStatement = 188                     ' UsingStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Throw statement.
        ''' </summary>
        ThrowStatement = 189                     ' ThrowStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        SimpleAssignmentStatement = 190          ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        MidAssignmentStatement = 191             ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        AddAssignmentStatement = 192             ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        SubtractAssignmentStatement = 193        ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        MultiplyAssignmentStatement = 194        ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        DivideAssignmentStatement = 195          ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        IntegerDivideAssignmentStatement = 196   ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        ExponentiateAssignmentStatement = 197    ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        LeftShiftAssignmentStatement = 198       ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        RightShiftAssignmentStatement = 199      ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        ConcatenateAssignmentStatement = 200     ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a left-hand side of a MidAssignment statement.
        ''' </summary>
        MidExpression = 201                      ' MidExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represent an call statement (also known as a invocation statement).
        ''' </summary>
        CallStatement = 202                      ' CallStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an AddHandler or RemoveHandler statement. The Kind property
        ''' determines which one.
        ''' </summary>
        AddHandlerStatement = 203                ' AddRemoveHandlerStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an AddHandler or RemoveHandler statement. The Kind property
        ''' determines which one.
        ''' </summary>
        RemoveHandlerStatement = 204             ' AddRemoveHandlerStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent a RaiseEvent statement.
        ''' </summary>
        RaiseEventStatement = 205                ' RaiseEventStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "With" statement. This statement always occurs as the
        ''' BeginStatement of a WithBlock, and the body of the With is the Body of that
        ''' WithBlock.
        ''' </summary>
        WithStatement = 206                      ' WithStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a ReDim statement.
        ''' </summary>
        ReDimStatement = 207                     ' ReDimStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a ReDim statement.
        ''' </summary>
        ReDimPreserveStatement = 208             ' ReDimStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a ReDim statement clause.
        ''' </summary>
        RedimClause = 209                        ' RedimClauseSyntax
        ''' <summary>
        ''' Represents an "Erase" statement.
        ''' </summary>
        EraseStatement = 210                     ' EraseStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        CharacterLiteralExpression = 211         ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        TrueLiteralExpression = 212              ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        FalseLiteralExpression = 213             ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        NumericLiteralExpression = 214           ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        DateLiteralExpression = 215              ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        StringLiteralExpression = 216            ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        NothingLiteralExpression = 217           ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a parenthesized expression.
        ''' </summary>
        ParenthesizedExpression = 218            ' ParenthesizedExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Identifies the special instance "Me"
        ''' </summary>
        MeExpression = 219                       ' MeExpressionSyntax : InstanceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Identifies the special instance "MyBase"
        ''' </summary>
        MyBaseExpression = 220                   ' MyBaseExpressionSyntax : InstanceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Identifies the special instance "MyClass"
        ''' </summary>
        MyClassExpression = 221                  ' MyClassExpressionSyntax : InstanceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a GetType expression.
        ''' </summary>
        GetTypeExpression = 222                  ' GetTypeExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a TypeOf...Is or IsNot expression.
        ''' </summary>
        TypeOfIsExpression = 223                 ' TypeOfExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a TypeOf...Is or IsNot expression.
        ''' </summary>
        TypeOfIsNotExpression = 224              ' TypeOfExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a GetXmlNamespace expression.
        ''' </summary>
        GetXmlNamespaceExpression = 225          ' GetXmlNamespaceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents member access (.name) or dictionary access (!name). The Kind
        ''' property determines which kind of access.
        ''' </summary>
        SimpleMemberAccessExpression = 226       ' MemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents member access (.name) or dictionary access (!name). The Kind
        ''' property determines which kind of access.
        ''' </summary>
        DictionaryAccessExpression = 227         ' MemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML member element access (node.&lt;Element&gt;), attribute
        ''' access (node.@Attribute) or descendants access (node...&lt;Descendant&gt;). The
        ''' Kind property determines which kind of access.
        ''' </summary>
        XmlElementAccessExpression = 228         ' XmlMemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML member element access (node.&lt;Element&gt;), attribute
        ''' access (node.@Attribute) or descendants access (node...&lt;Descendant&gt;). The
        ''' Kind property determines which kind of access.
        ''' </summary>
        XmlDescendantAccessExpression = 229      ' XmlMemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML member element access (node.&lt;Element&gt;), attribute
        ''' access (node.@Attribute) or descendants access (node...&lt;Descendant&gt;). The
        ''' Kind property determines which kind of access.
        ''' </summary>
        XmlAttributeAccessExpression = 230       ' XmlMemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an invocation expression consisting of an invocation target and an
        ''' optional argument list or an array, parameterized property or object default
        ''' property index.
        ''' </summary>
        InvocationExpression = 231               ' InvocationExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a New expression that creates a new non-array object, possibly with
        ''' a "With" or "From" clause.
        ''' </summary>
        ObjectCreationExpression = 232           ' ObjectCreationExpressionSyntax : NewExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a New expression that create an object of anonymous type.
        ''' </summary>
        AnonymousObjectCreationExpression = 233  ' AnonymousObjectCreationExpressionSyntax : NewExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an expression that creates a new array.
        ''' </summary>
        ArrayCreationExpression = 234            ' ArrayCreationExpressionSyntax : NewExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an expression that creates a new array without naming the element
        ''' type.
        ''' </summary>
        CollectionInitializer = 235              ' CollectionInitializerSyntax : ExpressionSyntax
        CTypeExpression = 236                    ' CTypeExpressionSyntax : CastExpressionSyntax : ExpressionSyntax
        DirectCastExpression = 237               ' DirectCastExpressionSyntax : CastExpressionSyntax : ExpressionSyntax
        TryCastExpression = 238                  ' TryCastExpressionSyntax : CastExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a cast to a pre-defined type using a pre-defined cast expression,
        ''' such as CInt or CLng.
        ''' </summary>
        PredefinedCastExpression = 239           ' PredefinedCastExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        AddExpression = 240                      ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        SubtractExpression = 241                 ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        MultiplyExpression = 242                 ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        DivideExpression = 243                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        IntegerDivideExpression = 244            ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ExponentiateExpression = 245             ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LeftShiftExpression = 246                ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        RightShiftExpression = 247               ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ConcatenateExpression = 248              ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ModuloExpression = 249                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        EqualsExpression = 250                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        NotEqualsExpression = 251                ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LessThanExpression = 252                 ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LessThanOrEqualExpression = 253          ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        GreaterThanOrEqualExpression = 254       ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        GreaterThanExpression = 255              ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        IsExpression = 256                       ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        IsNotExpression = 257                    ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LikeExpression = 258                     ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        OrExpression = 259                       ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ExclusiveOrExpression = 260              ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        AndExpression = 261                      ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        OrElseExpression = 262                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        AndAlsoExpression = 263                  ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        UnaryPlusExpression = 264                ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        UnaryMinusExpression = 265               ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        NotExpression = 266                      ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        AddressOfExpression = 267                ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a conditional expression, If(condition, true-expr, false-expr) or
        ''' If(expr, nothing-expr).
        ''' </summary>
        BinaryConditionalExpression = 268        ' BinaryConditionalExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a conditional expression, If(condition, true-expr, false-expr) or
        ''' If(expr, nothing-expr).
        ''' </summary>
        TernaryConditionalExpression = 269       ' TernaryConditionalExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single line lambda expression.
        ''' </summary>
        SingleLineFunctionLambdaExpression = 270 ' SingleLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single line lambda expression.
        ''' </summary>
        SingleLineSubLambdaExpression = 271      ' SingleLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a multi-line lambda expression.
        ''' </summary>
        MultiLineFunctionLambdaExpression = 272  ' MultiLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a multi-line lambda expression.
        ''' </summary>
        MultiLineSubLambdaExpression = 273       ' MultiLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the header part of a lambda expression
        ''' </summary>
        SubLambdaHeader = 274                    ' LambdaHeaderSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the header part of a lambda expression
        ''' </summary>
        FunctionLambdaHeader = 275               ' LambdaHeaderSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a parenthesized argument list.
        ''' </summary>
        ArgumentList = 276                       ' ArgumentListSyntax
        ''' <summary>
        ''' Represents an omitted argument in an argument list. An omitted argument is not
        ''' considered a syntax error but a valid case when no argument is required.
        ''' </summary>
        OmittedArgument = 277                    ' OmittedArgumentSyntax : ArgumentSyntax
        ''' <summary>
        ''' Represents a simple argument that is just an expression.
        ''' </summary>
        SimpleArgument = 278                     ' SimpleArgumentSyntax : ArgumentSyntax
        ''' <summary>
        ''' Represents a named argument, such as "Value:=7".
        ''' </summary>
        NamedArgument = 279                      ' NamedArgumentSyntax : ArgumentSyntax
        ''' <summary>
        ''' Represents a range argument, such as "0 to 5", used in array bounds. The
        ''' "Value" property represents the upper bound of the range.
        ''' </summary>
        RangeArgument = 280                      ' RangeArgumentSyntax : ArgumentSyntax
        ''' <summary>
        ''' This class represents a query expression. A query expression is composed of one
        ''' or more query operators in a row. The first query operator must be a From or
        ''' Aggregate.
        ''' </summary>
        QueryExpression = 281                    ' QueryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a single variable of the form "x [As Type] In expression" for use in
        ''' query expressions.
        ''' </summary>
        CollectionRangeVariable = 282            ' CollectionRangeVariableSyntax
        ''' <summary>
        ''' Describes a single variable of the form "[x [As Type] =] expression" for use in
        ''' query expressions.
        ''' </summary>
        ExpressionRangeVariable = 283            ' ExpressionRangeVariableSyntax
        ''' <summary>
        ''' Describes a single variable of the form "[x [As Type] =] aggregation-function"
        ''' for use in the Into clause of Aggregate or Group By or Group Join query
        ''' operators.
        ''' </summary>
        AggregationRangeVariable = 284           ' AggregationRangeVariableSyntax
        ''' <summary>
        ''' Represents the name and optional type of an expression range variable.
        ''' </summary>
        VariableNameEquals = 285                 ' VariableNameEqualsSyntax
        ''' <summary>
        ''' Represents an invocation of an Aggregation function in the aggregation range
        ''' variable declaration of a Group By, Group Join or Aggregate query operator.
        ''' </summary>
        FunctionAggregation = 286                ' FunctionAggregationSyntax : AggregationSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the use of "Group" as the aggregation function in the in the
        ''' aggregation range variable declaration of a Group By or Group Join query
        ''' operator.
        ''' </summary>
        GroupAggregation = 287                   ' GroupAggregationSyntax : AggregationSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a "From" query operator. If this is the beginning of a query, the
        ''' Source will be Nothing. Otherwise, the Source will be the part of the query to
        ''' the left of the From.
        ''' </summary>
        FromClause = 288                         ' FromClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Let" query operator.
        ''' </summary>
        LetClause = 289                          ' LetClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents an Aggregate query operator.
        ''' </summary>
        AggregateClause = 290                    ' AggregateClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Distinct" query operator.
        ''' </summary>
        DistinctClause = 291                     ' DistinctClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Where" query operator.
        ''' </summary>
        WhereClause = 292                        ' WhereClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip While" or "Take While" query operator. The Kind property
        ''' tells which.
        ''' </summary>
        SkipWhileClause = 293                    ' PartitionWhileClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip While" or "Take While" query operator. The Kind property
        ''' tells which.
        ''' </summary>
        TakeWhileClause = 294                    ' PartitionWhileClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip" or "Take" query operator. The Kind property tells which.
        ''' </summary>
        SkipClause = 295                         ' PartitionClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip" or "Take" query operator. The Kind property tells which.
        ''' </summary>
        TakeClause = 296                         ' PartitionClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Group By" query operator.
        ''' </summary>
        GroupByClause = 297                      ' GroupByClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "expression Equals expression" condition in a Join.
        ''' </summary>
        JoinCondition = 298                      ' JoinConditionSyntax
        ''' <summary>
        ''' Represents a Join query operator.
        ''' </summary>
        SimpleJoinClause = 299                   ' SimpleJoinClauseSyntax : JoinClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Group Join" query operator.
        ''' </summary>
        GroupJoinClause = 300                    ' GroupJoinClauseSyntax : JoinClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Order By" query operator.
        ''' </summary>
        OrderByClause = 301                      ' OrderByClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' An expression to order by, plus an optional ordering. The Kind indicates
        ''' whether to order in ascending or descending order.
        ''' </summary>
        AscendingOrdering = 302                  ' OrderingSyntax
        ''' <summary>
        ''' An expression to order by, plus an optional ordering. The Kind indicates
        ''' whether to order in ascending or descending order.
        ''' </summary>
        DescendingOrdering = 303                 ' OrderingSyntax
        ''' <summary>
        ''' Represents the "Select" query operator.
        ''' </summary>
        SelectClause = 304                       ' SelectClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents an XML Document literal expression.
        ''' </summary>
        XmlDocument = 305                        ' XmlDocumentSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the XML declaration prologue in an XML literal expression.
        ''' </summary>
        XmlDeclaration = 306                     ' XmlDeclarationSyntax
        ''' <summary>
        ''' Represents an XML document prologue option - version, encoding, standalone or
        ''' whitespace in an XML literal expression.
        ''' </summary>
        XmlDeclarationOption = 307               ' XmlDeclarationOptionSyntax
        ''' <summary>
        ''' Represents an XML element with content in an XML literal expression.
        ''' </summary>
        XmlElement = 308                         ' XmlElementSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents Xml text.
        ''' </summary>
        XmlText = 309                            ' XmlTextSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the start tag of an XML element of the form &lt;element&gt;.
        ''' </summary>
        XmlElementStartTag = 310                 ' XmlElementStartTagSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the end tag of an XML element of the form &lt;/element&gt;.
        ''' </summary>
        XmlElementEndTag = 311                   ' XmlElementEndTagSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an empty XML element of the form &lt;element /&gt;
        ''' </summary>
        XmlEmptyElement = 312                    ' XmlEmptyElementSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML attribute in an XML literal expression.
        ''' </summary>
        XmlAttribute = 313                       ' XmlAttributeSyntax : BaseXmlAttributeSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a string of XML characters embedded as the content of an XML
        ''' element.
        ''' </summary>
        XmlString = 314                          ' XmlStringSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML name of the form 'name' appearing in GetXmlNamespace().
        ''' </summary>
        XmlPrefixName = 315                      ' XmlPrefixNameSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML name of the form 'name' or 'namespace:name' appearing in
        ''' source as part of an XML literal or member access expression or an XML
        ''' namespace import clause.
        ''' </summary>
        XmlName = 316                            ' XmlNameSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML name of the form &lt;xml-name&gt; appearing in source as part
        ''' of an XML literal or member access expression or an XML namespace import
        ''' clause.
        ''' </summary>
        XmlBracketedName = 317                   ' XmlBracketedNameSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML namespace prefix of the form 'prefix:' as in xml:ns="".
        ''' </summary>
        XmlPrefix = 318                          ' XmlPrefixSyntax
        ''' <summary>
        ''' Represents an XML comment of the form &lt;!-- Comment --&gt; appearing in an
        ''' XML literal expression.
        ''' </summary>
        XmlComment = 319                         ' XmlCommentSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML processing instruction of the form '&lt;? XMLProcessingTarget
        ''' XMLProcessingValue ?&gt;'.
        ''' </summary>
        XmlProcessingInstruction = 320           ' XmlProcessingInstructionSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML CDATA section in an XML literal expression.
        ''' </summary>
        XmlCDataSection = 321                    ' XmlCDataSectionSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an embedded expression in an XML literal e.g. '&lt;name&gt;&lt;%=
        ''' obj.Name =%&gt;&lt;/name&gt;'.
        ''' </summary>
        XmlEmbeddedExpression = 322              ' XmlEmbeddedExpressionSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an array type, such as "A() or "A(,)", without bounds specified for
        ''' the array.
        ''' </summary>
        ArrayType = 323                          ' ArrayTypeSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' A type name that represents a nullable type, such as "Integer?".
        ''' </summary>
        NullableType = 324                       ' NullableTypeSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an occurrence of a Visual Basic built-in type such as Integer or
        ''' String in source code.
        ''' </summary>
        PredefinedType = 325                     ' PredefinedTypeSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a type name consisting of a single identifier (which might include
        ''' brackets or a type character).
        ''' </summary>
        IdentifierName = 326                     ' IdentifierNameSyntax : SimpleNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a simple type name with one or more generic arguments, such as "X(Of
        ''' Y, Z).
        ''' </summary>
        GenericName = 327                        ' GenericNameSyntax : SimpleNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a qualified type name, for example X.Y or X(Of Z).Y.
        ''' </summary>
        QualifiedName = 328                      ' QualifiedNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a name in the global namespace.
        ''' </summary>
        GlobalName = 329                         ' GlobalNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a parenthesized list of generic type arguments.
        ''' </summary>
        TypeArgumentList = 330                   ' TypeArgumentListSyntax
        ''' <summary>
        ''' Syntax node class that represents a value of 'cref' attribute inside
        ''' documentation comment trivia.
        ''' </summary>
        CrefReference = 331                      ' CrefReferenceSyntax
        ''' <summary>
        ''' Represents a parenthesized list of argument types for a signature inside
        ''' CrefReferenceSyntax syntax.
        ''' </summary>
        CrefSignature = 332                      ' CrefSignatureSyntax
        CrefSignaturePart = 333                  ' CrefSignaturePartSyntax
        CrefOperatorReference = 334              ' CrefOperatorReferenceSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        QualifiedCrefOperatorReference = 335     ' QualifiedCrefOperatorReferenceSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represent a Yield statement.
        ''' </summary>
        YieldStatement = 336                     ' YieldStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent a Await expression.
        ''' </summary>
        AwaitExpression = 337                    ' AwaitExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AddHandlerKeyword = 338                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AddressOfKeyword = 339                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AliasKeyword = 340                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AndKeyword = 341                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AndAlsoKeyword = 342                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AsKeyword = 343                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        BooleanKeyword = 344                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByRefKeyword = 345                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByteKeyword = 346                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByValKeyword = 347                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CallKeyword = 348                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CaseKeyword = 349                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CatchKeyword = 350                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CBoolKeyword = 351                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CByteKeyword = 352                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CCharKeyword = 353                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CDateKeyword = 354                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CDecKeyword = 355                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CDblKeyword = 356                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CharKeyword = 357                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CIntKeyword = 358                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ClassKeyword = 359                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CLngKeyword = 360                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CObjKeyword = 361                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ConstKeyword = 362                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReferenceKeyword = 363                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ContinueKeyword = 364                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CSByteKeyword = 365                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CShortKeyword = 366                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CSngKeyword = 367                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CStrKeyword = 368                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CTypeKeyword = 369                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CUIntKeyword = 370                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CULngKeyword = 371                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CUShortKeyword = 372                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DateKeyword = 373                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DecimalKeyword = 374                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DeclareKeyword = 375                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DefaultKeyword = 376                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DelegateKeyword = 377                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DimKeyword = 378                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DirectCastKeyword = 379                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DoKeyword = 380                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DoubleKeyword = 381                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EachKeyword = 382                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ElseKeyword = 383                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ElseIfKeyword = 384                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EndKeyword = 385                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EnumKeyword = 386                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EraseKeyword = 387                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ErrorKeyword = 388                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EventKeyword = 389                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExitKeyword = 390                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FalseKeyword = 391                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FinallyKeyword = 392                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ForKeyword = 393                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FriendKeyword = 394                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FunctionKeyword = 395                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GetKeyword = 396                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GetTypeKeyword = 397                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GetXmlNamespaceKeyword = 398             ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GlobalKeyword = 399                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GoToKeyword = 400                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        HandlesKeyword = 401                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IfKeyword = 402                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ImplementsKeyword = 403                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ImportsKeyword = 404                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InKeyword = 405                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InheritsKeyword = 406                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IntegerKeyword = 407                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InterfaceKeyword = 408                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsKeyword = 409                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsNotKeyword = 410                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LetKeyword = 411                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LibKeyword = 412                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LikeKeyword = 413                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LongKeyword = 414                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LoopKeyword = 415                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MeKeyword = 416                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ModKeyword = 417                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ModuleKeyword = 418                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MustInheritKeyword = 419                 ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MustOverrideKeyword = 420                ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MyBaseKeyword = 421                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MyClassKeyword = 422                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NamespaceKeyword = 423                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NarrowingKeyword = 424                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NextKeyword = 425                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NewKeyword = 426                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NotKeyword = 427                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NothingKeyword = 428                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NotInheritableKeyword = 429              ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NotOverridableKeyword = 430              ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ObjectKeyword = 431                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OfKeyword = 432                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OnKeyword = 433                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OperatorKeyword = 434                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OptionKeyword = 435                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OptionalKeyword = 436                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OrKeyword = 437                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OrElseKeyword = 438                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OverloadsKeyword = 439                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OverridableKeyword = 440                 ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OverridesKeyword = 441                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ParamArrayKeyword = 442                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PartialKeyword = 443                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PrivateKeyword = 444                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PropertyKeyword = 445                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ProtectedKeyword = 446                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PublicKeyword = 447                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        RaiseEventKeyword = 448                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReadOnlyKeyword = 449                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReDimKeyword = 450                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        REMKeyword = 451                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        RemoveHandlerKeyword = 452               ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ResumeKeyword = 453                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReturnKeyword = 454                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SByteKeyword = 455                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SelectKeyword = 456                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SetKeyword = 457                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ShadowsKeyword = 458                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SharedKeyword = 459                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ShortKeyword = 460                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SingleKeyword = 461                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StaticKeyword = 462                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StepKeyword = 463                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StopKeyword = 464                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StringKeyword = 465                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StructureKeyword = 466                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SubKeyword = 467                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SyncLockKeyword = 468                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ThenKeyword = 469                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ThrowKeyword = 470                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ToKeyword = 471                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TrueKeyword = 472                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TryKeyword = 473                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TryCastKeyword = 474                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TypeOfKeyword = 475                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UIntegerKeyword = 476                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ULongKeyword = 477                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UShortKeyword = 478                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UsingKeyword = 479                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WhenKeyword = 480                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WhileKeyword = 481                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WideningKeyword = 482                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WithKeyword = 483                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WithEventsKeyword = 484                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WriteOnlyKeyword = 485                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        XorKeyword = 486                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EndIfKeyword = 487                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GosubKeyword = 488                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        VariantKeyword = 489                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WendKeyword = 490                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AggregateKeyword = 491                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AllKeyword = 492                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AnsiKeyword = 493                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AscendingKeyword = 494                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AssemblyKeyword = 495                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AutoKeyword = 496                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        BinaryKeyword = 497                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByKeyword = 498                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CompareKeyword = 499                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CustomKeyword = 500                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DescendingKeyword = 501                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DisableKeyword = 502                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DistinctKeyword = 503                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EnableKeyword = 504                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EqualsKeyword = 505                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExplicitKeyword = 506                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExternalSourceKeyword = 507              ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExternalChecksumKeyword = 508            ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FromKeyword = 509                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GroupKeyword = 510                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InferKeyword = 511                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IntoKeyword = 512                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsFalseKeyword = 513                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsTrueKeyword = 514                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        JoinKeyword = 515                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        KeyKeyword = 516                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MidKeyword = 517                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OffKeyword = 518                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OrderKeyword = 519                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OutKeyword = 520                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PreserveKeyword = 521                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        RegionKeyword = 522                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SkipKeyword = 523                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StrictKeyword = 524                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TakeKeyword = 525                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TextKeyword = 526                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UnicodeKeyword = 527                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UntilKeyword = 528                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WarningKeyword = 529                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WhereKeyword = 530                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TypeKeyword = 531                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        XmlKeyword = 532                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AsyncKeyword = 533                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AwaitKeyword = 534                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IteratorKeyword = 535                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        YieldKeyword = 536                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        ExclamationToken = 537                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AtToken = 538                            ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CommaToken = 539                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        HashToken = 540                          ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AmpersandToken = 541                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SingleQuoteToken = 542                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        OpenParenToken = 543                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CloseParenToken = 544                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        OpenBraceToken = 545                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CloseBraceToken = 546                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SemicolonToken = 547                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AsteriskToken = 548                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        PlusToken = 549                          ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        MinusToken = 550                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        DotToken = 551                           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SlashToken = 552                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        ColonToken = 553                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanToken = 554                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanEqualsToken = 555                ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanGreaterThanToken = 556           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EqualsToken = 557                        ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanToken = 558                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanEqualsToken = 559             ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        BackslashToken = 560                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CaretToken = 561                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        ColonEqualsToken = 562                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AmpersandEqualsToken = 563               ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AsteriskEqualsToken = 564                ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        PlusEqualsToken = 565                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        MinusEqualsToken = 566                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SlashEqualsToken = 567                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        BackslashEqualsToken = 568               ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CaretEqualsToken = 569                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanLessThanToken = 570              ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanGreaterThanToken = 571        ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanLessThanEqualsToken = 572        ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanGreaterThanEqualsToken = 573  ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        QuestionToken = 574                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        DoubleQuoteToken = 575                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        StatementTerminatorToken = 576           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EndOfFileToken = 577                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EmptyToken = 578                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SlashGreaterThanToken = 579              ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanSlashToken = 580                 ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanExclamationMinusMinusToken = 581 ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        MinusMinusGreaterThanToken = 582         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanQuestionToken = 583              ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        QuestionGreaterThanToken = 584           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanPercentEqualsToken = 585         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        PercentGreaterThanToken = 586            ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        BeginCDataToken = 587                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EndCDataToken = 588                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EndOfXmlToken = 589                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a sequence of characters appearing in source with no possible
        ''' meaning in the Visual Basic language (e.g. the semicolon ';'). This token
        ''' should only appear in SkippedTokenTrivia as an artifact of parsing error
        ''' recovery.
        ''' </summary>
        BadToken = 590                           ' BadTokenSyntax : PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an Xml NCName per Namespaces in XML 1.0
        ''' </summary>
        XmlNameToken = 591                       ' XmlNameTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents character data in Xml content also known as PCData or in an Xml
        ''' attribute value. All text is here for now even text that does not need
        ''' normalization such as comment, pi and cdata text.
        ''' </summary>
        XmlTextLiteralToken = 592                ' XmlTextTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents character data in Xml content also known as PCData or in an Xml
        ''' attribute value. All text is here for now even text that does not need
        ''' normalization such as comment, pi and cdata text.
        ''' </summary>
        XmlEntityLiteralToken = 593              ' XmlTextTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents character data in Xml content also known as PCData or in an Xml
        ''' attribute value. All text is here for now even text that does not need
        ''' normalization such as comment, pi and cdata text.
        ''' </summary>
        DocumentationCommentLineBreakToken = 594 ' XmlTextTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an identifier token. This might include brackets around the name and
        ''' a type character.
        ''' </summary>
        IdentifierToken = 595                    ' IdentifierTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an integer literal token.
        ''' </summary>
        IntegerLiteralToken = 596                ' IntegerLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an floating literal token.
        ''' </summary>
        FloatingLiteralToken = 597               ' FloatingLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a Decimal literal token.
        ''' </summary>
        DecimalLiteralToken = 598                ' DecimalLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an Date literal token.
        ''' </summary>
        DateLiteralToken = 599                   ' DateLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an string literal token.
        ''' </summary>
        StringLiteralToken = 600                 ' StringLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an string literal token.
        ''' </summary>
        CharacterLiteralToken = 601              ' CharacterLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents tokens that were skipped by the parser as part of error recovery,
        ''' and thus are not part of any syntactic structure.
        ''' </summary>
        SkippedTokensTrivia = 602                ' SkippedTokensTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents a documentation comment e.g. ''' &lt;Summary&gt; apearing in source.
        ''' </summary>
        DocumentationCommentTrivia = 603         ' DocumentationCommentTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' A symbol referenced by a cref attribute (e.g. in a &lt;see&gt; or
        ''' &lt;seealso&gt; documentation comment tag). For example, the M in &lt;see
        ''' cref="M" /&gt;.
        ''' </summary>
        XmlCrefAttribute = 604                   ' XmlCrefAttributeSyntax : BaseXmlAttributeSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' A param or type param symbol referenced by a name attribute (e.g. in a
        ''' &lt;param&gt; or &lt;typeparam&gt; documentation comment tag). For example, the
        ''' M in &lt;param name="M" /&gt;.
        ''' </summary>
        XmlNameAttribute = 605                   ' XmlNameAttributeSyntax : BaseXmlAttributeSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' ExpressionSyntax node representing the object conditionally accessed.
        ''' </summary>
        ConditionalAccessExpression = 606        ' ConditionalAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents true whitespace: spaces, tabs, newlines and the like.
        ''' </summary>
        WhitespaceTrivia = 607                   ' SyntaxTrivia
        ''' <summary>
        ''' Represents line breaks that are syntactically insignificant.
        ''' </summary>
        EndOfLineTrivia = 608                    ' SyntaxTrivia
        ''' <summary>
        ''' Represents colons that are syntactically insignificant.
        ''' </summary>
        ColonTrivia = 609                        ' SyntaxTrivia
        ''' <summary>
        ''' Represents a comment.
        ''' </summary>
        CommentTrivia = 610                      ' SyntaxTrivia
        ''' <summary>
        ''' Represents an explicit line continuation character at the end of a line, i.e.,
        ''' _
        ''' </summary>
        LineContinuationTrivia = 611             ' SyntaxTrivia
        ''' <summary>
        ''' Represents a ''' prefix for an XML Documentation Comment.
        ''' </summary>
        DocumentationCommentExteriorTrivia = 612 ' SyntaxTrivia
        ''' <summary>
        ''' Represents text in a false preprocessor block
        ''' </summary>
        DisabledTextTrivia = 613                 ' SyntaxTrivia
        ''' <summary>
        ''' Represents a #Const pre-processing constant declaration appearing in source.
        ''' </summary>
        ConstDirectiveTrivia = 614               ' ConstDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of an #If pre-processing directive appearing in
        ''' source.
        ''' </summary>
        IfDirectiveTrivia = 615                  ' IfDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of an #If pre-processing directive appearing in
        ''' source.
        ''' </summary>
        ElseIfDirectiveTrivia = 616              ' IfDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #Else pre-processing directive appearing in source.
        ''' </summary>
        ElseDirectiveTrivia = 617                ' ElseDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #End If pre-processing directive appearing in source.
        ''' </summary>
        EndIfDirectiveTrivia = 618               ' EndIfDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of a #Region directive appearing in source.
        ''' </summary>
        RegionDirectiveTrivia = 619              ' RegionDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #End Region directive appearing in source.
        ''' </summary>
        EndRegionDirectiveTrivia = 620           ' EndRegionDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of a #ExternalSource pre-processing directive
        ''' appearing in source.
        ''' </summary>
        ExternalSourceDirectiveTrivia = 621      ' ExternalSourceDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #End ExternalSource pre-processing directive appearing in source.
        ''' </summary>
        EndExternalSourceDirectiveTrivia = 622   ' EndExternalSourceDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #ExternalChecksum pre-processing directive appearing in source.
        ''' </summary>
        ExternalChecksumDirectiveTrivia = 623    ' ExternalChecksumDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents #Enable Warning pre-processing directive appearing in source.
        ''' </summary>
        EnableWarningDirectiveTrivia = 624       ' EnableWarningDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents #Disable Warning pre-processing directive appearing in source.
        ''' </summary>
        DisableWarningDirectiveTrivia = 625      ' DisableWarningDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #r directive appearing in scripts.
        ''' </summary>
        ReferenceDirectiveTrivia = 626           ' ReferenceDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an unrecognized pre-processing directive. This occurs when the
        ''' parser encounters a hash '#' token at the beginning of a physical line but does
        ''' recognize the text that follows as a valid Visual Basic pre-processing
        ''' directive.
        ''' </summary>
        BadDirectiveTrivia = 627                 ' BadDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
    End Enum

End Namespace

