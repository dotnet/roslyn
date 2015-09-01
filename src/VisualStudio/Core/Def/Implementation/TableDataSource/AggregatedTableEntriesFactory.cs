// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class AggregatedTableEntriesFactory<TData> : TableEntriesFactory<TData>
    {
        private readonly FactoryCollection _factories;

        public AggregatedTableEntriesFactory(AbstractTableDataSource<TData> source, TableEntriesFactory<TData> primary) : base(source, null)
        {
            _factories = new FactoryCollection(primary);
        }

        public void AddOrUpdateFactory(TableEntriesFactory<TData> factory)
        {
            lock (this.Gate)
            {
                var factories = _factories.GetFactories();
                factories.Add(factory);

                UpdateVersion_NoLock();
            }
        }

        private struct FactoryCollection
        {
            private TableEntriesFactory<TData> _primary;
            private List<TableEntriesFactory<TData>> _factories;

            public FactoryCollection(TableEntriesFactory<TData> primary) : this()
            {
                _primary = primary;
            }

            public TableEntriesFactory<TData> Primary
            {
                get
                {
                    if (_primary != null)
                    {
                        return _primary;
                    }

                    if (_factories.Count == 1)
                    {
                        return _factories[0];
                    }

                    return null;
                }
            }

            public List<TableEntriesFactory<TData>> GetFactories()
            {
                if (_factories == null)
                {
                    _factories = new List<TableEntriesFactory<TData>>();
                }

                _factories.Add(_primary);
                _primary = null;

                return _factories;
            }
        }
    }
}