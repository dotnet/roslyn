//// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//using System.Composition;
//using System.IO;
//using Microsoft.CodeAnalysis.Host.Mef;
//using Roslyn.Utilities;

//namespace Microsoft.CodeAnalysis.Serialization
//{
//    [ExportWorkspaceService(typeof(IAssemblySerializationInfoService), ServiceLayer.Default), Shared]
//    internal class DefaultAssemblySerializationInfoService : IAssemblySerializationInfoService
//    {
//        public bool Serializable(Solution solution, string assemblyFilePath)
//        {
//            return false;
//        }

//        public bool TryGetSerializationPrefix(Solution solution, string assemblyFilePath, out string prefix)
//        {
//            prefix = string.Empty;
//            return false;
//        }
//    }

//    internal class SimpleAssemblySerializationInfoService : IAssemblySerializationInfoService
//    {
//        public bool Serializable(Solution solution, string assemblyFilePath)
//        {
//            if (assemblyFilePath == null || !File.Exists(assemblyFilePath))
//            {
//                return false;
//            }

//            // if solution is not from a disk, just create one.
//            if (solution.FilePath == null || !File.Exists(solution.FilePath))
//            {
//                return false;
//            }

//            return true;
//        }

//        public bool TryGetSerializationPrefix(Solution solution, string assemblyFilePath, out string prefix)
//        {
//            prefix = PathUtilities.GetRelativePath(solution.FilePath, assemblyFilePath);
//            return true;
//        }
//    }
//}