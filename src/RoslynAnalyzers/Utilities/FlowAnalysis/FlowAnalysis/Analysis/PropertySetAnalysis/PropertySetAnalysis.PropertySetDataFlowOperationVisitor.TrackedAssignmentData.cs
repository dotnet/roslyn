// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    internal partial class PropertySetAnalysis
    {
        private sealed partial class PropertySetDataFlowOperationVisitor
        {
            /// <summary>
            /// For a given analysis entity, keep track of property / field assignment operations and abstract locations, for
            /// evaluating hazardous usages on initializations.
            /// </summary>
            private class TrackedAssignmentData
            {
                public PooledHashSet<IAssignmentOperation>? AssignmentsWithUnknownLocation
                {
                    get;
                    private set;
                }

                public PooledDictionary<AbstractLocation, PooledHashSet<IAssignmentOperation>>? AbstractLocationsToAssignments
                {
                    get;
                    private set;
                }

                public void Free()
                {
                    this.AssignmentsWithUnknownLocation?.Free();
                    this.AssignmentsWithUnknownLocation = null;

                    if (this.AbstractLocationsToAssignments != null)
                    {
                        foreach (PooledHashSet<IAssignmentOperation> hashSet in this.AbstractLocationsToAssignments.Values)
                        {
                            hashSet?.Free();
                        }

                        this.AbstractLocationsToAssignments.Free();
                        this.AbstractLocationsToAssignments = null;
                    }
                }

                public void TrackAssignmentWithUnknownLocation(IAssignmentOperation assignmentOperation)
                {
                    this.AssignmentsWithUnknownLocation ??= PooledHashSet<IAssignmentOperation>.GetInstance();

                    this.AssignmentsWithUnknownLocation.Add(assignmentOperation);
                }

                public void TrackAssignmentWithAbstractLocation(
                    IAssignmentOperation assignmentOperation,
                    AbstractLocation abstractLocation)
                {
                    this.AbstractLocationsToAssignments ??=
                            PooledDictionary<AbstractLocation, PooledHashSet<IAssignmentOperation>>.GetInstance();

                    if (!this.AbstractLocationsToAssignments.TryGetValue(
                            abstractLocation,
                            out PooledHashSet<IAssignmentOperation> assignments))
                    {
                        assignments = PooledHashSet<IAssignmentOperation>.GetInstance();
                        this.AbstractLocationsToAssignments.Add(abstractLocation, assignments);
                    }

                    assignments.Add(assignmentOperation);
                }
            }
        }
    }
}
