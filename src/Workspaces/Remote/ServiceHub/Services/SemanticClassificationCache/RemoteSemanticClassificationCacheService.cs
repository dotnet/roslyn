//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using System;
//using System.Collections.Generic;
//using System.Collections.Immutable;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.CodeAnalysis.Classification;
//using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
//using Microsoft.CodeAnalysis.Host;
//using Microsoft.CodeAnalysis.Internal.Log;
//using Microsoft.CodeAnalysis.PooledObjects;
//using Microsoft.CodeAnalysis.SemanticClassificationCache;
//using Microsoft.CodeAnalysis.Shared.Extensions;
//using Microsoft.CodeAnalysis.Shared.TestHooks;
//using Microsoft.CodeAnalysis.Storage;
//using Microsoft.CodeAnalysis.Text;
//using Roslyn.Utilities;

//namespace Microsoft.CodeAnalysis.Remote
//{
//    internal sealed class RemoteSemanticClassificationCacheService : BrokeredServiceBase, IRemoteSemanticClassificationCacheService
//    {
//        internal sealed class Factory : FactoryBase<IRemoteSemanticClassificationCacheService>
//        {
//            protected override IRemoteSemanticClassificationCacheService CreateService(in ServiceConstructionArguments arguments)
//                => new RemoteSemanticClassificationCacheService(arguments);
//        }




//        public RemoteSemanticClassificationCacheService(in ServiceConstructionArguments arguments)
//            : base(arguments)
//        {
//            _workQueue = new AsyncBatchingWorkQueue<Document>(
//                TimeSpan.FromMilliseconds(TaggerConstants.ShortDelay),
//                CacheSemanticClassificationsAsync,
//                AsynchronousOperationListenerProvider.NullListener,
//                _cancellationTokenSource.Token);
//        }

//        public override void Dispose()
//        {
//            _cancellationTokenSource.Cancel();
//            base.Dispose();
//        }


//    }
//}
