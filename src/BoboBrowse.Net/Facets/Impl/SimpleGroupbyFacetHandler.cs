//////TODO: Work out how to make this function with Lucene.Net 3.0.3.

namespace BoboBrowse.Net.Facets.Impl
{
    using BoboBrowse.Net;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Util;   
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class SimpleGroupbyFacetHandler : FacetHandler, IFacetHandlerFactory
    {
        private readonly HashSet<string> _fieldsSet;
        private IList<FacetHandler> _facetHandlers;
        private IDictionary<string, FacetHandler> _facetHandlerMap;

        private const string SEP = ",";
        private int _maxdoc;
        private readonly string _sep;

        public SimpleGroupbyFacetHandler(string name, IEnumerable<string> dependsOn, string separator)
            : base(name, dependsOn)
        {
            _fieldsSet = new HashSet<string>(dependsOn);            
            _facetHandlers = null;
            _facetHandlerMap = null;
            _maxdoc = 0;
            _sep = separator;
        }

        public SimpleGroupbyFacetHandler(string name, IEnumerable<string> dependsOn)
            : this(name, dependsOn, SEP)
        {
        }

        public override RandomAccessFilter BuildRandomAccessFilter(string @value, Properties selectionProperty)
        {
            List<RandomAccessFilter> filterList = new List<RandomAccessFilter>();
            string[] vals = @value.Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < vals.Length; ++i)
            {
                FacetHandler handler = _facetHandlers[i];
                BrowseSelection sel = new BrowseSelection(handler.Name);
                sel.AddValue(vals[i]);
                filterList.Add(handler.BuildFilter(sel));
            }
            return new RandomAccessAndFilter(filterList);
        }

        public override IFacetCountCollector GetFacetCountCollector(BrowseSelection sel, FacetSpec fspec)
        {
            var collectorList = new List<DefaultFacetCountCollector>(_facetHandlers.Count);
            foreach (SimpleFacetHandler facetHandler in _facetHandlers)
            {
                collectorList.Add((DefaultFacetCountCollector)facetHandler.GetFacetCountCollector(sel, fspec));
            }
            return new GroupbyFacetCountCollector(Name, fspec, collectorList.ToArray(), _maxdoc, _sep);
        }

        public override string[] GetFieldValues(int id)
        {
            List<string> valList = new List<string>();
            foreach (FacetHandler handler in _facetHandlers)
            {
                StringBuilder buf = new StringBuilder();
                bool firsttime = true;
                string[] vals = handler.GetFieldValues(id);
                if (vals != null && vals.Length > 0)
                {
                    if (!firsttime)
                    {
                        buf.Append(",");
                    }
                    else
                    {
                        firsttime = false;
                    }
                    foreach (string val in vals)
                    {
                        buf.Append(val);
                    }
                }
                valList.Add(buf.ToString());
            }
            return valList.ToArray();
        }

        public override object[] GetRawFieldValues(int id)
        {
            return GetFieldValues(id);
        }
       
        public override FieldComparator GetComparator(int numDocs, SortField field)
        {
            var comparatorList = new List<FieldComparator>(_fieldsSet.Count);
            foreach (var handler in _facetHandlers)
            {
                comparatorList.Add(handler.GetComparator(numDocs, field));
            }
            return new GroupbyFacetFieldComparator(numDocs, comparatorList);
        }

        public override void Load(BoboIndexReader reader)
        {
            _facetHandlers = new List<FacetHandler>(_fieldsSet.Count);
            _facetHandlerMap = new Dictionary<string, FacetHandler>(_fieldsSet.Count);
            foreach (string name in _fieldsSet)
            {
                FacetHandler handler = reader.GetFacetHandler(name);
                if (handler == null || !(handler is SimpleFacetHandler))
                {
                    throw new InvalidOperationException("only simple facet handlers supported");
                }
                _facetHandlers.Add(handler);
                _facetHandlerMap.Add(name, handler);
            }
            _maxdoc = reader.MaxDoc;
        }

        public virtual FacetHandler NewInstance()
        {
            return new SimpleGroupbyFacetHandler(Name, _fieldsSet);
        }

        private class GroupbyFacetFieldComparator : FieldComparator
        {
            private int[] _docs;
            private IList<FieldComparator> _comparatorList;

            public GroupbyFacetFieldComparator(int numHits, IList<FieldComparator> comparatorList)
            {
                _docs = new int[numHits];
                _comparatorList = comparatorList;
            }

            public override int Compare(int slot1, int slot2)
            {
                foreach (var comparator in _comparatorList)
                {
                    var value = comparator.Compare(slot1, slot2);
                    if (value != 0)
                    {
                        return value;
                    }
                }
                return 0;
            }

            public override int CompareBottom(int doc)
            {
                foreach (var comparator in _comparatorList)
                {
                    var value = comparator.CompareBottom(doc);
                    if (value != 0)
                    {
                        return value;
                    }
                }
                return 0;
            }

            public override void Copy(int slot, int doc)
            {
                foreach (var comparator in _comparatorList)
                {
                    comparator.Copy(slot, doc);
                }
            }

            public override void SetBottom(int slot)
            {
                foreach (var comparator in _comparatorList)
                {
                    comparator.SetBottom(slot);
                }
            }

            public override void SetNextReader(Lucene.Net.Index.IndexReader reader, int docBase)
            {                
            }

            public override IComparable this[int slot]
            {
                get
                {
                    var sb = new StringBuilder();
                    foreach (var comparator in _comparatorList)
                    {
                        sb.Append(comparator[slot]);
                        sb.Append(",");
                    }
                    return sb.ToString();
                }
            }
        }

        private class GroupbyFacetCountCollector : IFacetCountCollector
        {
            private readonly DefaultFacetCountCollector[] _subcollectors;
            private readonly string _name;
            private readonly FacetSpec _fspec;
            private readonly int[] _count;
            private readonly int[] _lens;
            private readonly int _maxdoc;
            private readonly string _sep;

            public GroupbyFacetCountCollector(string name, FacetSpec fspec, DefaultFacetCountCollector[] subcollectors, int maxdoc, string sep)
            {
                _name = name;
                _fspec = fspec;
                _subcollectors = subcollectors;
                _sep = sep;
                int totalLen = 1;
                _lens = new int[_subcollectors.Length];
                for (int i = 0; i < _subcollectors.Length; ++i)
                {
                    _lens[i] = _subcollectors[i]._count.Length;
                    totalLen *= _lens[i];
                }
                _count = new int[totalLen];
                _maxdoc = maxdoc;
            }

            public void Collect(int docid)
            {
                int idx = 0;
                int i = 0;
                int segsize = _count.Length;
                foreach (DefaultFacetCountCollector subcollector in _subcollectors)
                {
                    segsize = segsize / _lens[i++];
                    idx += (subcollector._dataCache.orderArray.Get(docid) * segsize);
                }
                _count[idx]++;
            }

            public virtual void CollectAll()
            {
                for (int i = 0; i < _maxdoc; ++i)
                {
                    Collect(i);
                }
            }

            public virtual int[] GetCountDistribution()
            {
                return _count;
            }

            public virtual string Name
            {
                get
                {
                    return _name;
                }
            }

            public virtual BrowseFacet GetFacet(string @value)
            {
                string[] vals = @value.Split(new string[] { _sep }, StringSplitOptions.RemoveEmptyEntries);
                if (vals.Length == 0)
                    return null;
                StringBuilder buf = new StringBuilder();
                int startIdx = 0;
                int segLen = _count.Length;

                for (int i = 0; i < vals.Length; ++i)
                {
                    if (i > 0)
                    {
                        buf.Append(_sep);
                    }
                    int index = _subcollectors[i]._dataCache.valArray.IndexOf(vals[i]);
                    string facetName = _subcollectors[i]._dataCache.valArray.Get(index);
                    buf.Append(facetName);

                    segLen /= _subcollectors[i]._count.Length;
                    startIdx += index * segLen;
                }

                int count = _count[startIdx];
                for (int i = startIdx; i < startIdx + segLen; ++i)
                {
                    count += _count[i];
                }

                BrowseFacet f = new BrowseFacet(buf.ToString(), count);
                return f;
            }

            private string getFacetString(int idx)
            {
                StringBuilder buf = new StringBuilder();
                int i = 0;
                foreach (int len in _lens)
                {
                    if (i > 0)
                    {
                        buf.Append(_sep);
                    }

                    int adjusted = idx * len;

                    int bucket = adjusted / _count.Length;
                    buf.Append(_subcollectors[i]._dataCache.valArray.Get(bucket));
                    idx = adjusted % _count.Length;
                    i++;
                }
                return buf.ToString();
            }

            private object[] getRawFaceValue(int idx)
            {
                object[] retVal = new object[_lens.Length];
                int i = 0;
                foreach (int len in _lens)
                {
                    int adjusted = idx * len;
                    int bucket = adjusted / _count.Length;
                    retVal[i++] = _subcollectors[i]._dataCache.valArray.GetRawValue(bucket);
                    idx = adjusted % _count.Length;
                }
                return retVal;
            }

            private class GroupByFieldValueAccessor : IFieldValueAccessor
            {
                private GroupbyFacetCountCollector parent;

                public GroupByFieldValueAccessor(GroupbyFacetCountCollector parent)
                {
                    this.parent = parent;
                }

                public string GetFormatedValue(int index)
                {
                    return parent.getFacetString(index);
                }

                public object GetRawValue(int index)
                {
                    return parent.getRawFaceValue(index);
                }
            }

            public virtual IEnumerable<BrowseFacet> GetFacets()
            {
                if (_fspec != null)
                {
                    int minCount = _fspec.MinHitCount;
                    int max = _fspec.MaxCount;
                    if (max <= 0)
                        max = _count.Length;

                    FacetSpec.FacetSortSpec sortspec = _fspec.OrderBy;
                    List<BrowseFacet> facetColl;
                    if (sortspec == FacetSpec.FacetSortSpec.OrderValueAsc)
                    {
                        facetColl = new List<BrowseFacet>(max);
                        for (int i = 1; i < _count.Length; ++i) // exclude zero
                        {
                            int hits = _count[i];
                            if (hits >= minCount)
                            {
                                BrowseFacet facet = new BrowseFacet(getFacetString(i), hits);
                                facetColl.Add(facet);
                            }
                            if (facetColl.Count >= max)
                                break;
                        }
                    }
                    else
                    {
                        IComparatorFactory comparatorFactory;
                        if (sortspec == FacetSpec.FacetSortSpec.OrderHitsDesc)
                        {
                            comparatorFactory = new FacetHitcountComparatorFactory();
                        }
                        else
                        {
                            comparatorFactory = _fspec.CustomComparatorFactory;
                        }

                        if (comparatorFactory == null)
                        {
                            throw new System.ArgumentException("facet comparator factory not specified");
                        }

                        IComparer<int> comparator = comparatorFactory.NewComparator(new GroupByFieldValueAccessor(this), _count);
                        facetColl = new List<BrowseFacet>();
                        BoundedPriorityQueue<int> pq = new BoundedPriorityQueue<int>(comparator, max);

                        for (int i = 1; i < _count.Length; ++i) // exclude zero
                        {
                            int hits = _count[i];
                            if (hits >= minCount)
                            {
                                if (!pq.Offer(i))
                                {
                                    // pq is full. we can safely ignore any facet with <=hits.
                                    minCount = hits + 1;
                                }
                            }
                        }

                        while (!pq.IsEmpty)
                        {
                            int val = pq.DeleteMax();
                            BrowseFacet facet = new BrowseFacet(getFacetString(val), _count[val]);
                            facetColl.Add(facet);
                        }
                    }
                    return facetColl;
                }
                else
                {
                    return IFacetCountCollector_Fields.EMPTY_FACET_LIST;
                }
            }
        }
    }
}