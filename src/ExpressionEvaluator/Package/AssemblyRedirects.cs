// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell;

[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ExpressionCompiler",
    OldVersionLowerBound = Constants.OldVersionLowerBound,
    OldVersionUpperBound = Constants.OldVersionUpperBound,
    NewVersion = Constants.NewVersion,
    PublicKeyToken = Constants.PublicKeyToken,
    GenerateCodeBase = false)]

[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator.ResultProvider",
    OldVersionLowerBound = Constants.OldVersionLowerBound,
    OldVersionUpperBound = Constants.OldVersionUpperBound,
    NewVersion = Constants.NewVersion,
    PublicKeyToken = Constants.PublicKeyToken,
    GenerateCodeBase = false)]

[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.CodeAnalysis.ExpressionEvaluator.ExpressionCompiler",
    OldVersionLowerBound = Constants.OldVersionLowerBound,
    OldVersionUpperBound = Constants.OldVersionUpperBound,
    NewVersion = Constants.NewVersion,
    PublicKeyToken = Constants.PublicKeyToken,
    GenerateCodeBase = false)]

[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.CodeAnalysis.ExpressionEvaluator.ResultProvider",
    OldVersionLowerBound = Constants.OldVersionLowerBound,
    OldVersionUpperBound = Constants.OldVersionUpperBound,
    NewVersion = Constants.NewVersion,
    PublicKeyToken = Constants.PublicKeyToken,
    GenerateCodeBase = false)]

[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ExpressionCompiler",
    OldVersionLowerBound = Constants.OldVersionLowerBound,
    OldVersionUpperBound = Constants.OldVersionUpperBound,
    NewVersion = Constants.NewVersion,
    PublicKeyToken = Constants.PublicKeyToken,
    GenerateCodeBase = false)]

[assembly: ProvideBindingRedirection(
    AssemblyName = "Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.ResultProvider",
    OldVersionLowerBound = Constants.OldVersionLowerBound,
    OldVersionUpperBound = Constants.OldVersionUpperBound,
    NewVersion = Constants.NewVersion,
    PublicKeyToken = Constants.PublicKeyToken,
    GenerateCodeBase = false)]

internal class Constants
{
#if OFFICIAL_BUILD
    // If this is an official build we want to generate binding
    // redirects from our old versions to the release version 
    public const string OldVersionLowerBound = "0.7.0.0";
    public const string OldVersionUpperBound = "0.7.0.0";
    public const string NewVersion = "1.0.0.0";
#else
    // Non-official builds get redirects to local 42.42.42.42,
    // which will only be built locally
    public const string OldVersionLowerBound = "0.7.0.0";
    public const string OldVersionUpperBound = "1.0.0.0";
    public const string NewVersion = "42.42.42.42";
#endif
    public const string PublicKeyToken = "31BF3856AD364E35";
}
