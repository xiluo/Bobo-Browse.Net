namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

	public class TermShortList : TermNumberList<short>
	{
		public TermShortList() 
            : base()
		{}

		public TermShortList(string formatString) 
            : base(formatString)
		{}

        public TermShortList(string formatString, IFormatProvider formatProvider)
            : base(formatString, formatProvider)
        {}

		public TermShortList(int capacity, string formatString) 
            : base(capacity, formatString)
		{}

        public TermShortList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity, formatString, formatProvider)
        { }

        private short Parse(string s)
		{
			if (s==null || s.Length == 0)
			{
				return (short)0;
			}
			else
			{
				return Convert.ToInt16(s);
			}
		}

        public override void Add(string o)
        {
            _innerList.Add(Parse(o));
        }

		public override int IndexOf(object o)
		{
            short val = short.Parse((string)o, CultureInfo.InvariantCulture);
            return _innerList.BinarySearch(val);
		}

		protected override object ParseString(string o)
		{
			return Parse(o);
		}
	}
}