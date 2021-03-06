// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Caching.Queries.Filters;
using Alachisoft.NCache.Serialization.Formatters;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Queries;
using Alachisoft.NCache.Common.Enum;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Caching.Queries
{
    public class QueryResultSet : ICompactSerializable
    {
        private ArrayList _searchKeysResult;
        private Hashtable _searchEntriesResult;
        private DictionaryEntry _aggregateFunctionResult;
        private bool _isInitialized = false;
        private ArrayList _keysForUpdateIndices;

        private QueryType _queryType = QueryType.SearchKeys;
        private AggregateFunctionType _aggregateFunctionType = AggregateFunctionType.NOTAPPLICABLE;

        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        public QueryType Type
        {
            get { return _queryType; }
            set { _queryType = value; }
        }

        public AggregateFunctionType AggregateFunctionType
        {
            get { return _aggregateFunctionType; }
            set { _aggregateFunctionType = value; }
        }

        public ArrayList SearchKeysResult
        {
            get { return _searchKeysResult; }
            set { _searchKeysResult = value; }
        }

        public Hashtable SearchEntriesResult
        {
            get { return _searchEntriesResult; }
            set { _searchEntriesResult = value; }
        }

        public DictionaryEntry AggregateFunctionResult
        {
            get { return _aggregateFunctionResult; }
            set { _aggregateFunctionResult = value; }
        }

        public ArrayList UpdateIndicesKeys 
        {
            get { return _keysForUpdateIndices; }
            set { _keysForUpdateIndices = value; }
        }

        public void Initialize(QueryResultSet resultSet)
        {
            if (!_isInitialized)
            {
                this.Type = resultSet.Type;
                this.AggregateFunctionType = resultSet.AggregateFunctionType;
                this.AggregateFunctionResult = resultSet.AggregateFunctionResult;
                this.SearchKeysResult = resultSet.SearchKeysResult;
                this.SearchEntriesResult = resultSet.SearchEntriesResult;
                this._isInitialized = true;
            }
        }

        public void Compile(QueryResultSet resultSet)
        {
            if (!this._isInitialized)
            {
                Initialize(resultSet);
                return;
            }

            switch (this.Type)
            {
                case QueryType.AggregateFunction:

                    switch ((AggregateFunctionType)this.AggregateFunctionResult.Key)
                    {
                        case AggregateFunctionType.SUM:
                            decimal a;
                            decimal b;

                            object thisVal = this.AggregateFunctionResult.Value;
                            object otherVal = resultSet.AggregateFunctionResult.Value;

                            Nullable<decimal> sum = null;

                            if (thisVal == null && otherVal != null)
                            {
                                sum = (decimal)otherVal;
                            }
                            else if (thisVal != null && otherVal == null)
                            {
                                sum = (decimal)thisVal;
                            }
                            else if (thisVal != null && otherVal != null)
                            { 
                                a = (decimal)thisVal;
                                b = (decimal)otherVal;
                                sum = a + b;
                            }

                            if (sum != null)
                            {
                                this.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.SUM, sum);
                            }
                            else
                            {
                                this.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.SUM, null);
                            }
                            break;

                        case AggregateFunctionType.COUNT:
                            a = (decimal)this.AggregateFunctionResult.Value;
                            b = (decimal)resultSet.AggregateFunctionResult.Value;
                            decimal count = a + b;

                            this.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.COUNT, count);
                            break;

                        case AggregateFunctionType.MIN:
                            IComparable thisValue = (IComparable)this.AggregateFunctionResult.Value;
                            IComparable otherValue = (IComparable)resultSet.AggregateFunctionResult.Value;
                            IComparable min = thisValue;

                            if (thisValue == null && otherValue != null)
                            {
                                min = otherValue;
                            }
                            else if (thisValue != null && otherValue == null)
                            {
                                min = thisValue;
                            }
                            else if (thisValue == null && otherValue == null)
                            {
                                min = null;
                            }
                            else if (otherValue.CompareTo(thisValue) < 0)
                            {
                                min = otherValue;
                            }

                            this.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.MIN, min);
                            break;

                        case AggregateFunctionType.MAX:
                            thisValue = (IComparable)this.AggregateFunctionResult.Value;
                            otherValue = (IComparable)resultSet.AggregateFunctionResult.Value;
                            IComparable max = thisValue;

                            if (thisValue == null && otherValue != null)
                            {
                                max = otherValue;
                            }
                            else if (thisValue != null && otherValue == null)
                            {
                                max = thisValue;
                            }
                            else if (thisValue == null && otherValue == null)
                            {
                                max = null;
                            }
                            else if (otherValue.CompareTo(thisValue) > 0)
                            {
                                max = otherValue;
                            }

                            this.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.MAX, max);
                            break;

                        case AggregateFunctionType.AVG:
                            thisVal = this.AggregateFunctionResult.Value;
                            otherVal = resultSet.AggregateFunctionResult.Value;

                            AverageResult avg = null;
                            if (thisVal == null && otherVal != null)
                            {
                                avg = (AverageResult)otherVal;
                            }
                            else if (thisVal != null && otherVal == null)
                            {
                                avg = (AverageResult)thisVal;
                            }
                            else if (thisVal != null && otherVal != null)
                            {
                                AverageResult thisResult = (AverageResult)thisVal;
                                AverageResult otherResult = (AverageResult)otherVal;

                                avg = new AverageResult();
                                avg.Sum = thisResult.Sum + otherResult.Sum;
                                avg.Count = thisResult.Count + otherResult.Count;
                            }

                            if (avg != null)
                            {
                                this.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.AVG, avg);
                            }
                            else
                            {
                                this.AggregateFunctionResult = new DictionaryEntry(AggregateFunctionType.AVG, null);
                            }
                            break;
                    }
                    
                    break;

                case QueryType.SearchKeys:
                    if (this.SearchKeysResult == null)
                        this.SearchKeysResult = resultSet.SearchKeysResult;
                    else
                        if (resultSet.SearchKeysResult != null)
                            this.SearchKeysResult.AddRange(resultSet.SearchKeysResult);

                    break;

                case QueryType.SearchEntries:
                    if (this.SearchEntriesResult == null)
                        this.SearchEntriesResult = resultSet.SearchEntriesResult;
                    else
                    {
                        IDictionaryEnumerator ide = resultSet.SearchEntriesResult.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            this.SearchEntriesResult[ide.Key]=ide.Value;
                        }
                    }

                    break;
            }
        }

        void ICompactSerializable.Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _aggregateFunctionResult = (DictionaryEntry)reader.ReadObject();
            _searchKeysResult = reader.ReadObject() as ArrayList;
            _searchEntriesResult = reader.ReadObject() as Hashtable;
            _queryType = (QueryType)reader.ReadInt32();
            _aggregateFunctionType = (AggregateFunctionType)reader.ReadInt32();
        }

        void ICompactSerializable.Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_aggregateFunctionResult);
            writer.WriteObject(_searchKeysResult);
            writer.WriteObject(_searchEntriesResult);
            writer.Write(Convert.ToInt32(_queryType));
            writer.Write(Convert.ToInt32(_aggregateFunctionType));
        }
    }
}
