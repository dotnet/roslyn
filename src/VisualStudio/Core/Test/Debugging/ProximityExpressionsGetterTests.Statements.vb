' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Linq
Imports System.Text
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.Debugging
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.UnitTests.Debugging
    Partial Public Class ProximityExpressionsGetterTests
        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_0()
            ' Line 1

            ' Imports System.ComponentModel.Composition
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 0)
            Assert.NotNull(terms)
            AssertEx.Equal({"System.ComponentModel.Composition", "System.ComponentModel", "System", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_1()
            ' Line 2

            ' Imports System.ComponentModel.Composition
            ' Imports Microsoft.VisualStudio.Text
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 43)
            Assert.NotNull(terms)
            AssertEx.Equal({"Microsoft.VisualStudio.Text", "Microsoft.VisualStudio", "Microsoft", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_2()
            ' Line 3

            ' Imports Microsoft.VisualStudio.Text
            ' Imports Microsoft.VisualStudio.Text.Editor
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 80)
            Assert.NotNull(terms)
            AssertEx.Equal({"Microsoft.VisualStudio.Text.Editor", "Microsoft.VisualStudio.Text", "Microsoft.VisualStudio", "Microsoft", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_3()
            ' Line 4

            ' Imports Microsoft.VisualStudio.Text.Editor
            ' Imports Microsoft.VisualStudio.Utilities
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 124)
            Assert.NotNull(terms)
            AssertEx.Equal({"Microsoft.VisualStudio.Utilities", "Microsoft.VisualStudio", "Microsoft", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_4()
            ' Line 5

            ' Imports Microsoft.VisualStudio.Utilities
            ' Imports Roslyn.Compilers.Internal
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 166)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Compilers.Internal", "Roslyn.Compilers", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_5()
            ' Line 6

            ' Imports Roslyn.Compilers.Internal
            ' Imports Roslyn.Compilers.VisualBasic
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 201)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Compilers.VisualBasic", "Roslyn.Compilers", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_6()
            ' Line 7

            ' Imports Roslyn.Compilers.VisualBasic
            ' Imports Roslyn.Services.Commands
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 239)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.Commands", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_7()
            ' Line 8

            ' Imports Roslyn.Services.Commands
            ' 
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 273)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.Internal.Extensions", "Roslyn.Services.Internal", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_8()
            ' Line 9

            ' 
            ' .Utilities
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 318)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.Internal.Utilities", "Roslyn.Services.Internal", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_9()
            ' Line 10

            ' .Utilities
            ' Imports Roslyn.Services.VisualBasic.Commands
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 362)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.VisualBasic.Commands", "Roslyn.Services.VisualBasic", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_10()
            ' Line 11

            ' Imports Roslyn.Services.VisualBasic.Commands
            ' Imports Roslyn.Services.VisualBasic.Utilities
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 408)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.VisualBasic.Utilities", "Roslyn.Services.VisualBasic", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_11()
            ' Line 12

            ' Imports Roslyn.Services.VisualBasic.Utilities
            ' Imports System.Text
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 455)
            Assert.NotNull(terms)
            AssertEx.Equal({"System.Text", "System", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_12()
            ' Line 13

            ' Imports System.Text
            ' Imports Roslyn.Services.Workspaces
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 476)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.Workspaces", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_13()
            ' Line 14

            ' Imports Roslyn.Services.Workspaces
            ' Imports Roslyn.Services.VisualBasic.Extensions
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 512)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.VisualBasic.Extensions", "Roslyn.Services.VisualBasic", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_14()
            ' Line 16

            ' 
            ' Namespace Roslyn.Services.VisualBasic.DocumentationComments
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 562)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.VisualBasic.DocumentationComments", "Roslyn.Services.VisualBasic", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_15()
            ' Line 16

            ' 
            ' Namespace Roslyn.Services.VisualBasic.DocumentationComments
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 562)
            Assert.NotNull(terms)
            AssertEx.Equal({"Roslyn.Services.VisualBasic.DocumentationComments", "Roslyn.Services.VisualBasic", "Roslyn.Services", "Roslyn", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_16()
            ' Line 17

            ' Namespace Roslyn.Services.VisualBasic.DocumentationComments
            '     <Export(GetType(ICommandHandler))>
            '     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 627)
            Assert.NotNull(terms)
            AssertEx.Equal({"Export", "ICommandHandler", "Name", "VisualBasicCommandHandlerNames.DocumentationComments", "VisualBasicCommandHandlerNames", "Order", "VisualBasicCommandHandlerNames.IntelliSense", "ContentType", "ContentTypeNames.VisualBasicContentType", "ContentTypeNames", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_17()
            ' Line 17

            ' Namespace Roslyn.Services.VisualBasic.DocumentationComments
            '     <Export(GetType(ICommandHandler))>
            '     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 627)
            Assert.NotNull(terms)
            AssertEx.Equal({"Export", "ICommandHandler", "Name", "VisualBasicCommandHandlerNames.DocumentationComments", "VisualBasicCommandHandlerNames", "Order", "VisualBasicCommandHandlerNames.IntelliSense", "ContentType", "ContentTypeNames.VisualBasicContentType", "ContentTypeNames", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_18()
            ' Line 22

            '     Friend NotInheritable Class DocumentationCommentCommandHandler
            '         Implements ICommandHandler(Of TypeCharCommandArgs)
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 930)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_19()
            ' Line 23

            '         Implements ICommandHandler(Of TypeCharCommandArgs)
            '         Implements ICommandHandler(Of ReturnKeyCommandArgs)
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 990)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_20()
            ' Line 24

            '         Implements ICommandHandler(Of ReturnKeyCommandArgs)
            '         Implements ICommandHandler(Of InsertCommentCommandArgs)
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1051)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_21()
            ' Line 26

            ' 
            '         Private ReadOnly _workspace As Workspace
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1118)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_22()
            ' Line 28

            ' 
            '         <ImportingConstructor()>
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1171)
            Assert.NotNull(terms)
            AssertEx.Equal({"ImportingConstructor", "workspace", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_23()
            ' Line 28

            ' 
            '         <ImportingConstructor()>
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1171)
            Assert.NotNull(terms)
            AssertEx.Equal({"ImportingConstructor", "workspace", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_24()
            ' Line 30

            '         Public Sub New(ByVal workspace As Workspace)
            '             Contract.ThrowIfNull(workspace)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1264)
            Assert.NotNull(terms)
            AssertEx.Equal({"Contract.ThrowIfNull", "Contract", "workspace", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_25()
            ' Line 32

            ' 
            '             _workspace = workspace
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1311)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace", "workspace", "Contract.ThrowIfNull", "Contract", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_26()
            ' Line 33

            '             _workspace = workspace
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1343)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace", "workspace", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_27()
            ' Line 35

            ' 
            '         Public Function GetCommandState_InsertCommandCommandHandler(ByVal args As TypeCharCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of TypeCharCommandArgs).GetCommandState
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1362)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_28()
            ' Line 35

            ' 
            '         Public Function GetCommandState_InsertCommandCommandHandler(ByVal args As TypeCharCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of TypeCharCommandArgs).GetCommandState
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1362)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_29()
            ' Line 36

            '         Public Function GetCommandState_InsertCommandCommandHandler(ByVal args As TypeCharCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of TypeCharCommandArgs).GetCommandState
            '             Return nextHandler()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1597)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "args", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_30()
            ' Line 37

            '             Return nextHandler()
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1627)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "args", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_31()
            ' Line 39

            ' 
            '         Public Sub ExecuteCommand_InsertCommandCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of InsertCommentCommandArgs).ExecuteCommand
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1651)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_32()
            ' Line 39

            ' 
            '         Public Sub ExecuteCommand_InsertCommandCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of InsertCommentCommandArgs).ExecuteCommand
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1651)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_33()
            ' Line 40

            '         Public Sub ExecuteCommand_InsertCommandCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of InsertCommentCommandArgs).ExecuteCommand
            '             If Not InsertCommentOnContainingMember(args.TextView, args.SubjectBuffer) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1858)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentOnContainingMember", "args.TextView", "args", "args.SubjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_34()
            ' Line 40

            '         Public Sub ExecuteCommand_InsertCommandCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of InsertCommentCommandArgs).ExecuteCommand
            '             If Not InsertCommentOnContainingMember(args.TextView, args.SubjectBuffer) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1858)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentOnContainingMember", "args.TextView", "args", "args.SubjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_35()
            ' Line 41

            '             If Not InsertCommentOnContainingMember(args.TextView, args.SubjectBuffer) Then
            '                 nextHandler()
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1954)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_36()
            ' Line 42

            '                 nextHandler()
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1981)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_37()
            ' Line 43

            '             End If
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 1997)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_38()
            ' Line 45

            ' 
            '         Public Function GetCommandState_TypeCharCommandHandler(ByVal args As ReturnKeyCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2016)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_39()
            ' Line 45

            ' 
            '         Public Function GetCommandState_TypeCharCommandHandler(ByVal args As ReturnKeyCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2016)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_40()
            ' Line 46

            '         Public Function GetCommandState_TypeCharCommandHandler(ByVal args As ReturnKeyCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of ReturnKeyCommandArgs).GetCommandState
            '             Return nextHandler()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2248)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "args", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_41()
            ' Line 47

            '             Return nextHandler()
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2278)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "args", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_42()
            ' Line 49

            ' 
            '         Public Sub ExecuteCommand_TypeCharCommandHandler(ByVal args As TypeCharCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of TypeCharCommandArgs).ExecuteCommand
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2302)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_43()
            ' Line 49

            ' 
            '         Public Sub ExecuteCommand_TypeCharCommandHandler(ByVal args As TypeCharCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of TypeCharCommandArgs).ExecuteCommand
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2302)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_44()
            ' Line 51

            '             ' Ensure the character is actually typed in the editor
            '             nextHandler()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2562)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "args", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_45()
            ' Line 53

            ' 
            '             If args.TypedChar = "'"c Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2591)
            Assert.NotNull(terms)
            AssertEx.Equal({"args.TypedChar", "args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_46()
            ' Line 53

            ' 
            '             If args.TypedChar = "'"c Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2591)
            Assert.NotNull(terms)
            AssertEx.Equal({"args.TypedChar", "args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_47()
            ' Line 54

            '             If args.TypedChar = "'"c Then
            '                 InsertCommentAfterTripleApostrophes(args.TextView, args.SubjectBuffer)
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2638)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentAfterTripleApostrophes", "args.TextView", "args", "args.SubjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_48()
            ' Line 55

            '                 InsertCommentAfterTripleApostrophes(args.TextView, args.SubjectBuffer)
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2722)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentAfterTripleApostrophes", "args.TextView", "args", "args.SubjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_49()
            ' Line 56

            '             End If
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2738)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_50()
            ' Line 58

            ' 
            '         Public Function GetCommandState_ReturnKeyCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of InsertCommentCommandArgs).GetCommandState
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2757)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_51()
            ' Line 58

            ' 
            '         Public Function GetCommandState_ReturnKeyCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of InsertCommentCommandArgs).GetCommandState
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2757)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_52()
            ' Line 59

            '         Public Function GetCommandState_ReturnKeyCommandHandler(ByVal args As InsertCommentCommandArgs, ByVal nextHandler As Func(Of CommandState)) As CommandState Implements ICommandHandler(Of InsertCommentCommandArgs).GetCommandState
            '             Return nextHandler()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 2998)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "args", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_53()
            ' Line 60

            '             Return nextHandler()
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 3028)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "args", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_54()
            ' Line 62

            ' 
            '         Public Sub ExecuteCommand_ReturnKeyCommandHandler(ByVal args As ReturnKeyCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of ReturnKeyCommandArgs).ExecuteCommand
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 3052)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_55()
            ' Line 62

            ' 
            '         Public Sub ExecuteCommand_ReturnKeyCommandHandler(ByVal args As ReturnKeyCommandArgs, ByVal nextHandler As Action) Implements ICommandHandler(Of ReturnKeyCommandArgs).ExecuteCommand
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 3052)
            Assert.NotNull(terms)
            AssertEx.Equal({"args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_56()
            ' Line 75

            ' 
            '             Dim subjectBufferCaretPosition = New SubjectBufferCaretPosition(args.TextView, args.SubjectBuffer)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4120)
            Assert.NotNull(terms)
            AssertEx.Equal({"SubjectBufferCaretPosition", "args.TextView", "args", "args.SubjectBuffer", "subjectBufferCaretPosition", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_57()
            ' Line 76

            '             Dim subjectBufferCaretPosition = New SubjectBufferCaretPosition(args.TextView, args.SubjectBuffer)
            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4232)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "SubjectBufferCaretPosition", "args.TextView", "args", "args.SubjectBuffer", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_58()
            ' Line 77

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4309)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_59()
            ' Line 77

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4309)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_60()
            ' Line 78

            '             If caretPosition < 0 Then
            '                 nextHandler()
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4352)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_61()
            ' Line 79

            '                 nextHandler()
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4383)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_62()
            ' Line 80

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4403)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_63()
            ' Line 82

            ' 
            '             Dim snapshot = args.SubjectBuffer.CurrentSnapshot
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4425)
            Assert.NotNull(terms)
            AssertEx.Equal({"args.SubjectBuffer.CurrentSnapshot", "args.SubjectBuffer", "args", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_64()
            ' Line 83

            '             Dim snapshot = args.SubjectBuffer.CurrentSnapshot
            '             Dim syntaxTree As SyntaxTree = Nothing
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4488)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot", "args.SubjectBuffer.CurrentSnapshot", "args.SubjectBuffer", "args", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_65()
            ' Line 84

            '             Dim syntaxTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4534)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "snapshot", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_66()
            ' Line 84

            '             Dim syntaxTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4534)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "snapshot", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_67()
            ' Line 85

            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '                 nextHandler()
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4607)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_68()
            ' Line 86

            '                 nextHandler()
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4638)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_69()
            ' Line 87

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4658)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_70()
            ' Line 90

            '             ' Note that the doc comment span starts *after* the first exterior trivia
            '             Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4767)
            Assert.NotNull(terms)
            AssertEx.Equal({"GetDocumentationComment", "tree", "caretPosition", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_71()
            ' Line 91

            '             Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            '             If documentationComment Is Nothing OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4852)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "ExteriorTriviaStartsLine", "tree", "caretPosition", "documentationComment.Span.Start", "documentationComment.Span", "GetDocumentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_72()
            ' Line 91

            '             Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            '             If documentationComment Is Nothing OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 4852)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "ExteriorTriviaStartsLine", "tree", "caretPosition", "documentationComment.Span.Start", "documentationComment.Span", "GetDocumentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_73()
            ' Line 95

            ' 
            '                 nextHandler()
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5079)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_74()
            ' Line 96

            '                 nextHandler()
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5110)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_75()
            ' Line 97

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5130)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_76()
            ' Line 99

            ' 
            '             If Not SpansSingleLine(documentationComment, snapshot) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5152)
            Assert.NotNull(terms)
            AssertEx.Equal({"SpansSingleLine", "documentationComment", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_77()
            ' Line 99

            ' 
            '             If Not SpansSingleLine(documentationComment, snapshot) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5152)
            Assert.NotNull(terms)
            AssertEx.Equal({"SpansSingleLine", "documentationComment", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_78()
            ' Line 102

            '                 ' So, it must be case #3
            '                 InsertLineBreakAndTripleApostrophesAtCaret(subjectBufferCaretPosition)
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5377)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertLineBreakAndTripleApostrophesAtCaret", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_79()
            ' Line 103

            '                 InsertLineBreakAndTripleApostrophesAtCaret(subjectBufferCaretPosition)
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5465)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertLineBreakAndTripleApostrophesAtCaret", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_80()
            ' Line 104

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5485)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_81()
            ' Line 106

            ' 
            '             Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5507)
            Assert.NotNull(terms)
            AssertEx.Equal({"GetDocumentationCommentTargetMember", "documentationComment", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_82()
            ' Line 107

            '             Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            '             If targetMember.SupportsDocumentationComments() AndAlso
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5597)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.SupportsDocumentationComments", "targetMember", "targetMember.Span.Start", "targetMember.Span", "documentationComment.Span.Start", "documentationComment.Span", "documentationComment", "GetDocumentationCommentTargetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_83()
            ' Line 107

            '             Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            '             If targetMember.SupportsDocumentationComments() AndAlso
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5597)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.SupportsDocumentationComments", "targetMember", "targetMember.Span.Start", "targetMember.Span", "documentationComment.Span.Start", "documentationComment.Span", "documentationComment", "GetDocumentationCommentTargetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_84()
            ' Line 109

            '                 targetMember.Span.Start > documentationComment.Span.Start Then
            '                 If Not IsRestOfLineWhitespace(snapshot, caretPosition) Then
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5750)
            Assert.NotNull(terms)
            AssertEx.Equal({"IsRestOfLineWhitespace", "snapshot", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_85()
            ' Line 109

            '                 targetMember.Span.Start > documentationComment.Span.Start Then
            '                 If Not IsRestOfLineWhitespace(snapshot, caretPosition) Then
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5750)
            Assert.NotNull(terms)
            AssertEx.Equal({"IsRestOfLineWhitespace", "snapshot", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_86()
            ' Line 111

            '                     ' Since there is text to the right, this must be cast #3 (e.g. /// <summary>|</summary>)
            '                     InsertLineBreakAndTripleApostrophesAtCaret(subjectBufferCaretPosition)
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 5941)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertLineBreakAndTripleApostrophesAtCaret", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_87()
            ' Line 112

            '                     InsertLineBreakAndTripleApostrophesAtCaret(subjectBufferCaretPosition)
            '                 Else
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6029)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertLineBreakAndTripleApostrophesAtCaret", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_88()
            ' Line 114

            '                     ' At this point, we know it's case #1
            '                     InsertCommentAfterTripleApostrophesCore(targetMember, tree, caretPosition, subjectBufferCaretPosition)
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6114)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentAfterTripleApostrophesCore", "targetMember", "tree", "caretPosition", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_89()
            ' Line 115

            '                     InsertCommentAfterTripleApostrophesCore(targetMember, tree, caretPosition, subjectBufferCaretPosition)
            '                 End If
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6234)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentAfterTripleApostrophesCore", "targetMember", "tree", "caretPosition", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_90()
            ' Line 117

            ' 
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6260)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_91()
            ' Line 118

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6280)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_92()
            ' Line 124

            '             ' Let the ENTER key pass through to the editor
            '             nextHandler()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6544)
            Assert.NotNull(terms)
            AssertEx.Equal({"nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_93()
            ' Line 126

            ' 
            '             Dim postSnapshot = args.SubjectBuffer.CurrentSnapshot
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6573)
            Assert.NotNull(terms)
            AssertEx.Equal({"args.SubjectBuffer.CurrentSnapshot", "args.SubjectBuffer", "args", "nextHandler", "postSnapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_94()
            ' Line 127

            '             Dim postSnapshot = args.SubjectBuffer.CurrentSnapshot
            '             Dim postTree As SyntaxTree = Nothing
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6640)
            Assert.NotNull(terms)
            AssertEx.Equal({"postSnapshot", "args.SubjectBuffer.CurrentSnapshot", "args.SubjectBuffer", "args", "postTree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_95()
            ' Line 128

            '             Dim postTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(postSnapshot, postTree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6690)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "postSnapshot", "postTree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_96()
            ' Line 128

            '             Dim postTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(postSnapshot, postTree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6690)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "postSnapshot", "postTree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_97()
            ' Line 129

            '             If Not _workspace.TryGetSyntaxTree(postSnapshot, postTree) Then
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6771)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_98()
            ' Line 130

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6791)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_99()
            ' Line 135

            ' 
            '             Dim postDocumentationComment = GetDocumentationComment(postTree, caretPosition)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 6982)
            Assert.NotNull(terms)
            AssertEx.Equal({"GetDocumentationComment", "postTree", "caretPosition", "postDocumentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_100()
            ' Line 136

            '             Dim postDocumentationComment = GetDocumentationComment(postTree, caretPosition)
            '             If postDocumentationComment Is Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7075)
            Assert.NotNull(terms)
            AssertEx.Equal({"postDocumentationComment", "GetDocumentationComment", "postTree", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_101()
            ' Line 136

            '             Dim postDocumentationComment = GetDocumentationComment(postTree, caretPosition)
            '             If postDocumentationComment Is Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7075)
            Assert.NotNull(terms)
            AssertEx.Equal({"postDocumentationComment", "GetDocumentationComment", "postTree", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_102()
            ' Line 137

            '             If postDocumentationComment Is Nothing Then
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7136)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_103()
            ' Line 138

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7156)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_104()
            ' Line 140

            ' 
            '             If Not SpansSingleLine(postDocumentationComment, postSnapshot) OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7178)
            Assert.NotNull(terms)
            AssertEx.Equal({"SpansSingleLine", "postDocumentationComment", "postSnapshot", "IsExteriorTriviaLeftOfPosition", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_105()
            ' Line 140

            ' 
            '             If Not SpansSingleLine(postDocumentationComment, postSnapshot) OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7178)
            Assert.NotNull(terms)
            AssertEx.Equal({"SpansSingleLine", "postDocumentationComment", "postSnapshot", "IsExteriorTriviaLeftOfPosition", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_106()
            ' Line 142

            '                 Not IsExteriorTriviaLeftOfPosition(postDocumentationComment, caretPosition) Then
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7363)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_107()
            ' Line 143

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7383)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_108()
            ' Line 145

            ' 
            '             Dim postTargetMember = GetDocumentationCommentTargetMember(postDocumentationComment)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7405)
            Assert.NotNull(terms)
            AssertEx.Equal({"GetDocumentationCommentTargetMember", "postDocumentationComment", "postTargetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_109()
            ' Line 146

            '             Dim postTargetMember = GetDocumentationCommentTargetMember(postDocumentationComment)
            '             If Not postTargetMember.SupportsDocumentationComments() OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7503)
            Assert.NotNull(terms)
            AssertEx.Equal({"postTargetMember.SupportsDocumentationComments", "postTargetMember", "caretPosition", "postTargetMember.Span.Start", "postTargetMember.Span", "GetDocumentationCommentTargetMember", "postDocumentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_110()
            ' Line 146

            '             Dim postTargetMember = GetDocumentationCommentTargetMember(postDocumentationComment)
            '             If Not postTargetMember.SupportsDocumentationComments() OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7503)
            Assert.NotNull(terms)
            AssertEx.Equal({"postTargetMember.SupportsDocumentationComments", "postTargetMember", "caretPosition", "postTargetMember.Span.Start", "postTargetMember.Span", "GetDocumentationCommentTargetMember", "postDocumentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_111()
            ' Line 148

            '                 caretPosition > postTargetMember.Span.Start Then
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7649)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_112()
            ' Line 149

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7669)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_113()
            ' Line 153

            ' 
            '             Dim indent = postSnapshot.GetLeadingWhitespaceOfLineAtPosition(caretPosition)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7737)
            Assert.NotNull(terms)
            AssertEx.Equal({"postSnapshot.GetLeadingWhitespaceOfLineAtPosition", "postSnapshot", "caretPosition", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_114()
            ' Line 154

            '             Dim indent = postSnapshot.GetLeadingWhitespaceOfLineAtPosition(caretPosition)
            '             Dim replaceSpan = Span.FromBounds(caretPosition, postTargetMember.GetFirstToken().Span.Start)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7828)
            Assert.NotNull(terms)
            AssertEx.Equal({"Span.FromBounds", "Span", "caretPosition", "postTargetMember.GetFirstToken().Span.Start", "postTargetMember.GetFirstToken().Span", "postTargetMember.GetFirstToken", "postTargetMember", "indent", "postSnapshot.GetLeadingWhitespaceOfLineAtPosition", "postSnapshot", "replaceSpan", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_115()
            ' Line 156

            ' 
            '             Dim pair = GenerateDocumentationCommentText(
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 7937)
            Assert.NotNull(terms)
            AssertEx.Equal({"GenerateDocumentationCommentText", "postTargetMember", "postTree", "indent", "replaceSpan", "Span.FromBounds", "Span", "caretPosition", "postTargetMember.GetFirstToken().Span.Start", "postTargetMember.GetFirstToken().Span", "postTargetMember.GetFirstToken", "pair", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_116()
            ' Line 164

            ' 
            '             ReplaceWithCommentText(
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 8298)
            Assert.NotNull(terms)
            AssertEx.Equal({"ReplaceWithCommentText", "replaceSpan", "pair.Item1", "pair", "pair.Item2", "subjectBufferCaretPosition", "GenerateDocumentationCommentText", "postTargetMember", "postTree", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_117()
            ' Line 169

            '                 subjectBufferCaretPosition:=subjectBufferCaretPosition)
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 8518)
            Assert.NotNull(terms)
            AssertEx.Equal({"ReplaceWithCommentText", "replaceSpan", "pair.Item1", "pair", "pair.Item2", "subjectBufferCaretPosition", "args", "nextHandler", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_118()
            ' Line 171

            ' 
            '         Private Function InsertCommentAfterTripleApostrophes(ByVal textView As ITextView, ByVal subjectBuffer As ITextBuffer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 8537)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_119()
            ' Line 171

            ' 
            '         Private Function InsertCommentAfterTripleApostrophes(ByVal textView As ITextView, ByVal subjectBuffer As ITextBuffer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 8537)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_120()
            ' Line 178

            ' 
            '             Dim subjectBufferCaretPosition As New SubjectBufferCaretPosition(textView, subjectBuffer)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9103)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_121()
            ' Line 180

            ' 
            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9208)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "textView", "subjectBuffer", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_122()
            ' Line 181

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9285)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_123()
            ' Line 181

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9285)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_124()
            ' Line 182

            '             If caretPosition < 0 Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9328)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_125()
            ' Line 183

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9354)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_126()
            ' Line 185

            ' 
            '             Dim snapshot = subjectBuffer.CurrentSnapshot
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9376)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBuffer.CurrentSnapshot", "subjectBuffer", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_127()
            ' Line 187

            ' 
            '             If Not IsRestOfLineWhitespace(snapshot, caretPosition) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9436)
            Assert.NotNull(terms)
            AssertEx.Equal({"IsRestOfLineWhitespace", "snapshot", "caretPosition", "subjectBuffer.CurrentSnapshot", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_128()
            ' Line 187

            ' 
            '             If Not IsRestOfLineWhitespace(snapshot, caretPosition) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9436)
            Assert.NotNull(terms)
            AssertEx.Equal({"IsRestOfLineWhitespace", "snapshot", "caretPosition", "subjectBuffer.CurrentSnapshot", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_129()
            ' Line 188

            '             If Not IsRestOfLineWhitespace(snapshot, caretPosition) Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9513)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_130()
            ' Line 189

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9539)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_131()
            ' Line 191

            ' 
            '             Dim syntaxTree As SyntaxTree = Nothing
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9561)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_132()
            ' Line 192

            '             Dim syntaxTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9607)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "snapshot", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_133()
            ' Line 192

            '             Dim syntaxTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9607)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "snapshot", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_134()
            ' Line 193

            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9680)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_135()
            ' Line 194

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9706)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_136()
            ' Line 196

            ' 
            '             Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9728)
            Assert.NotNull(terms)
            AssertEx.Equal({"GetDocumentationComment", "tree", "caretPosition", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_137()
            ' Line 197

            '             Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            '             If documentationComment Is Nothing OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9813)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "IsExteriorTriviaLeftOfPosition", "caretPosition", "SpansSingleLine", "snapshot", "GetDocumentationComment", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_138()
            ' Line 197

            '             Dim documentationComment = GetDocumentationComment(tree, caretPosition)
            '             If documentationComment Is Nothing OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 9813)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "IsExteriorTriviaLeftOfPosition", "caretPosition", "SpansSingleLine", "snapshot", "GetDocumentationComment", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_139()
            ' Line 201

            ' 
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10044)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_140()
            ' Line 202

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10070)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_141()
            ' Line 204

            ' 
            '             Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10092)
            Assert.NotNull(terms)
            AssertEx.Equal({"GetDocumentationCommentTargetMember", "documentationComment", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_142()
            ' Line 205

            '             Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            '             If Not targetMember.SupportsDocumentationComments() Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10182)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.SupportsDocumentationComments", "targetMember", "GetDocumentationCommentTargetMember", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_143()
            ' Line 205

            '             Dim targetMember = GetDocumentationCommentTargetMember(documentationComment)
            '             If Not targetMember.SupportsDocumentationComments() Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10182)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.SupportsDocumentationComments", "targetMember", "GetDocumentationCommentTargetMember", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_144()
            ' Line 206

            '             If Not targetMember.SupportsDocumentationComments() Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10256)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_145()
            ' Line 207

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10282)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_146()
            ' Line 209

            ' 
            '             If caretPosition > targetMember.Span.Start Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10304)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "targetMember.Span.Start", "targetMember.Span", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_147()
            ' Line 209

            ' 
            '             If caretPosition > targetMember.Span.Start Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10304)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "targetMember.Span.Start", "targetMember.Span", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_148()
            ' Line 210

            '             If caretPosition > targetMember.Span.Start Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10369)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_149()
            ' Line 211

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10395)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_150()
            ' Line 213

            ' 
            '             InsertCommentAfterTripleApostrophesCore(targetMember, tree, caretPosition, subjectBufferCaretPosition)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10417)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentAfterTripleApostrophesCore", "targetMember", "tree", "caretPosition", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_151()
            ' Line 215

            ' 
            '             Return True
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10535)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentAfterTripleApostrophesCore", "targetMember", "tree", "caretPosition", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_152()
            ' Line 216

            '             Return True
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10556)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_153()
            ' Line 218

            ' 
            '         Private Sub InsertCommentAfterTripleApostrophesCore(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10580)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember", "tree", "position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_154()
            ' Line 218

            ' 
            '         Private Sub InsertCommentAfterTripleApostrophesCore(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10580)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember", "tree", "position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_155()
            ' Line 224

            ' 
            '             Dim indent = tree.Text.GetLeadingWhitespaceOfLineAtPosition(position)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10856)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree.Text.GetLeadingWhitespaceOfLineAtPosition", "tree.Text", "tree", "position", "indent", "targetMember", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_156()
            ' Line 226

            ' 
            '             Dim pair = GenerateDocumentationCommentText(
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 10941)
            Assert.NotNull(terms)
            AssertEx.Equal({"GenerateDocumentationCommentText", "targetMember", "tree", "indent", "tree.Text.GetLeadingWhitespaceOfLineAtPosition", "tree.Text", "position", "pair", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_157()
            ' Line 231

            ' 
            '             InsertCommentText(
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11143)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentText", "position", "pair.Item1", "pair", "pair.Item2", "subjectBufferCaretPosition", "GenerateDocumentationCommentText", "targetMember", "tree", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_158()
            ' Line 236

            '                 subjectBufferCaretPosition:=subjectBufferCaretPosition)
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11365)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentText", "position", "pair.Item1", "pair", "pair.Item2", "subjectBufferCaretPosition", "targetMember", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_159()
            ' Line 238

            ' 
            '         Private Function InsertCommentOnContainingMember(ByVal textView As ITextView, ByVal subjectBuffer As ITextBuffer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11384)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_160()
            ' Line 238

            ' 
            '         Private Function InsertCommentOnContainingMember(ByVal textView As ITextView, ByVal subjectBuffer As ITextBuffer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11384)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_161()
            ' Line 243

            ' 
            '             Dim subjectBufferCaretPosition As New SubjectBufferCaretPosition(textView, subjectBuffer)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11769)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_162()
            ' Line 245

            ' 
            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11874)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "textView", "subjectBuffer", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_163()
            ' Line 246

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11951)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_164()
            ' Line 246

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11951)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_165()
            ' Line 247

            '             If caretPosition < 0 Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 11994)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_166()
            ' Line 248

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12020)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_167()
            ' Line 250

            ' 
            '             Dim snapshot = subjectBuffer.CurrentSnapshot
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12042)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBuffer.CurrentSnapshot", "subjectBuffer", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_168()
            ' Line 251

            '             Dim snapshot = subjectBuffer.CurrentSnapshot
            '             Dim syntaxTree As SyntaxTree = Nothing
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12100)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot", "subjectBuffer.CurrentSnapshot", "subjectBuffer", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_169()
            ' Line 252

            '             Dim syntaxTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12146)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "snapshot", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_170()
            ' Line 252

            '             Dim syntaxTree As SyntaxTree = Nothing
            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12146)
            Assert.NotNull(terms)
            AssertEx.Equal({"_workspace.TryGetSyntaxTree", "_workspace", "snapshot", "tree", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_171()
            ' Line 253

            '             If Not _workspace.TryGetSyntaxTree(snapshot, tree) Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12219)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_172()
            ' Line 254

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12245)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_173()
            ' Line 256

            ' 
            '             Dim token = tree.Root.FindToken(caretPosition)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12267)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree.Root.FindToken", "tree.Root", "tree", "caretPosition", "token", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_174()
            ' Line 258

            ' 
            '             Dim targetMember = token.GetContainingMember()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12329)
            Assert.NotNull(terms)
            AssertEx.Equal({"token.GetContainingMember", "token", "tree.Root.FindToken", "tree.Root", "tree", "caretPosition", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_175()
            ' Line 259

            '             Dim targetMember = token.GetContainingMember()
            '             If Not targetMember.SupportsDocumentationComments() OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12389)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.SupportsDocumentationComments", "targetMember", "targetMember.Span.Start", "targetMember.Span", "caretPosition", "targetMember.Span.End", "token.GetContainingMember", "token", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_176()
            ' Line 259

            '             Dim targetMember = token.GetContainingMember()
            '             If Not targetMember.SupportsDocumentationComments() OrElse
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12389)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.SupportsDocumentationComments", "targetMember", "targetMember.Span.Start", "targetMember.Span", "caretPosition", "targetMember.Span.End", "token.GetContainingMember", "token", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_177()
            ' Line 263

            ' 
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12591)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_178()
            ' Line 264

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12617)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_179()
            ' Line 266

            ' 
            '             If targetMember.HasDocumentationComment() Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12639)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.HasDocumentationComment", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_180()
            ' Line 266

            ' 
            '             If targetMember.HasDocumentationComment() Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12639)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.HasDocumentationComment", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_181()
            ' Line 267

            '             If targetMember.HasDocumentationComment() Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12703)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_182()
            ' Line 268

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12729)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_183()
            ' Line 270

            ' 
            '             Dim indent = tree.Text.GetLeadingWhitespaceOfLineAtPosition(targetMember.GetFirstToken().Span.Start)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12751)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree.Text.GetLeadingWhitespaceOfLineAtPosition", "tree.Text", "tree", "targetMember.GetFirstToken().Span.Start", "targetMember.GetFirstToken().Span", "targetMember.GetFirstToken", "targetMember", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_184()
            ' Line 272

            ' 
            '             Dim pair = GenerateDocumentationCommentText(
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 12867)
            Assert.NotNull(terms)
            AssertEx.Equal({"GenerateDocumentationCommentText", "targetMember", "tree", "indent", "tree.Text.GetLeadingWhitespaceOfLineAtPosition", "tree.Text", "targetMember.GetFirstToken().Span.Start", "targetMember.GetFirstToken().Span", "targetMember.GetFirstToken", "pair", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_185()
            ' Line 277

            ' 
            '             InsertCommentText(
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13067)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentText", "targetMember.Span.Start", "targetMember.Span", "targetMember", "pair.Item1", "pair", "pair.Item2", "subjectBufferCaretPosition", "GenerateDocumentationCommentText", "tree", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_186()
            ' Line 283

            ' 
            '             Return True
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13310)
            Assert.NotNull(terms)
            AssertEx.Equal({"InsertCommentText", "targetMember.Span.Start", "targetMember.Span", "targetMember", "pair.Item1", "pair", "pair.Item2", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_187()
            ' Line 284

            '             Return True
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13331)
            Assert.NotNull(terms)
            AssertEx.Equal({"textView", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_188()
            ' Line 286

            ' 
            '         Private Sub InsertLineBreakAndTripleApostrophesAtCaret(ByVal subjectBufferCaretPosition As SubjectBufferCaretPosition)
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13355)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_189()
            ' Line 286

            ' 
            '         Private Sub InsertLineBreakAndTripleApostrophesAtCaret(ByVal subjectBufferCaretPosition As SubjectBufferCaretPosition)
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13355)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_190()
            ' Line 287

            '         Private Sub InsertLineBreakAndTripleApostrophesAtCaret(ByVal subjectBufferCaretPosition As SubjectBufferCaretPosition)
            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13487)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "caretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_191()
            ' Line 288

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13564)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_192()
            ' Line 288

            '             Dim caretPosition = If(subjectBufferCaretPosition.Position, -1)
            '             If caretPosition < 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13564)
            Assert.NotNull(terms)
            AssertEx.Equal({"caretPosition", "subjectBufferCaretPosition.Position", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_193()
            ' Line 289

            '             If caretPosition < 0 Then
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13607)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_194()
            ' Line 290

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13627)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_195()
            ' Line 292

            ' 
            '             Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13649)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer", "subjectBufferCaretPosition.SubjectBufferSnapshot", "subjectBufferCaretPosition", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_196()
            ' Line 294

            ' 
            '             Dim snapshot = subjectBuffer.CurrentSnapshot
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13744)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBuffer.CurrentSnapshot", "subjectBuffer", "subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer", "subjectBufferCaretPosition.SubjectBufferSnapshot", "subjectBufferCaretPosition", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_197()
            ' Line 295

            '             Dim snapshot = subjectBuffer.CurrentSnapshot
            '             Dim lineNumber = snapshot.GetLineNumberFromPosition(caretPosition)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13802)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot.GetLineNumberFromPosition", "snapshot", "caretPosition", "subjectBuffer.CurrentSnapshot", "subjectBuffer", "lineNumber", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_198()
            ' Line 297

            ' 
            '             Dim indent = String.Empty
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13884)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.Empty", "lineNumber", "snapshot.GetLineNumberFromPosition", "snapshot", "caretPosition", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_199()
            ' Line 298

            '             Dim indent = String.Empty
            '             If lineNumber >= 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13923)
            Assert.NotNull(terms)
            AssertEx.Equal({"lineNumber", "indent", "String.Empty", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_200()
            ' Line 298

            '             Dim indent = String.Empty
            '             If lineNumber >= 0 Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13923)
            Assert.NotNull(terms)
            AssertEx.Equal({"lineNumber", "indent", "String.Empty", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_201()
            ' Line 299

            '             If lineNumber >= 0 Then
            '                 Dim line = snapshot.GetLineFromLineNumber(lineNumber)
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 13964)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot.GetLineFromLineNumber", "snapshot", "lineNumber", "line", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_202()
            ' Line 300

            '                 Dim line = snapshot.GetLineFromLineNumber(lineNumber)
            '                 Dim lineText = line.GetText()
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14035)
            Assert.NotNull(terms)
            AssertEx.Equal({"line.GetText", "line", "snapshot.GetLineFromLineNumber", "snapshot", "lineNumber", "lineText", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_203()
            ' Line 301

            '                 Dim lineText = line.GetText()
            '                 Dim slashesIndex = lineText.IndexOf("'''")
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14082)
            Assert.NotNull(terms)
            AssertEx.Equal({"lineText.IndexOf", "lineText", "line.GetText", "line", "slashesIndex", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_204()
            ' Line 302

            '                 Dim slashesIndex = lineText.IndexOf("'''")
            '                 If slashesIndex >= 0 Then
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14142)
            Assert.NotNull(terms)
            AssertEx.Equal({"slashesIndex", "lineText.IndexOf", "lineText", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_205()
            ' Line 302

            '                 Dim slashesIndex = lineText.IndexOf("'''")
            '                 If slashesIndex >= 0 Then
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14142)
            Assert.NotNull(terms)
            AssertEx.Equal({"slashesIndex", "lineText.IndexOf", "lineText", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_206()
            ' Line 303

            '                 If slashesIndex >= 0 Then
            '                     indent = New String(" "c, slashesIndex)
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14189)
            Assert.NotNull(terms)
            AssertEx.Equal({"indent", "slashesIndex", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_207()
            ' Line 304

            '                     indent = New String(" "c, slashesIndex)
            '                 End If
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14246)
            Assert.NotNull(terms)
            AssertEx.Equal({"indent", "slashesIndex", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_208()
            ' Line 305

            '                 End If
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14266)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_209()
            ' Line 307

            ' 
            '             Dim text = vbCrLf & indent & "''' "
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14288)
            Assert.NotNull(terms)
            AssertEx.Equal({"vbCrLf", "indent", "text", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_210()
            ' Line 309

            ' 
            '             Dim newSnapshot = subjectBuffer.Insert(caretPosition, text)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14339)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBuffer.Insert", "subjectBuffer", "caretPosition", "text", "vbCrLf", "indent", "newSnapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_211()
            ' Line 310

            '             Dim newSnapshot = subjectBuffer.Insert(caretPosition, text)
            '             Dim caretPoint = New SnapshotPoint(newSnapshot, caretPosition + text.Length)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14412)
            Assert.NotNull(terms)
            AssertEx.Equal({"SnapshotPoint", "newSnapshot", "caretPosition", "text.Length", "text", "subjectBuffer.Insert", "subjectBuffer", "caretPoint", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_212()
            ' Line 312

            ' 
            '             subjectBufferCaretPosition.TryMoveTo(caretPoint)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14504)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.TryMoveTo", "subjectBufferCaretPosition", "caretPoint", "SnapshotPoint", "newSnapshot", "caretPosition", "text.Length", "text", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_213()
            ' Line 313

            '             subjectBufferCaretPosition.TryMoveTo(caretPoint)
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14562)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.TryMoveTo", "subjectBufferCaretPosition", "caretPoint", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_214()
            ' Line 315

            ' 
            '         Private Sub InsertCommentText(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14581)
            Assert.NotNull(terms)
            AssertEx.Equal({"position", "commentText", "caretOffset", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_215()
            ' Line 315

            ' 
            '         Private Sub InsertCommentText(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14581)
            Assert.NotNull(terms)
            AssertEx.Equal({"position", "commentText", "caretOffset", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_216()
            ' Line 321

            ' 
            '             If String.IsNullOrWhiteSpace(commentText) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14829)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "commentText", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_217()
            ' Line 321

            ' 
            '             If String.IsNullOrWhiteSpace(commentText) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14829)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "commentText", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_218()
            ' Line 322

            '             If String.IsNullOrWhiteSpace(commentText) Then
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14893)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_219()
            ' Line 323

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14913)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_220()
            ' Line 325

            ' 
            '             Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 14935)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer", "subjectBufferCaretPosition.SubjectBufferSnapshot", "subjectBufferCaretPosition", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_221()
            ' Line 326

            '             Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer
            '             Dim newSnapshot = subjectBuffer.Insert(position, commentText)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15028)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBuffer.Insert", "subjectBuffer", "position", "commentText", "subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer", "subjectBufferCaretPosition.SubjectBufferSnapshot", "subjectBufferCaretPosition", "newSnapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_222()
            ' Line 327

            '             Dim newSnapshot = subjectBuffer.Insert(position, commentText)
            '             Dim caretPoint = New SnapshotPoint(newSnapshot, position + caretOffset)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15103)
            Assert.NotNull(terms)
            AssertEx.Equal({"SnapshotPoint", "newSnapshot", "position", "caretOffset", "subjectBuffer.Insert", "subjectBuffer", "commentText", "caretPoint", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_223()
            ' Line 328

            '             Dim caretPoint = New SnapshotPoint(newSnapshot, position + caretOffset)
            '             subjectBufferCaretPosition.TryMoveTo(caretPoint)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15188)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.TryMoveTo", "subjectBufferCaretPosition", "caretPoint", "SnapshotPoint", "newSnapshot", "position", "caretOffset", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_224()
            ' Line 329

            '             subjectBufferCaretPosition.TryMoveTo(caretPoint)
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15246)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.TryMoveTo", "subjectBufferCaretPosition", "caretPoint", "position", "commentText", "caretOffset", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_225()
            ' Line 331

            ' 
            '         Private Sub ReplaceWithCommentText(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15265)
            Assert.NotNull(terms)
            AssertEx.Equal({"replaceSpan", "commentText", "caretOffset", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_226()
            ' Line 331

            ' 
            '         Private Sub ReplaceWithCommentText(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15265)
            Assert.NotNull(terms)
            AssertEx.Equal({"replaceSpan", "commentText", "caretOffset", "subjectBufferCaretPosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_227()
            ' Line 337

            ' 
            '             If String.IsNullOrWhiteSpace(commentText) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15518)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "commentText", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_228()
            ' Line 337

            ' 
            '             If String.IsNullOrWhiteSpace(commentText) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15518)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "commentText", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_229()
            ' Line 338

            '             If String.IsNullOrWhiteSpace(commentText) Then
            '                 Return
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15582)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_230()
            ' Line 339

            '                 Return
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15602)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_231()
            ' Line 341

            ' 
            '             Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15624)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer", "subjectBufferCaretPosition.SubjectBufferSnapshot", "subjectBufferCaretPosition", "subjectBuffer", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_232()
            ' Line 342

            '             Dim subjectBuffer = subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer
            '             Dim newSnapshot = subjectBuffer.Replace(replaceSpan, commentText)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15717)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBuffer.Replace", "subjectBuffer", "replaceSpan", "commentText", "subjectBufferCaretPosition.SubjectBufferSnapshot.TextBuffer", "subjectBufferCaretPosition.SubjectBufferSnapshot", "subjectBufferCaretPosition", "newSnapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_233()
            ' Line 343

            '             Dim newSnapshot = subjectBuffer.Replace(replaceSpan, commentText)
            '             Dim caretPoint = New SnapshotPoint(newSnapshot, replaceSpan.Start + caretOffset)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15796)
            Assert.NotNull(terms)
            AssertEx.Equal({"SnapshotPoint", "newSnapshot", "replaceSpan.Start", "replaceSpan", "caretOffset", "subjectBuffer.Replace", "subjectBuffer", "commentText", "caretPoint", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_234()
            ' Line 344

            '             Dim caretPoint = New SnapshotPoint(newSnapshot, replaceSpan.Start + caretOffset)
            '             subjectBufferCaretPosition.TryMoveTo(caretPoint)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15890)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.TryMoveTo", "subjectBufferCaretPosition", "caretPoint", "SnapshotPoint", "newSnapshot", "replaceSpan.Start", "replaceSpan", "caretOffset", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_235()
            ' Line 345

            '             subjectBufferCaretPosition.TryMoveTo(caretPoint)
            '         End Sub
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15948)
            Assert.NotNull(terms)
            AssertEx.Equal({"subjectBufferCaretPosition.TryMoveTo", "subjectBufferCaretPosition", "caretPoint", "replaceSpan", "commentText", "caretOffset", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_236()
            ' Line 347

            ' 
            '         Private Function IsRestOfLineWhitespace(ByVal snapshot As ITextSnapshot, ByVal position As Integer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15967)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_237()
            ' Line 347

            ' 
            '         Private Function IsRestOfLineWhitespace(ByVal snapshot As ITextSnapshot, ByVal position As Integer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 15967)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_238()
            ' Line 348

            '         Private Function IsRestOfLineWhitespace(ByVal snapshot As ITextSnapshot, ByVal position As Integer) As Boolean
            '             Dim line = snapshot.GetLineFromPosition(position)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16091)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot.GetLineFromPosition", "snapshot", "position", "line", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_239()
            ' Line 349

            '             Dim line = snapshot.GetLineFromPosition(position)
            '             Dim lineTextToEnd = line.GetText().Substring(position - line.Start.Position)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16154)
            Assert.NotNull(terms)
            AssertEx.Equal({"line.GetText().Substring", "line.GetText", "line", "position", "line.Start.Position", "line.Start", "snapshot.GetLineFromPosition", "snapshot", "lineTextToEnd", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_240()
            ' Line 350

            '             Dim lineTextToEnd = line.GetText().Substring(position - line.Start.Position)
            '             Return String.IsNullOrWhiteSpace(lineTextToEnd)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16244)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "lineTextToEnd", "line.GetText().Substring", "line.GetText", "line", "position", "line.Start.Position", "line.Start", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_241()
            ' Line 351

            '             Return String.IsNullOrWhiteSpace(lineTextToEnd)
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16301)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "lineTextToEnd", "snapshot", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_242()
            ' Line 353

            ' 
            '         Private Function GetDocumentationComment(ByVal syntaxTree As SyntaxTree, ByVal position As Integer) As DocumentationCommentSyntax
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16325)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_243()
            ' Line 353

            ' 
            '         Private Function GetDocumentationComment(ByVal syntaxTree As SyntaxTree, ByVal position As Integer) As DocumentationCommentSyntax
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16325)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_244()
            ' Line 354

            '         Private Function GetDocumentationComment(ByVal syntaxTree As SyntaxTree, ByVal position As Integer) As DocumentationCommentSyntax
            '             Dim trivia = tree.Root.FindTrivia(position)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16462)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree.Root.FindTrivia", "tree.Root", "tree", "position", "trivia", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_245()
            ' Line 355

            '             Dim trivia = tree.Root.FindTrivia(position)
            '             If (trivia.Kind = SyntaxKind.DocumentationComment) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16519)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.Kind", "trivia", "SyntaxKind.DocumentationComment", "SyntaxKind", "tree.Root.FindTrivia", "tree.Root", "tree", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_246()
            ' Line 355

            '             Dim trivia = tree.Root.FindTrivia(position)
            '             If (trivia.Kind = SyntaxKind.DocumentationComment) Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16519)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.Kind", "trivia", "SyntaxKind.DocumentationComment", "SyntaxKind", "tree.Root.FindTrivia", "tree.Root", "tree", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_247()
            ' Line 356

            '             If (trivia.Kind = SyntaxKind.DocumentationComment) Then
            '                 Return DirectCast(trivia.GetStructure(), DocumentationCommentSyntax)
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16592)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.GetStructure", "trivia", "DocumentationCommentSyntax", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_248()
            ' Line 357

            '                 Return DirectCast(trivia.GetStructure(), DocumentationCommentSyntax)
            '             Else
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16674)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.GetStructure", "trivia", "DocumentationCommentSyntax", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_249()
            ' Line 358

            '             Else
            '                 Return Nothing
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16696)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_250()
            ' Line 359

            '                 Return Nothing
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16724)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_251()
            ' Line 360

            '             End If
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16740)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_252()
            ' Line 362

            ' 
            '         Private Function ExteriorTriviaStartsLine(ByVal syntaxTree As SyntaxTree, ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16764)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree", "documentationComment", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_253()
            ' Line 362

            ' 
            '         Private Function ExteriorTriviaStartsLine(ByVal syntaxTree As SyntaxTree, ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16764)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree", "documentationComment", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_254()
            ' Line 363

            '         Private Function ExteriorTriviaStartsLine(ByVal syntaxTree As SyntaxTree, ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            '             Dim line = tree.Text.GetLineFromPosition(position)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 16941)
            Assert.NotNull(terms)
            AssertEx.Equal({"tree.Text.GetLineFromPosition", "tree.Text", "tree", "position", "line", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_255()
            ' Line 364

            '             Dim line = tree.Text.GetLineFromPosition(position)
            '             Dim firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17005)
            Assert.NotNull(terms)
            AssertEx.Equal({"line.GetFirstNonWhitespacePosition", "line", "tree.Text.GetLineFromPosition", "tree.Text", "tree", "position", "firstNonWhitespacePosition", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_256()
            ' Line 365

            '             Dim firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition()
            '             If Not firstNonWhitespacePosition.HasValue Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17088)
            Assert.NotNull(terms)
            AssertEx.Equal({"firstNonWhitespacePosition.HasValue", "firstNonWhitespacePosition", "line.GetFirstNonWhitespacePosition", "line", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_257()
            ' Line 365

            '             Dim firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition()
            '             If Not firstNonWhitespacePosition.HasValue Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17088)
            Assert.NotNull(terms)
            AssertEx.Equal({"firstNonWhitespacePosition.HasValue", "firstNonWhitespacePosition", "line.GetFirstNonWhitespacePosition", "line", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_258()
            ' Line 366

            '             If Not firstNonWhitespacePosition.HasValue Then
            '                 Return False
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17153)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_259()
            ' Line 367

            '                 Return False
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17179)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_260()
            ' Line 369

            ' 
            '             Dim token = documentationComment.FindToken(firstNonWhitespacePosition.Value)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17201)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment.FindToken", "documentationComment", "firstNonWhitespacePosition.Value", "firstNonWhitespacePosition", "token", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_261()
            ' Line 370

            '             Dim token = documentationComment.FindToken(firstNonWhitespacePosition.Value)
            '             Dim trivia = token.LeadingTrivia.FirstOrDefault()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17291)
            Assert.NotNull(terms)
            AssertEx.Equal({"token.LeadingTrivia.FirstOrDefault", "token.LeadingTrivia", "token", "documentationComment.FindToken", "documentationComment", "firstNonWhitespacePosition.Value", "firstNonWhitespacePosition", "trivia", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_262()
            ' Line 371

            '             Dim trivia = token.LeadingTrivia.FirstOrDefault()
            '             Return trivia.Kind = SyntaxKind.DocumentationCommentExteriorTrivia
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17354)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.Kind", "trivia", "SyntaxKind.DocumentationCommentExteriorTrivia", "SyntaxKind", "token.LeadingTrivia.FirstOrDefault", "token.LeadingTrivia", "token", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_263()
            ' Line 372

            '             Return trivia.Kind = SyntaxKind.DocumentationCommentExteriorTrivia
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17430)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.Kind", "trivia", "SyntaxKind.DocumentationCommentExteriorTrivia", "SyntaxKind", "tree", "documentationComment", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_264()
            ' Line 374

            ' 
            '         Private Function IsExteriorTriviaLeftOfPosition(ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17454)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_265()
            ' Line 374

            ' 
            '         Private Function IsExteriorTriviaLeftOfPosition(ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17454)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "position", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_266()
            ' Line 375

            '         Private Function IsExteriorTriviaLeftOfPosition(ByVal documentationComment As DocumentationCommentSyntax, ByVal position As Integer) As Boolean
            '             Dim token = documentationComment.FindToken(position)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17611)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment.FindToken", "documentationComment", "position", "token", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_267()
            ' Line 376

            '             Dim token = documentationComment.FindToken(position)
            '             Dim trivia = token.LeadingTrivia.FirstOrDefault()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17677)
            Assert.NotNull(terms)
            AssertEx.Equal({"token.LeadingTrivia.FirstOrDefault", "token.LeadingTrivia", "token", "documentationComment.FindToken", "documentationComment", "position", "trivia", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_268()
            ' Line 377

            '             Dim trivia = token.LeadingTrivia.FirstOrDefault()
            '             Return trivia.Kind = SyntaxKind.DocumentationCommentExteriorTrivia AndAlso
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17740)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.Kind", "trivia", "SyntaxKind.DocumentationCommentExteriorTrivia", "SyntaxKind", "trivia.Span.End", "trivia.Span", "position", "token.LeadingTrivia.FirstOrDefault", "token.LeadingTrivia", "token", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_269()
            ' Line 379

            '                 trivia.Span.End = position
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17868)
            Assert.NotNull(terms)
            AssertEx.Equal({"trivia.Kind", "trivia", "SyntaxKind.DocumentationCommentExteriorTrivia", "SyntaxKind", "trivia.Span.End", "trivia.Span", "position", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_270()
            ' Line 381

            ' 
            '         Private Function GetDocumentationCommentTargetMember(ByVal documentationComment As DocumentationCommentSyntax) As StatementSyntax
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17892)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_271()
            ' Line 381

            ' 
            '         Private Function GetDocumentationCommentTargetMember(ByVal documentationComment As DocumentationCommentSyntax) As StatementSyntax
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 17892)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_272()
            ' Line 382

            '         Private Function GetDocumentationCommentTargetMember(ByVal documentationComment As DocumentationCommentSyntax) As StatementSyntax
            '             Dim parentTrivia = documentationComment.ParentTrivia
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18035)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment.ParentTrivia", "documentationComment", "parentTrivia", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_273()
            ' Line 383

            '             Dim parentTrivia = documentationComment.ParentTrivia
            '             Return parentTrivia.Token.GetAncestor(Of StatementSyntax)()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18101)
            Assert.NotNull(terms)
            AssertEx.Equal({"parentTrivia.Token.GetAncestor(Of StatementSyntax)", "parentTrivia.Token", "parentTrivia", "documentationComment.ParentTrivia", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_274()
            ' Line 384

            '             Return parentTrivia.Token.GetAncestor(Of StatementSyntax)()
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18170)
            Assert.NotNull(terms)
            AssertEx.Equal({"parentTrivia.Token.GetAncestor(Of StatementSyntax)", "parentTrivia.Token", "parentTrivia", "documentationComment", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_275()
            ' Line 386

            ' 
            '         Private Function SpansSingleLine(ByVal documentationComment As DocumentationCommentSyntax, ByVal snapshot As ITextSnapshot) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18194)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_276()
            ' Line 386

            ' 
            '         Private Function SpansSingleLine(ByVal documentationComment As DocumentationCommentSyntax, ByVal snapshot As ITextSnapshot) As Boolean
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18194)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_277()
            ' Line 388

            '             ' Use full span to include leading exterior trivia
            '             Dim startLine = snapshot.GetLineNumberFromPosition(documentationComment.FullSpan.Start)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18406)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot.GetLineNumberFromPosition", "snapshot", "documentationComment.FullSpan.Start", "documentationComment.FullSpan", "documentationComment", "startLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_278()
            ' Line 392

            '             ' to ensure that we aren't getting the line number after the line break.
            '             Dim lastToken = documentationComment.GetLastToken()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18703)
            Assert.NotNull(terms)
            AssertEx.Equal({"documentationComment.GetLastToken", "documentationComment", "startLine", "snapshot.GetLineNumberFromPosition", "snapshot", "documentationComment.FullSpan.Start", "documentationComment.FullSpan", "lastToken", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_279()
            ' Line 393

            '             Dim lastToken = documentationComment.GetLastToken()
            '             Dim endLine = snapshot.GetLineNumberFromPosition(lastToken.Span.Start)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18768)
            Assert.NotNull(terms)
            AssertEx.Equal({"snapshot.GetLineNumberFromPosition", "snapshot", "lastToken.Span.Start", "lastToken.Span", "lastToken", "documentationComment.GetLastToken", "documentationComment", "endLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_280()
            ' Line 395

            ' 
            '             Return startLine = endLine
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18854)
            Assert.NotNull(terms)
            AssertEx.Equal({"startLine", "endLine", "snapshot.GetLineNumberFromPosition", "snapshot", "lastToken.Span.Start", "lastToken.Span", "lastToken", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_281()
            ' Line 396

            '             Return startLine = endLine
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 18890)
            Assert.NotNull(terms)
            AssertEx.Equal({"startLine", "endLine", "documentationComment", "snapshot", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_282()
            ' Line 402

            '         ''' </summary>
            '         Private Function GenerateDocumentationCommentText(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19146)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember", "tree", "indent", "prependExteriorTrivia", "appendLineBreakAndIndent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_283()
            ' Line 402

            '         ''' </summary>
            '         Private Function GenerateDocumentationCommentText(
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19146)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember", "tree", "indent", "prependExteriorTrivia", "appendLineBreakAndIndent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_284()
            ' Line 409

            ' 
            '             Dim builder As New StringBuilder
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19511)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder", "targetMember", "tree", "indent", "prependExteriorTrivia", "appendLineBreakAndIndent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_285()
            ' Line 411

            ' 
            '             If prependExteriorTrivia Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19559)
            Assert.NotNull(terms)
            AssertEx.Equal({"prependExteriorTrivia", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_286()
            ' Line 411

            ' 
            '             If prependExteriorTrivia Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19559)
            Assert.NotNull(terms)
            AssertEx.Equal({"prependExteriorTrivia", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_287()
            ' Line 412

            '             If prependExteriorTrivia Then
            '                 builder.Append("'''")
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19606)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_288()
            ' Line 413

            '                 builder.Append("'''")
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19641)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_289()
            ' Line 416

            '             ' Append summary
            '             builder.AppendLine(" <summary>")
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19693)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.AppendLine", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_290()
            ' Line 417

            '             builder.AppendLine(" <summary>")
            '             builder.Append(indent + "''' ")
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19739)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "builder.AppendLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_291()
            ' Line 418

            '             builder.Append(indent + "''' ")
            '             Dim offset = builder.Length
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19784)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Length", "builder", "builder.Append", "indent", "offset", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_292()
            ' Line 419

            '             Dim offset = builder.Length
            '             builder.AppendLine()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19825)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.AppendLine", "builder", "offset", "builder.Length", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_293()
            ' Line 420

            '             builder.AppendLine()
            '             builder.Append(indent + "''' </summary>")
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19859)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "builder.AppendLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_294()
            ' Line 423

            '             ' Append any type parameters
            '             Dim typeParameterList = targetMember.GetTypeParameterList()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 19958)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.GetTypeParameterList", "targetMember", "builder.Append", "builder", "indent", "typeParameterList", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_295()
            ' Line 424

            '             Dim typeParameterList = targetMember.GetTypeParameterList()
            '             If typeParameterList IsNot Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20031)
            Assert.NotNull(terms)
            AssertEx.Equal({"typeParameterList", "targetMember.GetTypeParameterList", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_296()
            ' Line 424

            '             Dim typeParameterList = targetMember.GetTypeParameterList()
            '             If typeParameterList IsNot Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20031)
            Assert.NotNull(terms)
            AssertEx.Equal({"typeParameterList", "targetMember.GetTypeParameterList", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_297()
            ' Line 425

            '             If typeParameterList IsNot Nothing Then
            '                 For Each typeParameter In typeParameterList.Parameters
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20088)
            Assert.NotNull(terms)
            AssertEx.Equal({"typeParameter", "typeParameterList.Parameters", "typeParameterList", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_298()
            ' Line 425

            '             If typeParameterList IsNot Nothing Then
            '                 For Each typeParameter In typeParameterList.Parameters
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20088)
            Assert.NotNull(terms)
            AssertEx.Equal({"typeParameter", "typeParameterList.Parameters", "typeParameterList", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_299()
            ' Line 426

            '                 For Each typeParameter In typeParameterList.Parameters
            '                     builder.AppendLine()
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20164)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.AppendLine", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_300()
            ' Line 428

            ' 
            '                     builder.Append(indent + "''' <typeparam name=""")
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20208)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "builder.AppendLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_301()
            ' Line 430

            ' 
            '                     Dim typeParameterName = typeParameter.Name.GetText()
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20281)
            Assert.NotNull(terms)
            AssertEx.Equal({"typeParameter.Name.GetText", "typeParameter.Name", "typeParameter", "builder.Append", "builder", "indent", "typeParameterName", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_302()
            ' Line 431

            '                     Dim typeParameterName = typeParameter.Name.GetText()
            '                     If Not String.IsNullOrWhiteSpace(typeParameterName) Then
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20355)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "typeParameterName", "typeParameter.Name.GetText", "typeParameter.Name", "typeParameter", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_303()
            ' Line 431

            '                     Dim typeParameterName = typeParameter.Name.GetText()
            '                     If Not String.IsNullOrWhiteSpace(typeParameterName) Then
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20355)
            Assert.NotNull(terms)
            AssertEx.Equal({"String.IsNullOrWhiteSpace", "typeParameterName", "typeParameter.Name.GetText", "typeParameter.Name", "typeParameter", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_304()
            ' Line 432

            '                     If Not String.IsNullOrWhiteSpace(typeParameterName) Then
            '                         builder.Append(typeParameterName)
            '                         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20437)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "typeParameterName", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_305()
            ' Line 433

            '                         builder.Append(typeParameterName)
            '                     Else
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20492)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "typeParameterName", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_306()
            ' Line 434

            '                     Else
            '                         builder.Append("?")
            '                         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20522)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_307()
            ' Line 435

            '                         builder.Append("?")
            '                     End If
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20563)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_308()
            ' Line 437

            ' 
            '                     builder.Append("""></typeparam>")
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20593)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_309()
            ' Line 438

            '                     builder.Append("""></typeparam>")
            '                 Next
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20644)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_310()
            ' Line 439

            '                 Next
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20662)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_311()
            ' Line 442

            '             ' Append any parameters
            '             Dim parameterList = targetMember.GetParameterList()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20721)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.GetParameterList", "targetMember", "parameterList", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_312()
            ' Line 443

            '             Dim parameterList = targetMember.GetParameterList()
            '             If parameterList IsNot Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20786)
            Assert.NotNull(terms)
            AssertEx.Equal({"parameterList", "targetMember.GetParameterList", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_313()
            ' Line 443

            '             Dim parameterList = targetMember.GetParameterList()
            '             If parameterList IsNot Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20786)
            Assert.NotNull(terms)
            AssertEx.Equal({"parameterList", "targetMember.GetParameterList", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_314()
            ' Line 444

            '             If parameterList IsNot Nothing Then
            '                 For Each parameter In parameterList.Parameters
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20839)
            Assert.NotNull(terms)
            AssertEx.Equal({"parameter", "parameterList.Parameters", "parameterList", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_315()
            ' Line 444

            '             If parameterList IsNot Nothing Then
            '                 For Each parameter In parameterList.Parameters
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20839)
            Assert.NotNull(terms)
            AssertEx.Equal({"parameter", "parameterList.Parameters", "parameterList", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_316()
            ' Line 445

            '                 For Each parameter In parameterList.Parameters
            '                     builder.AppendLine()
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20907)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.AppendLine", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_317()
            ' Line 447

            ' 
            '                     builder.Append(indent + "''' <param name=""")
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 20951)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "builder.AppendLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_318()
            ' Line 448

            '                     builder.Append(indent + "''' <param name=""")
            '                     builder.Append(parameter.Name.GetText())
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21018)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "parameter.Name.GetText", "parameter.Name", "parameter", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_319()
            ' Line 449

            '                     builder.Append(parameter.Name.GetText())
            '                     builder.Append("""></param>")
            '                     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21080)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "parameter.Name.GetText", "parameter.Name", "parameter", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_320()
            ' Line 450

            '                     builder.Append("""></param>")
            '                 Next
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21127)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_321()
            ' Line 451

            '                 Next
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21145)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_322()
            ' Line 454

            '             ' Append return type
            '             Dim returnType = targetMember.GetReturnType()
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21201)
            Assert.NotNull(terms)
            AssertEx.Equal({"targetMember.GetReturnType", "targetMember", "returnType", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_323()
            ' Line 455

            '             Dim returnType = targetMember.GetReturnType()
            '             If returnType IsNot Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21260)
            Assert.NotNull(terms)
            AssertEx.Equal({"returnType", "targetMember.GetReturnType", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_324()
            ' Line 455

            '             Dim returnType = targetMember.GetReturnType()
            '             If returnType IsNot Nothing Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21260)
            Assert.NotNull(terms)
            AssertEx.Equal({"returnType", "targetMember.GetReturnType", "targetMember", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_325()
            ' Line 456

            '             If returnType IsNot Nothing Then
            '                 builder.AppendLine()
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21310)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.AppendLine", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_326()
            ' Line 457

            '                 builder.AppendLine()
            '                 builder.Append(indent + "''' <returns></returns>")
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21348)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "builder.AppendLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_327()
            ' Line 458

            '                 builder.Append(indent + "''' <returns></returns>")
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21412)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_328()
            ' Line 460

            ' 
            '             If appendLineBreakAndIndent Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21434)
            Assert.NotNull(terms)
            AssertEx.Equal({"appendLineBreakAndIndent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_329()
            ' Line 460

            ' 
            '             If appendLineBreakAndIndent Then
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21434)
            Assert.NotNull(terms)
            AssertEx.Equal({"appendLineBreakAndIndent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_330()
            ' Line 461

            '             If appendLineBreakAndIndent Then
            '                 builder.AppendLine()
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21484)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.AppendLine", "builder", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_331()
            ' Line 462

            '                 builder.AppendLine()
            '                 builder.Append(indent)
            '                 ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21522)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "builder.AppendLine", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_332()
            ' Line 463

            '                 builder.Append(indent)
            '             End If
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21558)
            Assert.NotNull(terms)
            AssertEx.Equal({"builder.Append", "builder", "indent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_333()
            ' Line 465

            ' 
            '             Return Tuple.Create(builder.ToString(), offset)
            '             ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21580)
            Assert.NotNull(terms)
            AssertEx.Equal({"Tuple.Create", "Tuple", "builder.ToString", "builder", "offset", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_334()
            ' Line 466

            '             Return Tuple.Create(builder.ToString(), offset)
            '         End Function
            '         ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21637)
            Assert.NotNull(terms)
            AssertEx.Equal({"Tuple.Create", "Tuple", "builder.ToString", "builder", "offset", "targetMember", "tree", "indent", "prependExteriorTrivia", "appendLineBreakAndIndent", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_335()
            ' Line 467

            '         End Function
            '     End Class
            '     ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21655)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAtStartOfStatement_336()
            ' Line 468

            '     End Class
            ' End Namespace
            ' ^
            Dim tree = GetTree()
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, 21666)
            Assert.NotNull(terms)
            AssertEx.Equal({"Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAddHandler()
            Const source = "
Class C
    Event E As System.Action

    Sub Main()
        Dim c As New C()
        AddHandler c.E, AddressOf M
        Dim x = 1
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim x", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"c.E", "c", "M", "x", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestRemoveHandler()
            Const source = "
Class C
    Event E As System.Action

    Sub Main()
        Dim c As New C()
        RemoveHandler c.E, AddressOf M
        Dim x = 1
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim x", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"c.E", "c", "M", "x", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestRaiseEvent()
            Const source = "
Class C
    Event E As System.Action

    Sub Main()
        RaiseEvent E()
        Dim x = 1
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim x", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"E", "x", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestYield()
            Const source = "
Class C
    Iterator Function I() As System.Collections.Enumerable
        Dim x = 1
        Yield x
        Dim y = 1
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim y", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"x", "y", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestRedim()
            Const source = "
Class C
    Sub Main()
        Dim intArray(10, 10, 10) As Integer
        ReDim intArray(10, 10, 10)
        Dim x = 1
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim x", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"intArray", "x", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestErase()
            Const source = "
Class C
    Sub Main()
        Dim intArray(10, 10, 10) As Integer
        Erase intArray
        Dim x = 1
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim x", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"intArray", "x", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestAssignment()
            Const sourceTemplate = "
Class C
    Sub Main()
        Dim x = 1
        x {0} 1
        Dim y = 2
    End Sub
End Class
"
            ' NOTE: The feature is syntactic, so it doesn't matter that the types don't work out.
            For Each op In {"=", "+=", "-=", "*=", "/=", "\=", "^=", "<<=", ">>=", "&="}
                Dim source = String.Format(sourceTemplate, op)
                Dim tree = VisualBasicSyntaxTree.ParseText(source)
                Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim y", StringComparison.Ordinal))
                Assert.NotNull(terms)
                AssertEx.Equal({"x", "y", "Me"}, terms)
            Next
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions)>
        Public Sub TestMidAssignment()
            Const source = "
Class C
    Sub Main()
        Dim s = ""abc""
        Dim x = 1
        Mid(s, x, x) = ""q""
        Dim y = 1
    End Sub
End Class
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim y", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"s", "x", "y", "Me"}, terms)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.DebuggingProximityExpressions), WorkItem(903546, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/903546")>
        Public Sub Repro903546()
            Const source = "
Module Module1
    Sub Main()
        Dim a As Integer = 1
        Dim b As Integer = 2
        b = 3
        Dim c = 4
    End Sub
End Module
"
            Dim tree = VisualBasicSyntaxTree.ParseText(source)
            Dim terms = VisualBasicProximityExpressionsService.Do(tree, source.IndexOf("Dim c", StringComparison.Ordinal))
            Assert.NotNull(terms)
            AssertEx.Equal({"b", "c"}, terms)
        End Sub
    End Class
End Namespace
