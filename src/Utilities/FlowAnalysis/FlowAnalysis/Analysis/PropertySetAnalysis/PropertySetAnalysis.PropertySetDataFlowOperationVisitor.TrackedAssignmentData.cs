// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.Operations;

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
                public PooledHashSet<IAssignmentOperation> AssignmentsWithUnknownLocation
                {
                    get;
                    private set;
                }

                public PooledDictionary<AbstractLocation, PooledHashSet<IAssignmentOperation>> AbstractLocationsToAssignments
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
                    if (this.AssignmentsWithUnknownLocation == null)
                    {
                        this.AssignmentsWithUnknownLocation = PooledHashSet<IAssignmentOperation>.GetInstance();
                    }

                    this.AssignmentsWithUnknownLocation.Add(assignmentOperation);
                }

                public void TrackAssignmentWithAbstractLocation(
                    IAssignmentOperation assignmentOperation,
                    AbstractLocation abstractLocation)
                {
                    if (this.AbstractLocationsToAssignments == null)
                    {
                        this.AbstractLocationsToAssignments =
                            PooledDictionary<AbstractLocation, PooledHashSet<IAssignmentOperation>>.GetInstance();
                    }

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
