namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;

	public class TermCharList : TermValueList<char>
	{

		private char Parse(string s)
		{
			return string.IsNullOrEmpty(s) ? (char)0 : s[0];
		}

		public TermCharList() : base()
		{
		}

		public TermCharList(int capacity) : base(capacity)
		{
		}

		public override void Add(string o)
		{
            _innerList.Add(Parse(o));
		}

		public override int IndexOf(object o)
		{
			char val = Parse((string)o);
            return _innerList.BinarySearch(val);
		}

		public override string Format(object o)
		{
			return Convert.ToString(o);
		}
	}
}