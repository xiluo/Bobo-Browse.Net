namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class TermFloatList : TermNumberList<float>
    {
        public TermFloatList()
            : base()
        { }

        public TermFloatList(string formatString)
            : base(formatString)
        { }

        public TermFloatList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        {}

        public TermFloatList(int capacity, string formatString)
            : base(capacity, formatString)
        { }

        public TermFloatList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }

        private float Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0.0f;
            }
            else
            {
                return NumericUtils.PrefixCodedToFloat(s);
            }
        }

        public override void Add(string @value)
        {
            _innerList.Add(Parse(@value));
        }

        public override int IndexOf(object o)
        {
            float val = float.Parse((string)o, CultureInfo.InvariantCulture);
            return _innerList.BinarySearch(val);
        }

        protected override object ParseString(string o)
        {
            return Parse(o);
        }
    }
}