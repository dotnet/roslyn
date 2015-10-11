' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Enumeration with all Visual Basic syntax node kinds.
    ''' </summary>
    Public Enum SyntaxKind As UShort
        ' ADD NEW SYNTAX TO THE END OF THIS ENUM OR YOU WILL BREAK BINARY COMPATIBILITY
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
        EndIfStatement = 5                       ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndUsingStatement = 6                    ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndWithStatement = 7                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSelectStatement = 8                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndStructureStatement = 9                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndEnumStatement = 10                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndInterfaceStatement = 11                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndClassStatement = 12                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndModuleStatement = 13                  ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndNamespaceStatement = 14               ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSubStatement = 15                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndFunctionStatement = 16                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndGetStatement = 17                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSetStatement = 18                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndPropertyStatement = 19                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndOperatorStatement = 20                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndEventStatement = 21                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndAddHandlerStatement = 22              ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndRemoveHandlerStatement = 23           ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndRaiseEventStatement = 24              ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndWhileStatement = 25                   ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndTryStatement = 26                     ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an "End XXX" statement, where XXX is a single keyword.
        ''' </summary>
        EndSyncLockStatement = 27                ' EndBlockStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an entire source file of VB code.
        ''' </summary>
        CompilationUnit = 38                     ' CompilationUnitSyntax
        ''' <summary>
        ''' Represents an Option statement, such as "Option Strict On".
        ''' </summary>
        OptionStatement = 41                     ' OptionStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Imports statement, which has one or more imports clauses.
        ''' </summary>
        ImportsStatement = 42                    ' ImportsStatementSyntax : DeclarationStatementSyntax : StatementSyntax

        ' AliasImportsClause = 43                ' Removed.

        ''' <summary>
        ''' Represents the clause of an Imports statement that imports all members of a type or namespace or aliases a type or namespace.
        ''' </summary>
        SimpleImportsClause = 44                ' SimpleImportsClauseSyntax : ImportsClauseSyntax
        ''' <summary>
        ''' Defines a XML namespace for XML expressions.
        ''' </summary>
        XmlNamespaceImportsClause = 45           ' XmlNamespaceImportsClauseSyntax : ImportsClauseSyntax
        ''' <summary>
        ''' Represents a Namespace statement, its contents and the End Namespace statement.
        ''' </summary>
        NamespaceBlock = 48                      ' NamespaceBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a namespace declaration. This node always
        ''' appears as the Begin of a BlockStatement with Kind=NamespaceBlock.
        ''' </summary>
        NamespaceStatement = 49                  ' NamespaceStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of Module, its contents and the End statement that
        ''' ends it.
        ''' </summary>
        ModuleBlock = 50                         ' ModuleBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of a Structure, its contents and the End statement
        ''' that ends it.
        ''' </summary>
        StructureBlock = 51                      ' StructureBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of a Interface, its contents and the End statement
        ''' that ends it.
        ''' </summary>
        InterfaceBlock = 52                      ' InterfaceBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of a Class its contents and the End statement that
        ''' ends it.
        ''' </summary>
        ClassBlock = 53                          ' ClassBlockSyntax : TypeBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a declaration of an Enum, its contents and the End Enum statement
        ''' that ends it.
        ''' </summary>
        EnumBlock = 54                           ' EnumBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Inherits statement in a Class, Structure or Interface.
        ''' </summary>
        InheritsStatement = 57                   ' InheritsStatementSyntax : InheritsOrImplementsStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Implements statement in a Class or Structure.
        ''' </summary>
        ImplementsStatement = 58                 ' ImplementsStatementSyntax : InheritsOrImplementsStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Module declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=ModuleDeclarationBlock.
        ''' </summary>
        ModuleStatement = 59                     ' ModuleStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Structure declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=StructureDeclarationBlock.
        ''' </summary>
        StructureStatement = 60                  ' StructureStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Interface declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=InterfaceDeclarationBlock.
        ''' </summary>
        InterfaceStatement = 61                  ' InterfaceStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of a Class declaration. This node always
        ''' appears as the Begin of a TypeBlock with Kind=ClassDeclarationBlock.
        ''' </summary>
        ClassStatement = 62                      ' ClassStatementSyntax : TypeStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the beginning statement of an Enum declaration. This node always
        ''' appears as the Begin of an EnumBlock with Kind=EnumDeclarationBlock.
        ''' </summary>
        EnumStatement = 63                       ' EnumStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the type parameter list in a declaration.
        ''' </summary>
        TypeParameterList = 66                   ' TypeParameterListSyntax
        ''' <summary>
        ''' Represents a type parameter on a generic type declaration.
        ''' </summary>
        TypeParameter = 67                       ' TypeParameterSyntax
        ''' <summary>
        ''' One of the type parameter constraints clauses. This represents a constraint
        ''' clause in the form of "As Constraint".
        ''' </summary>
        TypeParameterSingleConstraintClause = 70  ' TypeParameterSingleConstraintClauseSyntax : TypeParameterConstraintClauseSyntax
        ''' <summary>
        ''' One of the type parameter constraints clauses. This represents a constraint
        ''' clause in the form of "As { Constraints }".
        ''' </summary>
        TypeParameterMultipleConstraintClause = 71  ' TypeParameterMultipleConstraintClauseSyntax : TypeParameterConstraintClauseSyntax
        ''' <summary>
        ''' One of the special type parameter constraints: New, Class or Structure. Which
        ''' kind of special constraint it is can be obtained from the Kind property and is
        ''' one of: NewConstraint, ReferenceConstraint or ValueConstraint.
        ''' </summary>
        NewConstraint = 72                       ' SpecialConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' One of the special type parameter constraints: New, Class or Structure. Which
        ''' kind of special constraint it is can be obtained from the Kind property and is
        ''' one of: NewConstraint, ReferenceConstraint or ValueConstraint.
        ''' </summary>
        ClassConstraint = 73                     ' SpecialConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' One of the special type parameter constraints: New, Class or Structure. Which
        ''' kind of special constraint it is can be obtained from the Kind property and is
        ''' one of: NewConstraint, ReferenceConstraint or ValueConstraint.
        ''' </summary>
        StructureConstraint = 74                 ' SpecialConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' Represents a type parameter constraint that is a type.
        ''' </summary>
        TypeConstraint = 75                      ' TypeConstraintSyntax : ConstraintSyntax
        ''' <summary>
        ''' Represents a name and value in an EnumDeclarationBlock.
        ''' </summary>
        EnumMemberDeclaration = 78               ' EnumMemberDeclarationSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Function or Sub block declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' </summary>
        SubBlock = 79                            ' MethodBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Function or Sub block declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' </summary>
        FunctionBlock = 80                       ' MethodBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a constructor block declaration: A declaration that has a beginning
        ''' declaration, a body of executable statements and an end statement.
        ''' </summary>
        ConstructorBlock = 81                    ' ConstructorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an Operator block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' </summary>
        OperatorBlock = 82                       ' OperatorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        GetAccessorBlock = 83                    ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        SetAccessorBlock = 84                    ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        AddHandlerAccessorBlock = 85                     ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        RemoveHandlerAccessorBlock = 86                  ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an accessor block member declaration: A declaration that has a
        ''' beginning declaration, a body of executable statements and an end statement.
        ''' Examples include property accessors and custom event accessors.
        ''' </summary>
        RaiseEventAccessorBlock = 87                     ' AccessorBlockSyntax : MethodBlockBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a block property declaration: A declaration that has a beginning
        ''' declaration, some get or set accessor blocks and an end statement.
        ''' </summary>
        PropertyBlock = 88                       ' PropertyBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a custom event declaration: A declaration that has a beginning event
        ''' declaration, some accessor blocks and an end statement.
        ''' </summary>
        EventBlock = 89                          ' EventBlockSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the parameter list in a method declaration.
        ''' </summary>
        ParameterList = 92                       ' ParameterListSyntax
        ''' <summary>
        ''' The statement that declares a Sub or Function. If this method has a body, this
        ''' statement will be the Begin of a BlockStatement with
        ''' Kind=MethodDeclarationBlock, and the body of the method will be the Body of
        ''' that BlockStatement.
        ''' </summary>
        SubStatement = 93                        ' MethodStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The statement that declares a Sub or Function. If this method has a body, this
        ''' statement will be the Begin of a BlockStatement with
        ''' Kind=MethodDeclarationBlock, and the body of the method will be the Body of
        ''' that BlockStatement.
        ''' </summary>
        FunctionStatement = 94                   ' MethodStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares a constructor. This statement will be the Begin of a
        ''' BlockStatement with Kind=MethodDeclarationBlock, and the body of the method
        ''' will be the Body of that BlockStatement.
        ''' </summary>
        SubNewStatement = 95                     ' SubNewStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A Declare statement that declares an external DLL method.
        ''' </summary>
        DeclareSubStatement = 96                 ' DeclareStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A Declare statement that declares an external DLL method.
        ''' </summary>
        DeclareFunctionStatement = 97            ' DeclareStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares a delegate type.
        ''' </summary>
        DelegateSubStatement = 98                ' DelegateStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares a delegate type.
        ''' </summary>
        DelegateFunctionStatement = 99           ' DelegateStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares an event. If the event being declared is a custom
        ''' event, this statement will be the Begin of a PropertyOrEventBlock, and the
        ''' accessors will be part of the Accessors of that node.
        ''' </summary>
        EventStatement = 102                      ' EventStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A statement that declares an operator. If this operator has a body, this
        ''' statement will be the Begin of a BlockStatement with
        ''' Kind=MethodDeclarationBlock, and the body of the method will be the Body of
        ''' that BlockStatement.
        ''' </summary>
        OperatorStatement = 103                   ' OperatorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Statement that declares a property. If this property has accessors declared,
        ''' this statement will be the Begin of a BlockNode, and the accessors will be the
        ''' Body of that node. Auto properties are property declarations without a
        ''' PropertyBlock.
        ''' </summary>
        PropertyStatement = 104                   ' PropertyStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        GetAccessorStatement = 105                ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        SetAccessorStatement = 106                ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        AddHandlerAccessorStatement = 107         ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        RemoveHandlerAccessorStatement = 108      ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Get or Set accessor on a property declaration or an AddHandler,
        ''' RemoveHandler or RaiseEvent accessor on a custom event declaration. The Kind of
        ''' the node determines what kind of accessor this is. This statement is always the
        ''' Begin of a BlockNode, and the body of the accessor is the Body of that node.
        ''' </summary>
        RaiseEventAccessorStatement = 111         ' AccessorStatementSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the "Implements ..." clause on a type member, which describes which
        ''' interface members this member implements.
        ''' </summary>
        ImplementsClause = 112                    ' ImplementsClauseSyntax
        ''' <summary>
        ''' Represents the "Handles ..." clause on a method declaration that describes
        ''' which events this method handles.
        ''' </summary>
        HandlesClause = 113                       ' HandlesClauseSyntax
        ''' <summary>
        ''' Represents event container specified through special keywords "Me", "MyBase" or
        ''' "MyClass"..
        ''' </summary>
        KeywordEventContainer = 114               ' KeywordEventContainerSyntax : EventContainerSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents event container that refers to a WithEvents member.
        ''' </summary>
        WithEventsEventContainer = 115            ' WithEventsEventContainerSyntax : EventContainerSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents event container that refers to a WithEvents member's property.
        ''' </summary>
        WithEventsPropertyEventContainer = 116    ' WithEventsPropertyEventContainerSyntax : EventContainerSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single handled event in a "Handles ..." clause.
        ''' </summary>
        HandlesClauseItem = 117                   ' HandlesClauseItemSyntax
        ''' <summary>
        ''' Represents the beginning of a declaration. However, not enough syntax is
        ''' detected to classify this as a field, method, property or event. This is node
        ''' always represents a syntax error.
        ''' </summary>
        IncompleteMember = 118                    ' IncompleteMemberSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the declaration of one or more variables or constants, either as
        ''' local variables or as class/structure members. In the case of a constant, it is
        ''' represented by having "Const" in the Modifiers (although technically "Const" is
        ''' not a modifier, it is represented as one in the parse trees.)
        ''' </summary>
        FieldDeclaration = 119                    ' FieldDeclarationSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the part of a variable or constant declaration statement that
        ''' associated one or more variable names with a type.
        ''' </summary>
        VariableDeclarator = 122                  ' VariableDeclaratorSyntax
        ''' <summary>
        ''' Represents an "As {type-name}" clause that does not have an initializer or
        ''' "New". The type has optional attributes associated with it, although attributes
        ''' are not permitted in all possible places where this node occurs.
        ''' </summary>
        SimpleAsClause = 123                      ' SimpleAsClauseSyntax : AsClauseSyntax
        ''' <summary>
        ''' Represents an "As New {type-name} [arguments] [initializers]" clause in a
        ''' declaration. The type has optional attributes associated with it, although
        ''' attributes are not permitted in many places where this node occurs (they are
        ''' permitted, for example, on automatically implemented properties.)
        ''' </summary>
        AsNewClause = 124                         ' AsNewClauseSyntax : AsClauseSyntax
        ''' <summary>
        ''' Represents a "With {...} clause used to initialize a new object's members.
        ''' </summary>
        ObjectMemberInitializer = 125             ' ObjectMemberInitializerSyntax : ObjectCreationInitializerSyntax
        ''' <summary>
        ''' Represents a "From {...} clause used to initialize a new collection object's
        ''' elements.
        ''' </summary>
        ObjectCollectionInitializer = 126         ' ObjectCollectionInitializerSyntax : ObjectCreationInitializerSyntax
        ''' <summary>
        ''' Represent a field initializer in a With {...} initializer where the field name
        ''' is inferred from the initializer expression.
        ''' </summary>
        InferredFieldInitializer = 127            ' InferredFieldInitializerSyntax : FieldInitializerSyntax
        ''' <summary>
        ''' Represent a named field initializer in a With {...} initializer, such as ".x =
        ''' expr".
        ''' </summary>
        NamedFieldInitializer = 128               ' NamedFieldInitializerSyntax : FieldInitializerSyntax
        ''' <summary>
        ''' Represents an "= initializer" clause in a declaration for a variable,
        ''' parameter or automatic property.
        ''' </summary>
        EqualsValue = 129                         ' EqualsValueSyntax
        ''' <summary>
        ''' Represent a parameter to a method, property, constructor, etc.
        ''' </summary>
        Parameter = 132                           ' ParameterSyntax
        ''' <summary>
        ''' Represents an identifier with optional "?" or "()" or "(,,,)" modifiers, as
        ''' used in parameter declarations and variable declarations.
        ''' </summary>
        ModifiedIdentifier = 133                  ' ModifiedIdentifierSyntax
        ''' <summary>
        ''' Represents a modifier that describes an array type, without bounds, such as
        ''' "()" or "(,)".
        ''' </summary>
        ArrayRankSpecifier = 134                 ' ArrayRankSpecifierSyntax
        ''' <summary>
        ''' Represents a group of attributes within "&lt;" and "&gt;" brackets.
        ''' </summary>
        AttributeList = 135                      ' AttributeListSyntax
        ''' <summary>
        ''' Represents a single attribute declaration within an attribute list.
        ''' </summary>
        Attribute = 136                          ' AttributeSyntax
        ''' <summary>
        ''' Represents a single attribute declaration within an attribute list.
        ''' </summary>
        AttributeTarget = 137                    ' AttributeTargetSyntax
        ''' <summary>
        ''' Represents a file-level attribute, in which the attributes have no other
        ''' syntactic element they are attached to.
        ''' </summary>
        AttributesStatement = 138                ' AttributesStatementSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent an expression in a statement context. This may only be a invocation
        ''' or await expression in standard code but may be any expression in VB
        ''' Interactive code.
        ''' </summary>
        ExpressionStatement = 139                ' ExpressionStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent a "? expression" "Print" statement in VB Interactive code.
        ''' </summary>
        PrintStatement = 140                     ' PrintStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a While...End While statement, including the While, body and End
        ''' While.
        ''' </summary>
        WhileBlock = 141                         ' WhileBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an entire Using...End Using statement, including the Using, body and
        ''' End Using statements.
        ''' </summary>
        UsingBlock = 144                         ' UsingBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a entire SyncLock...End SyncLock block, including the SyncLock
        ''' statement, the enclosed statements, and the End SyncLock statement.
        ''' </summary>
        SyncLockBlock = 145                      ' SyncLockBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a With...End With block, include the With statement, the body of the
        ''' block and the End With statement.
        ''' </summary>
        WithBlock = 146                          ' WithBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the declaration of one or more local variables or constants.
        ''' </summary>
        LocalDeclarationStatement = 147          ' LocalDeclarationStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a label statement.
        ''' </summary>
        LabelStatement = 148                     ' LabelStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "GoTo" statement.
        ''' </summary>
        GoToStatement = 149                      ' GoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' A label for a GoTo, Resume, or On Error statement. An identifier, line number,
        ''' or next keyword.
        ''' </summary>
        IdentifierLabel = 150                    ' LabelSyntax : ExpressionSyntax
        ''' <summary>
        ''' A label for a GoTo, Resume, or On Error statement. An identifier, line number,
        ''' or next keyword.
        ''' </summary>
        NumericLabel = 151                       ' LabelSyntax : ExpressionSyntax
        ''' <summary>
        ''' A label for a GoTo, Resume, or On Error statement. An identifier, line number,
        ''' or next keyword.
        ''' </summary>
        NextLabel = 152                          ' LabelSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a "Stop" or "End" statement. The Kind can be used to determine which
        ''' kind of statement this is.
        ''' </summary>
        StopStatement = 153                      ' StopOrEndStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Stop" or "End" statement. The Kind can be used to determine which
        ''' kind of statement this is.
        ''' </summary>
        EndStatement = 156                       ' StopOrEndStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitDoStatement = 157                    ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitForStatement = 158                   ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitSubStatement = 159                   ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitFunctionStatement = 160              ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitOperatorStatement = 161              ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitPropertyStatement = 162              ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitTryStatement = 163                   ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitSelectStatement = 164                ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' An exit statement. The kind of block being exited can be found by examining the
        ''' Kind.
        ''' </summary>
        ExitWhileStatement = 165                 ' ExitStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Continue (block)" statement. THe kind of block referenced can be
        ''' determined by examining the Kind.
        ''' </summary>
        ContinueWhileStatement = 166             ' ContinueStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Continue (block)" statement. THe kind of block referenced can be
        ''' determined by examining the Kind.
        ''' </summary>
        ContinueDoStatement = 167                ' ContinueStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Continue (block)" statement. THe kind of block referenced can be
        ''' determined by examining the Kind.
        ''' </summary>
        ContinueForStatement = 168               ' ContinueStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Return" statement.
        ''' </summary>
        ReturnStatement = 169                    ' ReturnStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a line If-Then-Else statement.
        ''' </summary>
        SingleLineIfStatement = 170              ' SingleLineIfStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents part of a single line If statement, consisting of a beginning
        ''' if-statement, followed by a body of statement controlled by that beginning
        ''' statement. The Kind property returns if this is an SingleLineIf.
        ''' </summary>
        SingleLineIfPart = 171                   ' SingleLineIfPartSyntax
        ''' <summary>
        ''' Represents the Else part of an If statement, consisting of a Else statement,
        ''' followed by a body of statement controlled by that Else.
        ''' </summary>
        SingleLineElseClause = 172                 ' SingleLineElseClauseSyntax
        ''' <summary>
        ''' Represents a block If...Then...Else...EndIf Statement. The Kind property can be
        ''' used to determine if it is a block or line If.
        ''' </summary>
        MultiLineIfBlock = 173                   ' MultiLineIfBlockSyntax : ExecutableStatementSyntax : StatementSyntax

        ' IfPart = 179                           ' This node was removed.

        ''' <summary>
        ''' Represents part of an If statement, consisting of a beginning statement (If or
        ''' ElseIf), followed by a body of statement controlled by that beginning
        ''' statement. The Kind property returns if this is an If or ElseIf.
        ''' </summary>
        ElseIfBlock = 180                         ' ElseIfBlockSyntax
        ''' <summary>
        ''' Represents the Else part of an If statement, consisting of a Else statement,
        ''' followed by a body of statement controlled by that Else.
        ''' </summary>
        ElseBlock = 181                           ' ElseBlockSyntax
        ''' <summary>
        ''' Represents the If part or ElseIf part of a If...End If block (or line If). This
        ''' statement is always the Begin of a IfPart. The Kind can be examined to
        ''' determine if this is an If or an ElseIf statement.
        ''' </summary>
        IfStatement = 182                        ' IfStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the If part or ElseIf part of a If...End If block (or line If). This
        ''' statement is always the Begin of a IfPart. The Kind can be examined to
        ''' determine if this is an If or an ElseIf statement.
        ''' </summary>
        ElseIfStatement = 183                    ' IfStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the Else part of a If...End If block (or line If). This statement is
        ''' always the Begin of a ElsePart.
        ''' </summary>
        ElseStatement = 184                      ' ElseStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an entire Try...Catch...Finally...End Try statement.
        ''' </summary>
        TryBlock = 185                           ' TryBlockSyntax : ExecutableStatementSyntax : StatementSyntax

        ' TryPart = 186                            ' This node was removed.

        ''' <summary>
        ''' Represents a Catch part of an Try...Catch...Finally...End Try statement,
        ''' consisting of a Catch statement, followed by a body of statements controlled by
        ''' that Catch statement. The Kind property returns which kind of part this is.
        ''' </summary>
        CatchBlock = 187                          ' CatchBlockSyntax
        ''' <summary>
        ''' Represents the Finally part of an Try...Catch...Finally...End Try statement,
        ''' consisting of a Finally statement, followed by a body of statements controlled
        ''' by the Finally.
        ''' </summary>
        FinallyBlock = 188                        ' FinallyBlockSyntax
        ''' <summary>
        ''' Represents the Try part of a Try...Catch...Finally...End Try. This
        ''' statement is always the Begin of a TryPart.
        ''' </summary>
        TryStatement = 189                       ' TryStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the Catch part of a Try...Catch...Finally...End Try. This
        ''' statement is always the Begin of a CatchPart.
        ''' </summary>
        CatchStatement = 190                     ' CatchStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the When/Filter clause of a Catch statement
        ''' </summary>
        CatchFilterClause = 191                  ' CatchFilterClauseSyntax
        ''' <summary>
        ''' Represents the Finally part of a Try...Catch...Finally...End Try. This
        ''' statement is always the Begin of a FinallyPart.
        ''' </summary>
        FinallyStatement = 194                   ' FinallyStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the "Error" statement.
        ''' </summary>
        ErrorStatement = 195                     ' ErrorStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Goto statement.
        ''' </summary>
        OnErrorGoToZeroStatement = 196           ' OnErrorGoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Goto statement.
        ''' </summary>
        OnErrorGoToMinusOneStatement = 197       ' OnErrorGoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Goto statement.
        ''' </summary>
        OnErrorGoToLabelStatement = 198          ' OnErrorGoToStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an OnError Resume Next statement.
        ''' </summary>
        OnErrorResumeNextStatement = 199         ' OnErrorResumeNextStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Resume" statement. The Kind property can be used to determine if
        ''' this is a "Resume", "Resume Next" or "Resume label" statement.
        ''' </summary>
        ResumeStatement = 200                    ' ResumeStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Resume" statement. The Kind property can be used to determine if
        ''' this is a "Resume", "Resume Next" or "Resume label" statement.
        ''' </summary>
        ResumeLabelStatement = 201               ' ResumeStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "Resume" statement. The Kind property can be used to determine if
        ''' this is a "Resume", "Resume Next" or "Resume label" statement.
        ''' </summary>
        ResumeNextStatement = 202                ' ResumeStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Select Case block, including the Select Case that begins it, the
        ''' contains Case blocks and the End Select.
        ''' </summary>
        SelectBlock = 203                        ' SelectBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Select Case statement. This statement always occurs as the Begin
        ''' of a SelectBlock.
        ''' </summary>
        SelectStatement = 204                    ' SelectStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a case statement and its subsequent block.
        ''' </summary>
        CaseBlock = 207                          ' CaseBlockSyntax
        ''' <summary>
        ''' Represents a case statement and its subsequent block.
        ''' </summary>
        CaseElseBlock = 210                      ' CaseBlockSyntax
        ''' <summary>
        ''' Represents a Case or Case Else statement. This statement is always the Begin of
        ''' a CaseBlock. If this is a Case Else statement, the Kind=CaseElse, otherwise the
        ''' Kind=Case.
        ''' </summary>
        CaseStatement = 211                      ' CaseStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Case or Case Else statement. This statement is always the Begin of
        ''' a CaseBlock. If this is a Case Else statement, the Kind=CaseElse, otherwise the
        ''' Kind=Case.
        ''' </summary>
        CaseElseStatement = 212                  ' CaseStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The "Else" part in a Case Else statement.
        ''' </summary>
        ElseCaseClause = 213                     ' ElseCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a single value in a Case.
        ''' </summary>
        SimpleCaseClause = 214                   ' SimpleCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a range "expression To expression" in a Case.
        ''' </summary>
        RangeCaseClause = 215                    ' RangeCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseEqualsClause = 216                   ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseNotEqualsClause = 217                ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseLessThanClause = 218                 ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseLessThanOrEqualClause = 219          ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseGreaterThanOrEqualClause = 222       ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents a relation clause in a Case statement, such as "Is &gt; expression".
        ''' </summary>
        CaseGreaterThanClause = 223              ' RelationalCaseClauseSyntax : CaseClauseSyntax
        ''' <summary>
        ''' Represents the "SyncLock" statement. This statement always occurs as the Begin
        ''' of a SyncLockBlock.
        ''' </summary>
        SyncLockStatement = 226                  ' SyncLockStatementSyntax : StatementSyntax

        'DoLoopTopTestBlock = 227                'Removed

        'DoLoopBottomTestBlock = 228             'Removed

        'DoLoopForeverBlock = 229                'Removed

        'DoStatement = 230                       'Removed

        'LoopStatement = 231                     'Removed

        'WhileClause = 232                       'Removed

        'UntilClause = 233                       'Removed

        WhileStatement = 234                     ' WhileStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a For or For Each block, including the introducing statement, the
        ''' body and the "Next" (which can be omitted if a containing For has a Next with
        ''' multiple variables).
        ''' </summary>
        ForBlock = 237                           ' ForBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a For or For Each block, including the introducing statement, the
        ''' body and the "Next" (which can be omitted if a containing For has a Next with
        ''' multiple variables).
        ''' </summary>
        ForEachBlock = 238                       ' ForBlockSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The For statement that begins a For-Next block. This statement always occurs as
        ''' the Begin of a ForBlock. Most of the time, the End of that ForBlock is the
        ''' corresponding Next statement. However, multiple nested For statements are ended
        ''' by a single Next statement with multiple variables, then the inner For
        ''' statements will have End set to Nothing, and the Next statement is the End of
        ''' the outermost For statement that is being ended.
        ''' </summary>
        ForStatement = 239                       ' ForStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Step clause in a For Statement.
        ''' </summary>
        ForStepClause = 240                      ' ForStepClauseSyntax
        ''' <summary>
        ''' The For Each statement that begins a For Each-Next block. This statement always
        ''' occurs as the Begin of a ForBlock, and the body of the For Each-Next is the
        ''' Body of that ForBlock. Most of the time, the End of that ForBlock is the
        ''' corresponding Next statement. However, multiple nested For statements are ended
        ''' by a single Next statement with multiple variables, then the inner For
        ''' statements will have End set to Nothing, and the Next statement is the End of
        ''' the outermost For statement that is being ended.
        ''' </summary>
        ForEachStatement = 241                   ' ForEachStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Next statement that ends a For-Next or For Each-Next block. This statement
        ''' always occurs as the End of a ForBlock (with Kind=ForBlock or ForEachBlock),
        ''' and the body of the For-Next is the Body of that ForBlock. The Begin of that
        ''' ForBlock has the corresponding For or For Each statement.
        ''' </summary>
        NextStatement = 242                      ' NextStatementSyntax : StatementSyntax
        ''' <summary>
        ''' The Using statement that begins a Using block. This statement always occurs as
        ''' the Begin of a UsingBlock, and the body of the Using is the Body of that
        ''' UsingBlock.
        ''' </summary>
        UsingStatement = 243                     ' UsingStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a Throw statement.
        ''' </summary>
        ThrowStatement = 246                     ' ThrowStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        SimpleAssignmentStatement = 247          ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        MidAssignmentStatement = 248             ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        AddAssignmentStatement = 249             ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        SubtractAssignmentStatement = 250        ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        MultiplyAssignmentStatement = 251        ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        DivideAssignmentStatement = 252          ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        IntegerDivideAssignmentStatement = 253   ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        ExponentiateAssignmentStatement = 254    ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        LeftShiftAssignmentStatement = 255       ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        RightShiftAssignmentStatement = 258      ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a simple, compound, or Mid assignment statement. Which one can be
        ''' determined by checking the Kind.
        ''' </summary>
        ConcatenateAssignmentStatement = 259     ' AssignmentStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a left-hand side of a MidAssignment statement.
        ''' </summary>
        MidExpression = 260                      ' MidExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represent an call statement (also known as a invocation statement).
        ''' </summary>
        CallStatement = 261                      ' CallStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an AddHandler or RemoveHandler statement. The Kind property
        ''' determines which one.
        ''' </summary>
        AddHandlerStatement = 262                ' AddRemoveHandlerStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents an AddHandler or RemoveHandler statement. The Kind property
        ''' determines which one.
        ''' </summary>
        RemoveHandlerStatement = 263             ' AddRemoveHandlerStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent a RaiseEvent statement.
        ''' </summary>
        RaiseEventStatement = 264                ' RaiseEventStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a "With" statement. This statement always occurs as the
        ''' BeginStatement of a WithBlock, and the body of the With is the Body of that
        ''' WithBlock.
        ''' </summary>
        WithStatement = 265                      ' WithStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a ReDim statement.
        ''' </summary>
        ReDimStatement = 266                     ' ReDimStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a ReDim statement.
        ''' </summary>
        ReDimPreserveStatement = 267             ' ReDimStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a ReDim statement clause.
        ''' </summary>
        RedimClause = 270                        ' RedimClauseSyntax
        ''' <summary>
        ''' Represents an "Erase" statement.
        ''' </summary>
        EraseStatement = 271                     ' EraseStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        CharacterLiteralExpression = 272         ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        TrueLiteralExpression = 273              ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        FalseLiteralExpression = 274             ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        NumericLiteralExpression = 275           ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        DateLiteralExpression = 276              ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        StringLiteralExpression = 279            ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a literal. The kind of literal is determined by the Kind property:
        ''' IntegerLiteral, CharacterLiteral, BooleanLiteral, DecimalLiteral,
        ''' FloatingLiteral, DateLiteral or StringLiteral. The value of the literal can be
        ''' determined by casting the associated Token to the correct type and getting the
        ''' value from the token.
        ''' </summary>
        NothingLiteralExpression = 280           ' LiteralExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a parenthesized expression.
        ''' </summary>
        ParenthesizedExpression = 281            ' ParenthesizedExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Identifies the special instance "Me"
        ''' </summary>
        MeExpression = 282                       ' MeExpressionSyntax : InstanceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Identifies the special instance "MyBase"
        ''' </summary>
        MyBaseExpression = 283                   ' MyBaseExpressionSyntax : InstanceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Identifies the special instance "MyClass"
        ''' </summary>
        MyClassExpression = 284                  ' MyClassExpressionSyntax : InstanceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a GetType expression.
        ''' </summary>
        GetTypeExpression = 285                  ' GetTypeExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a TypeOf...Is or IsNot expression.
        ''' </summary>
        TypeOfIsExpression = 286                 ' TypeOfExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a TypeOf...Is or IsNot expression.
        ''' </summary>
        TypeOfIsNotExpression = 287              ' TypeOfExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a GetXmlNamespace expression.
        ''' </summary>
        GetXmlNamespaceExpression = 290          ' GetXmlNamespaceExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents member access (.name) or dictionary access (!name). The Kind
        ''' property determines which kind of access.
        ''' </summary>
        SimpleMemberAccessExpression = 291       ' MemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents member access (.name) or dictionary access (!name). The Kind
        ''' property determines which kind of access.
        ''' </summary>
        DictionaryAccessExpression = 292         ' MemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML member element access (node.&lt;Element&gt;), attribute
        ''' access (node.@Attribute) or descendants access (node...&lt;Descendant&gt;). The
        ''' Kind property determines which kind of access.
        ''' </summary>
        XmlElementAccessExpression = 293         ' XmlMemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML member element access (node.&lt;Element&gt;), attribute
        ''' access (node.@Attribute) or descendants access (node...&lt;Descendant&gt;). The
        ''' Kind property determines which kind of access.
        ''' </summary>
        XmlDescendantAccessExpression = 294      ' XmlMemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML member element access (node.&lt;Element&gt;), attribute
        ''' access (node.@Attribute) or descendants access (node...&lt;Descendant&gt;). The
        ''' Kind property determines which kind of access.
        ''' </summary>
        XmlAttributeAccessExpression = 295       ' XmlMemberAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an invocation expression consisting of an invocation target and an
        ''' optional argument list or an array, parameterized property or object default
        ''' property index.
        ''' </summary>
        InvocationExpression = 296               ' InvocationExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a New expression that creates a new non-array object, possibly with
        ''' a "With" or "From" clause.
        ''' </summary>
        ObjectCreationExpression = 297           ' ObjectCreationExpressionSyntax : NewExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a New expression that create an object of anonymous type.
        ''' </summary>
        AnonymousObjectCreationExpression = 298  ' AnonymousObjectCreationExpressionSyntax : NewExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an expression that creates a new array.
        ''' </summary>
        ArrayCreationExpression = 301            ' ArrayCreationExpressionSyntax : NewExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an expression that creates a new array without naming the element
        ''' type.
        ''' </summary>
        CollectionInitializer = 302              ' CollectionInitializerSyntax : ExpressionSyntax
        CTypeExpression = 303                    ' CTypeExpressionSyntax : CastExpressionSyntax : ExpressionSyntax
        DirectCastExpression = 304               ' DirectCastExpressionSyntax : CastExpressionSyntax : ExpressionSyntax
        TryCastExpression = 305                  ' TryCastExpressionSyntax : CastExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a cast to a pre-defined type using a pre-defined cast expression,
        ''' such as CInt or CLng.
        ''' </summary>
        PredefinedCastExpression = 306           ' PredefinedCastExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        AddExpression = 307                      ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        SubtractExpression = 308                 ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        MultiplyExpression = 309                 ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        DivideExpression = 310                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        IntegerDivideExpression = 311            ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ExponentiateExpression = 314             ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LeftShiftExpression = 315                ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        RightShiftExpression = 316               ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ConcatenateExpression = 317              ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ModuloExpression = 318                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        EqualsExpression = 319                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        NotEqualsExpression = 320                ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LessThanExpression = 321                 ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LessThanOrEqualExpression = 322          ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        GreaterThanOrEqualExpression = 323       ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        GreaterThanExpression = 324              ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        IsExpression = 325                       ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        IsNotExpression = 326                    ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        LikeExpression = 327                     ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        OrExpression = 328                       ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        ExclusiveOrExpression = 329              ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        AndExpression = 330                      ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        OrElseExpression = 331                   ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a binary operator. The Kind property classifies the operators into
        ''' similar kind of operators (arithmetic, relational, logical or string); the
        ''' exact operation being performed is determined by the Operator property.
        ''' </summary>
        AndAlsoExpression = 332                  ' BinaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        UnaryPlusExpression = 333                ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        UnaryMinusExpression = 334               ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        NotExpression = 335                      ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a unary operator: Plus, Negate, Not or AddressOf.
        ''' </summary>
        AddressOfExpression = 336                ' UnaryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a conditional expression, If(condition, true-expr, false-expr) or
        ''' If(expr, nothing-expr).
        ''' </summary>
        BinaryConditionalExpression = 337        ' BinaryConditionalExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a conditional expression, If(condition, true-expr, false-expr) or
        ''' If(expr, nothing-expr).
        ''' </summary>
        TernaryConditionalExpression = 338       ' TernaryConditionalExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single line lambda expression.
        ''' </summary>
        SingleLineFunctionLambdaExpression = 339 ' SingleLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single line lambda expression.
        ''' </summary>
        SingleLineSubLambdaExpression = 342      ' SingleLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a multi-line lambda expression.
        ''' </summary>
        MultiLineFunctionLambdaExpression = 343  ' MultiLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a multi-line lambda expression.
        ''' </summary>
        MultiLineSubLambdaExpression = 344       ' MultiLineLambdaExpressionSyntax : LambdaExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the header part of a lambda expression
        ''' </summary>
        SubLambdaHeader = 345                    ' LambdaHeaderSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents the header part of a lambda expression
        ''' </summary>
        FunctionLambdaHeader = 346               ' LambdaHeaderSyntax : MethodBaseSyntax : DeclarationStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represents a parenthesized argument list.
        ''' </summary>
        ArgumentList = 347                       ' ArgumentListSyntax
        ''' <summary>
        ''' Represents an omitted argument in an argument list. An omitted argument is not
        ''' considered a syntax error but a valid case when no argument is required.
        ''' </summary>
        OmittedArgument = 348                    ' OmittedArgumentSyntax : ArgumentSyntax
        ''' <summary>
        ''' Represents an argument that is just an optional argument name and an expression.
        ''' </summary>
        SimpleArgument = 349                     ' SimpleArgumentSyntax : ArgumentSyntax

        ' NamedArgument = 350                    ' Removed

        ''' <summary>
        ''' Represents a range argument, such as "0 to 5", used in array bounds. The
        ''' "Value" property represents the upper bound of the range.
        ''' </summary>
        RangeArgument = 351                      ' RangeArgumentSyntax : ArgumentSyntax
        ''' <summary>
        ''' This class represents a query expression. A query expression is composed of one
        ''' or more query operators in a row. The first query operator must be a From or
        ''' Aggregate.
        ''' </summary>
        QueryExpression = 352                    ' QueryExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Describes a single variable of the form "x [As Type] In expression" for use in
        ''' query expressions.
        ''' </summary>
        CollectionRangeVariable = 353            ' CollectionRangeVariableSyntax
        ''' <summary>
        ''' Describes a single variable of the form "[x [As Type] =] expression" for use in
        ''' query expressions.
        ''' </summary>
        ExpressionRangeVariable = 354            ' ExpressionRangeVariableSyntax
        ''' <summary>
        ''' Describes a single variable of the form "[x [As Type] =] aggregation-function"
        ''' for use in the Into clause of Aggregate or Group By or Group Join query
        ''' operators.
        ''' </summary>
        AggregationRangeVariable = 355           ' AggregationRangeVariableSyntax
        ''' <summary>
        ''' Represents the name and optional type of an expression range variable.
        ''' </summary>
        VariableNameEquals = 356                 ' VariableNameEqualsSyntax
        ''' <summary>
        ''' Represents an invocation of an Aggregation function in the aggregation range
        ''' variable declaration of a Group By, Group Join or Aggregate query operator.
        ''' </summary>
        FunctionAggregation = 357                ' FunctionAggregationSyntax : AggregationSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the use of "Group" as the aggregation function in the in the
        ''' aggregation range variable declaration of a Group By or Group Join query
        ''' operator.
        ''' </summary>
        GroupAggregation = 358                   ' GroupAggregationSyntax : AggregationSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a "From" query operator. If this is the beginning of a query, the
        ''' Source will be Nothing. Otherwise, the Source will be the part of the query to
        ''' the left of the From.
        ''' </summary>
        FromClause = 359                         ' FromClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Let" query operator.
        ''' </summary>
        LetClause = 360                          ' LetClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents an Aggregate query operator.
        ''' </summary>
        AggregateClause = 361                    ' AggregateClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Distinct" query operator.
        ''' </summary>
        DistinctClause = 362                     ' DistinctClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Where" query operator.
        ''' </summary>
        WhereClause = 363                        ' WhereClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip While" or "Take While" query operator. The Kind property
        ''' tells which.
        ''' </summary>
        SkipWhileClause = 364                    ' PartitionWhileClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip While" or "Take While" query operator. The Kind property
        ''' tells which.
        ''' </summary>
        TakeWhileClause = 365                    ' PartitionWhileClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip" or "Take" query operator. The Kind property tells which.
        ''' </summary>
        SkipClause = 366                         ' PartitionClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents a "Skip" or "Take" query operator. The Kind property tells which.
        ''' </summary>
        TakeClause = 367                         ' PartitionClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Group By" query operator.
        ''' </summary>
        GroupByClause = 368                      ' GroupByClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "expression Equals expression" condition in a Join.
        ''' </summary>
        JoinCondition = 369                      ' JoinConditionSyntax
        ''' <summary>
        ''' Represents a Join query operator.
        ''' </summary>
        SimpleJoinClause = 370                   ' SimpleJoinClauseSyntax : JoinClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Group Join" query operator.
        ''' </summary>
        GroupJoinClause = 371                    ' GroupJoinClauseSyntax : JoinClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents the "Order By" query operator.
        ''' </summary>
        OrderByClause = 372                      ' OrderByClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' An expression to order by, plus an optional ordering. The Kind indicates
        ''' whether to order in ascending or descending order.
        ''' </summary>
        AscendingOrdering = 375                  ' OrderingSyntax
        ''' <summary>
        ''' An expression to order by, plus an optional ordering. The Kind indicates
        ''' whether to order in ascending or descending order.
        ''' </summary>
        DescendingOrdering = 376                 ' OrderingSyntax
        ''' <summary>
        ''' Represents the "Select" query operator.
        ''' </summary>
        SelectClause = 377                       ' SelectClauseSyntax : QueryClauseSyntax
        ''' <summary>
        ''' Represents an XML Document literal expression.
        ''' </summary>
        XmlDocument = 378                        ' XmlDocumentSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the XML declaration prologue in an XML literal expression.
        ''' </summary>
        XmlDeclaration = 379                     ' XmlDeclarationSyntax
        ''' <summary>
        ''' Represents an XML document prologue option - version, encoding, standalone or
        ''' whitespace in an XML literal expression.
        ''' </summary>
        XmlDeclarationOption = 380               ' XmlDeclarationOptionSyntax
        ''' <summary>
        ''' Represents an XML element with content in an XML literal expression.
        ''' </summary>
        XmlElement = 381                         ' XmlElementSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents Xml text.
        ''' </summary>
        XmlText = 382                            ' XmlTextSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the start tag of an XML element of the form &lt;element&gt;.
        ''' </summary>
        XmlElementStartTag = 383                 ' XmlElementStartTagSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents the end tag of an XML element of the form &lt;/element&gt;.
        ''' </summary>
        XmlElementEndTag = 384                   ' XmlElementEndTagSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an empty XML element of the form &lt;element /&gt;
        ''' </summary>
        XmlEmptyElement = 385                    ' XmlEmptyElementSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML attribute in an XML literal expression.
        ''' </summary>
        XmlAttribute = 386                       ' XmlAttributeSyntax : BaseXmlAttributeSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a string of XML characters embedded as the content of an XML
        ''' element.
        ''' </summary>
        XmlString = 387                          ' XmlStringSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML name of the form 'name' appearing in GetXmlNamespace().
        ''' </summary>
        XmlPrefixName = 388                      ' XmlPrefixNameSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML name of the form 'name' or 'namespace:name' appearing in
        ''' source as part of an XML literal or member access expression or an XML
        ''' namespace import clause.
        ''' </summary>
        XmlName = 389                            ' XmlNameSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML name of the form &lt;xml-name&gt; appearing in source as part
        ''' of an XML literal or member access expression or an XML namespace import
        ''' clause.
        ''' </summary>
        XmlBracketedName = 390                   ' XmlBracketedNameSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML namespace prefix of the form 'prefix:' as in xml:ns="".
        ''' </summary>
        XmlPrefix = 391                          ' XmlPrefixSyntax
        ''' <summary>
        ''' Represents an XML comment of the form &lt;!-- Comment --&gt; appearing in an
        ''' XML literal expression.
        ''' </summary>
        XmlComment = 392                         ' XmlCommentSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML processing instruction of the form '&lt;? XMLProcessingTarget
        ''' XMLProcessingValue ?&gt;'.
        ''' </summary>
        XmlProcessingInstruction = 393           ' XmlProcessingInstructionSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an XML CDATA section in an XML literal expression.
        ''' </summary>
        XmlCDataSection = 394                    ' XmlCDataSectionSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an embedded expression in an XML literal e.g. '&lt;name&gt;&lt;%=
        ''' obj.Name =%&gt;&lt;/name&gt;'.
        ''' </summary>
        XmlEmbeddedExpression = 395              ' XmlEmbeddedExpressionSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an array type, such as "A() or "A(,)", without bounds specified for
        ''' the array.
        ''' </summary>
        ArrayType = 396                          ' ArrayTypeSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' A type name that represents a nullable type, such as "Integer?".
        ''' </summary>
        NullableType = 397                       ' NullableTypeSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents an occurrence of a Visual Basic built-in type such as Integer or
        ''' String in source code.
        ''' </summary>
        PredefinedType = 398                     ' PredefinedTypeSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a type name consisting of a single identifier (which might include
        ''' brackets or a type character).
        ''' </summary>
        IdentifierName = 399                     ' IdentifierNameSyntax : SimpleNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a simple type name with one or more generic arguments, such as "X(Of
        ''' Y, Z).
        ''' </summary>
        GenericName = 400                        ' GenericNameSyntax : SimpleNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a qualified type name, for example X.Y or X(Of Z).Y.
        ''' </summary>
        QualifiedName = 401                      ' QualifiedNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a name in the global namespace.
        ''' </summary>
        GlobalName = 402                         ' GlobalNameSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a parenthesized list of generic type arguments.
        ''' </summary>
        TypeArgumentList = 403                   ' TypeArgumentListSyntax
        ''' <summary>
        ''' Syntax node class that represents a value of 'cref' attribute inside
        ''' documentation comment trivia.
        ''' </summary>
        CrefReference = 404                      ' CrefReferenceSyntax
        ''' <summary>
        ''' Represents a parenthesized list of argument types for a signature inside
        ''' CrefReferenceSyntax syntax.
        ''' </summary>
        CrefSignature = 407                      ' CrefSignatureSyntax
        CrefSignaturePart = 408                  ' CrefSignaturePartSyntax
        CrefOperatorReference = 409              ' CrefOperatorReferenceSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        QualifiedCrefOperatorReference = 410     ' QualifiedCrefOperatorReferenceSyntax : NameSyntax : TypeSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represent a Yield statement.
        ''' </summary>
        YieldStatement = 411                     ' YieldStatementSyntax : ExecutableStatementSyntax : StatementSyntax
        ''' <summary>
        ''' Represent a Await expression.
        ''' </summary>
        AwaitExpression = 412                    ' AwaitExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AddHandlerKeyword = 413                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AddressOfKeyword = 414                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AliasKeyword = 415                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AndKeyword = 416                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AndAlsoKeyword = 417                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AsKeyword = 418                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        BooleanKeyword = 421                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByRefKeyword = 422                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByteKeyword = 423                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByValKeyword = 424                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CallKeyword = 425                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CaseKeyword = 426                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CatchKeyword = 427                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CBoolKeyword = 428                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CByteKeyword = 429                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CCharKeyword = 432                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CDateKeyword = 433                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CDecKeyword = 434                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CDblKeyword = 435                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CharKeyword = 436                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CIntKeyword = 437                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ClassKeyword = 438                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CLngKeyword = 439                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CObjKeyword = 440                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ConstKeyword = 441                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReferenceKeyword = 442                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ContinueKeyword = 443                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CSByteKeyword = 444                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CShortKeyword = 445                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CSngKeyword = 446                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CStrKeyword = 447                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CTypeKeyword = 448                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CUIntKeyword = 449                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CULngKeyword = 450                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CUShortKeyword = 453                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DateKeyword = 454                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DecimalKeyword = 455                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DeclareKeyword = 456                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DefaultKeyword = 457                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DelegateKeyword = 458                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DimKeyword = 459                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DirectCastKeyword = 460                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DoKeyword = 461                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DoubleKeyword = 462                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EachKeyword = 463                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ElseKeyword = 464                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ElseIfKeyword = 465                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EndKeyword = 466                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EnumKeyword = 467                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EraseKeyword = 468                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ErrorKeyword = 469                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EventKeyword = 470                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExitKeyword = 471                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FalseKeyword = 474                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FinallyKeyword = 475                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ForKeyword = 476                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FriendKeyword = 477                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FunctionKeyword = 478                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GetKeyword = 479                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GetTypeKeyword = 480                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GetXmlNamespaceKeyword = 481             ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GlobalKeyword = 482                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GoToKeyword = 483                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        HandlesKeyword = 484                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IfKeyword = 485                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ImplementsKeyword = 486                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ImportsKeyword = 487                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InKeyword = 488                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InheritsKeyword = 489                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IntegerKeyword = 490                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InterfaceKeyword = 491                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsKeyword = 492                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsNotKeyword = 495                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LetKeyword = 496                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LibKeyword = 497                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LikeKeyword = 498                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LongKeyword = 499                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        LoopKeyword = 500                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MeKeyword = 501                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ModKeyword = 502                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ModuleKeyword = 503                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MustInheritKeyword = 504                 ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MustOverrideKeyword = 505                ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MyBaseKeyword = 506                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MyClassKeyword = 507                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NamespaceKeyword = 508                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NarrowingKeyword = 509                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NextKeyword = 510                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NewKeyword = 511                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NotKeyword = 512                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NothingKeyword = 513                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NotInheritableKeyword = 516              ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NotOverridableKeyword = 517              ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ObjectKeyword = 518                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OfKeyword = 519                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OnKeyword = 520                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OperatorKeyword = 521                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OptionKeyword = 522                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OptionalKeyword = 523                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OrKeyword = 524                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OrElseKeyword = 525                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OverloadsKeyword = 526                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OverridableKeyword = 527                 ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OverridesKeyword = 528                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ParamArrayKeyword = 529                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PartialKeyword = 530                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PrivateKeyword = 531                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PropertyKeyword = 532                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ProtectedKeyword = 533                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PublicKeyword = 534                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        RaiseEventKeyword = 537                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReadOnlyKeyword = 538                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReDimKeyword = 539                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        REMKeyword = 540                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        RemoveHandlerKeyword = 541               ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ResumeKeyword = 542                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ReturnKeyword = 543                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SByteKeyword = 544                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SelectKeyword = 545                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SetKeyword = 546                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ShadowsKeyword = 547                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SharedKeyword = 548                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ShortKeyword = 549                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SingleKeyword = 550                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StaticKeyword = 551                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StepKeyword = 552                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StopKeyword = 553                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StringKeyword = 554                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StructureKeyword = 555                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SubKeyword = 558                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SyncLockKeyword = 559                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ThenKeyword = 560                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ThrowKeyword = 561                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ToKeyword = 562                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TrueKeyword = 563                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TryKeyword = 564                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TryCastKeyword = 565                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TypeOfKeyword = 566                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UIntegerKeyword = 567                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ULongKeyword = 568                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UShortKeyword = 569                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UsingKeyword = 570                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WhenKeyword = 571                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WhileKeyword = 572                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WideningKeyword = 573                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WithKeyword = 574                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WithEventsKeyword = 575                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WriteOnlyKeyword = 578                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        XorKeyword = 579                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EndIfKeyword = 580                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GosubKeyword = 581                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        VariantKeyword = 582                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WendKeyword = 583                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AggregateKeyword = 584                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AllKeyword = 585                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AnsiKeyword = 586                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AscendingKeyword = 587                   ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AssemblyKeyword = 588                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AutoKeyword = 589                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        BinaryKeyword = 590                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ByKeyword = 591                          ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CompareKeyword = 592                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        CustomKeyword = 593                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DescendingKeyword = 594                  ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DisableKeyword = 595                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        DistinctKeyword = 596                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EnableKeyword = 599                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        EqualsKeyword = 600                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExplicitKeyword = 601                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExternalSourceKeyword = 602              ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        ExternalChecksumKeyword = 603            ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        FromKeyword = 604                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        GroupKeyword = 605                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        InferKeyword = 606                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IntoKeyword = 607                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsFalseKeyword = 608                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IsTrueKeyword = 609                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        JoinKeyword = 610                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        KeyKeyword = 611                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        MidKeyword = 612                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OffKeyword = 613                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OrderKeyword = 614                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        OutKeyword = 615                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        PreserveKeyword = 616                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        RegionKeyword = 617                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        SkipKeyword = 620                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        StrictKeyword = 621                      ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TakeKeyword = 622                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TextKeyword = 623                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UnicodeKeyword = 624                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        UntilKeyword = 625                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WarningKeyword = 626                     ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        WhereKeyword = 627                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        TypeKeyword = 628                        ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        XmlKeyword = 629                         ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AsyncKeyword = 630                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        AwaitKeyword = 631                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        IteratorKeyword = 632                    ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        YieldKeyword = 633                       ' KeywordSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        ExclamationToken = 634                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AtToken = 635                            ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CommaToken = 636                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        HashToken = 637                          ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AmpersandToken = 638                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SingleQuoteToken = 641                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        OpenParenToken = 642                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CloseParenToken = 643                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        OpenBraceToken = 644                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CloseBraceToken = 645                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SemicolonToken = 646                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AsteriskToken = 647                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        PlusToken = 648                          ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        MinusToken = 649                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        DotToken = 650                           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SlashToken = 651                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        ColonToken = 652                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanToken = 653                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanEqualsToken = 654                ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanGreaterThanToken = 655           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EqualsToken = 656                        ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanToken = 657                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanEqualsToken = 658             ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        BackslashToken = 659                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CaretToken = 662                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        ColonEqualsToken = 663                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AmpersandEqualsToken = 664               ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        AsteriskEqualsToken = 665                ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        PlusEqualsToken = 666                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        MinusEqualsToken = 667                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SlashEqualsToken = 668                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        BackslashEqualsToken = 669               ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        CaretEqualsToken = 670                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanLessThanToken = 671              ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanGreaterThanToken = 672        ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanLessThanEqualsToken = 673        ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        GreaterThanGreaterThanEqualsToken = 674  ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        QuestionToken = 675                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        DoubleQuoteToken = 676                   ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        StatementTerminatorToken = 677           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EndOfFileToken = 678                     ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EmptyToken = 679                         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        SlashGreaterThanToken = 680              ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanSlashToken = 683                 ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanExclamationMinusMinusToken = 684 ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        MinusMinusGreaterThanToken = 685         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanQuestionToken = 686              ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        QuestionGreaterThanToken = 687           ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        LessThanPercentEqualsToken = 688         ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        PercentGreaterThanToken = 689            ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        BeginCDataToken = 690                    ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EndCDataToken = 691                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a single punctuation mark or operator in a VB program. Which one can
        ''' be determined from the Kind property.
        ''' </summary>
        EndOfXmlToken = 692                      ' PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a sequence of characters appearing in source with no possible
        ''' meaning in the Visual Basic language (e.g. the semicolon ';'). This token
        ''' should only appear in SkippedTokenTrivia as an artifact of parsing error
        ''' recovery.
        ''' </summary>
        BadToken = 693                           ' BadTokenSyntax : PunctuationSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an Xml NCName per Namespaces in XML 1.0
        ''' </summary>
        XmlNameToken = 694                       ' XmlNameTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents character data in Xml content also known as PCData or in an Xml
        ''' attribute value. All text is here for now even text that does not need
        ''' normalization such as comment, pi and cdata text.
        ''' </summary>
        XmlTextLiteralToken = 695                ' XmlTextTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents character data in Xml content also known as PCData or in an Xml
        ''' attribute value. All text is here for now even text that does not need
        ''' normalization such as comment, pi and cdata text.
        ''' </summary>
        XmlEntityLiteralToken = 696              ' XmlTextTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents character data in Xml content also known as PCData or in an Xml
        ''' attribute value. All text is here for now even text that does not need
        ''' normalization such as comment, pi and cdata text.
        ''' </summary>
        DocumentationCommentLineBreakToken = 697 ' XmlTextTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an identifier token. This might include brackets around the name and
        ''' a type character.
        ''' </summary>
        IdentifierToken = 700                    ' IdentifierTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an integer literal token.
        ''' </summary>
        IntegerLiteralToken = 701                ' IntegerLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an floating literal token.
        ''' </summary>
        FloatingLiteralToken = 702               ' FloatingLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents a Decimal literal token.
        ''' </summary>
        DecimalLiteralToken = 703                ' DecimalLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an Date literal token.
        ''' </summary>
        DateLiteralToken = 704                   ' DateLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an string literal token.
        ''' </summary>
        StringLiteralToken = 705                 ' StringLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents an string literal token.
        ''' </summary>
        CharacterLiteralToken = 706              ' CharacterLiteralTokenSyntax : SyntaxToken
        ''' <summary>
        ''' Represents tokens that were skipped by the parser as part of error recovery,
        ''' and thus are not part of any syntactic structure.
        ''' </summary>
        SkippedTokensTrivia = 709                ' SkippedTokensTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents a documentation comment e.g. ''' &lt;Summary&gt; appearing in source.
        ''' </summary>
        DocumentationCommentTrivia = 710         ' DocumentationCommentTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' A symbol referenced by a cref attribute (e.g. in a &lt;see&gt; or
        ''' &lt;seealso&gt; documentation comment tag). For example, the M in &lt;see
        ''' cref="M" /&gt;.
        ''' </summary>
        XmlCrefAttribute = 711                   ' XmlCrefAttributeSyntax : BaseXmlAttributeSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' A param or type param symbol referenced by a name attribute (e.g. in a
        ''' &lt;param&gt; or &lt;typeparam&gt; documentation comment tag). For example, the
        ''' M in &lt;param name="M" /&gt;.
        ''' </summary>
        XmlNameAttribute = 712                   ' XmlNameAttributeSyntax : BaseXmlAttributeSyntax : XmlNodeSyntax : ExpressionSyntax
        ''' <summary>
        ''' ExpressionSyntax node representing the object conditionally accessed.
        ''' </summary>
        ConditionalAccessExpression = 713        ' ConditionalAccessExpressionSyntax : ExpressionSyntax
        ''' <summary>
        ''' Represents true whitespace: spaces, tabs, newlines and the like.
        ''' </summary>
        WhitespaceTrivia = 729                   ' SyntaxTrivia
        ''' <summary>
        ''' Represents line breaks that are syntactically insignificant.
        ''' </summary>
        EndOfLineTrivia = 730                    ' SyntaxTrivia
        ''' <summary>
        ''' Represents colons that are syntactically insignificant.
        ''' </summary>
        ColonTrivia = 731                        ' SyntaxTrivia
        ''' <summary>
        ''' Represents a comment.
        ''' </summary>
        CommentTrivia = 732                      ' SyntaxTrivia
        ''' <summary>
        ''' Represents an explicit line continuation character at the end of a line, i.e.,
        ''' _
        ''' </summary>
        LineContinuationTrivia = 733             ' SyntaxTrivia
        ''' <summary>
        ''' Represents a ''' prefix for an XML Documentation Comment.
        ''' </summary>
        DocumentationCommentExteriorTrivia = 734 ' SyntaxTrivia
        ''' <summary>
        ''' Represents text in a false preprocessor block
        ''' </summary>
        DisabledTextTrivia = 735                 ' SyntaxTrivia
        ''' <summary>
        ''' Represents a #Const pre-processing constant declaration appearing in source.
        ''' </summary>
        ConstDirectiveTrivia = 736               ' ConstDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of an #If pre-processing directive appearing in
        ''' source.
        ''' </summary>
        IfDirectiveTrivia = 737                  ' IfDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of an #If pre-processing directive appearing in
        ''' source.
        ''' </summary>
        ElseIfDirectiveTrivia = 738              ' IfDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #Else pre-processing directive appearing in source.
        ''' </summary>
        ElseDirectiveTrivia = 739                ' ElseDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #End If pre-processing directive appearing in source.
        ''' </summary>
        EndIfDirectiveTrivia = 740               ' EndIfDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of a #Region directive appearing in source.
        ''' </summary>
        RegionDirectiveTrivia = 741              ' RegionDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #End Region directive appearing in source.
        ''' </summary>
        EndRegionDirectiveTrivia = 744           ' EndRegionDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents the beginning of a #ExternalSource pre-processing directive
        ''' appearing in source.
        ''' </summary>
        ExternalSourceDirectiveTrivia = 745      ' ExternalSourceDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #End ExternalSource pre-processing directive appearing in source.
        ''' </summary>
        EndExternalSourceDirectiveTrivia = 746   ' EndExternalSourceDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #ExternalChecksum pre-processing directive appearing in source.
        ''' </summary>
        ExternalChecksumDirectiveTrivia = 747    ' ExternalChecksumDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents #Enable Warning pre-processing directive appearing in source.
        ''' </summary>
        EnableWarningDirectiveTrivia = 748       ' EnableWarningDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents #Disable Warning pre-processing directive appearing in source.
        ''' </summary>
        DisableWarningDirectiveTrivia = 749      ' DisableWarningDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an #r directive appearing in scripts.
        ''' </summary>
        ReferenceDirectiveTrivia = 750           ' ReferenceDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax
        ''' <summary>
        ''' Represents an unrecognized pre-processing directive. This occurs when the
        ''' parser encounters a hash '#' token at the beginning of a physical line but does
        ''' recognize the text that follows as a valid Visual Basic pre-processing
        ''' directive.
        ''' </summary>
        BadDirectiveTrivia = 753                 ' BadDirectiveTriviaSyntax : DirectiveTriviaSyntax : StructuredTriviaSyntax

        ''' <summary>
        ''' Represents an alias identifier followed by an "=" token in an Imports clause.
        ''' </summary>
        ImportAliasClause = 754                   ' ImportAliasClauseSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents an identifier name followed by a ":=" token in a named argument.
        ''' </summary>
        NameColonEquals = 755

        ''' <summary>
        ''' Represents a "Do ... Loop" block.
        ''' </summary>
        SimpleDoLoopBlock = 756                 ' DoLoopBlockSyntax : ExecutableStatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Do ... Loop" block.
        ''' </summary>
        DoWhileLoopBlock = 757                 ' DoLoopBlockSyntax : ExecutableStatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Do ... Loop" block.
        ''' </summary>
        DoUntilLoopBlock = 758                 ' DoLoopBlockSyntax : ExecutableStatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Do ... Loop" block.
        ''' </summary>
        DoLoopWhileBlock = 759                 ' DoLoopBlockSyntax : ExecutableStatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Do ... Loop" block.
        ''' </summary>
        DoLoopUntilBlock = 760                 ' DoLoopBlockSyntax : ExecutableStatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a simple "Do" statement that begins a "Do ... Loop" block.
        ''' </summary>
        SimpleDoStatement = 770                 ' DoStatement : StatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Do While" statement that begins a "Do ... Loop" block.
        ''' </summary>
        DoWhileStatement = 771                 ' DoStatement : StatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Do Until" statement that begins a "Do ... Loop" block.
        ''' </summary>
        DoUntilStatement = 772                 ' DoStatement : StatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a simple "Loop" statement that end a "Do ... Loop" block.
        ''' </summary>
        SimpleLoopStatement = 773               ' LoopStatement : StatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Loop While" statement that end a "Do ... Loop" block.
        ''' </summary>
        LoopWhileStatement = 774               ' LoopStatement : StatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "Loop Until" statement that end a "Do ... Loop" block.
        ''' </summary>
        LoopUntilStatement = 775               ' LoopStatement : StatementSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a "While ..." clause of a "Do" or "Loop" statement.
        ''' </summary>
        WhileClause = 776                       ' WhileOrUntilClause : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents an "Until ..." clause of a "Do" or "Loop" statement.
        ''' </summary>
        UntilClause = 777                       ' WhileOrUntilClause : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a single keyword in a VB program. Which keyword can be determined
        ''' from the Kind property.
        ''' </summary>
        NameOfKeyword = 778                     ' KeywordSyntax : SyntaxToken

        ''' <summary>
        ''' Represents a NameOf expression.
        ''' </summary>
        NameOfExpression = 779                  ' NameOfExpressionSyntax : ExpressionSyntax

        ''' <summary>
        ''' Represents an interpolated string expression.
        ''' </summary>
        InterpolatedStringExpression = 780                              ' InterpolatedStringExpressionSyntax : ExpressionSyntax

        ''' <summary>
        ''' Represents literal text content in an interpolated string.
        ''' </summary>
        InterpolatedStringText = 781                                    ' InterpolatedStringTextSyntax : InterpolatedStringContentSyntax

        ''' <summary>
        ''' Represents an embedded expression in an interpolated string expression e.g. '{expression[,alignment][:formatString]}'.
        ''' </summary>
        Interpolation = 782                                             ' InterpolationSyntax : InterpolatedStringContentSyntax

        ''' <summary>
        ''' Represents an alignment clause ', alignment' of an interpolated string embedded expression.
        ''' </summary>
        InterpolationAlignmentClause = 783                              ' InterpolationAlignmentClauseSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a format string clause ':formatString' of an interpolated string embedded expression.
        ''' </summary>
        InterpolationFormatClause = 784                                 ' InterpolationFormatClauseSyntax : VisualBasicSyntaxNode

        ''' <summary>
        ''' Represents a '$"' token in an interpolated string expression.
        ''' </summary>
        DollarSignDoubleQuoteToken = 785                                ' DollarSignDoubleQuoteTokenSyntax : PunctuationSyntax

        ''' <summary>
        ''' Represents literal character data in interpolated string expression.
        ''' </summary>
        InterpolatedStringTextToken = 786                               ' InterpolatedStringTextTokenSyntax : SyntaxToken

        ''' <summary>
        ''' Represents the end of interpolated string when parsing.
        ''' </summary>
        EndOfInterpolatedStringToken = 787                              ' PunctuationSyntax : SyntaxToken

    End Enum

End Namespace
