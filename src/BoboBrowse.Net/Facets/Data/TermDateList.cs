
namespace BoboBrowse.Net.Facets.Data
{
    using Lucene.Net.Documents;
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    ///<summary>Internal data are stored in a long[] with values generated from <seealso cref="Date#getTime()"/> </summary>
	public class TermDateList : TermValueList<long>
	{
        public TermDateList()
        {
        }

        public TermDateList(string formatString)
		{
            this.FormatString = formatString;
		}

        public TermDateList(string formatString, IFormatProvider formatProvider)
        {
            this.FormatString = formatString;
            this.FormatProvider = formatProvider;
        }

		public TermDateList(int capacity, string formatString) 
            : base(capacity)
		{
            this.FormatString = formatString;
        }

        public TermDateList(int capacity, string formatString, IFormatProvider formatProvider)
            : base(capacity)
        {
            this.FormatString = formatString;
            this.FormatProvider = formatProvider;
        }

        public string FormatString { get; protected set; }
        public IFormatProvider FormatProvider { get; protected set; }

        private long Parse(string s)
        {
            if (s == null || s.Length == 0)
            {
                return 0L;
            }
            else
            {
                return DateTime.Parse(s, this.FormatProvider).ToBinary();
            }
        }

		public override void Add(string @value)
		{
            _innerList.Add(Parse(@value));
		}

		public override string Format(object o)
		{
            long val;
            if (o is string)
            {
                val = Parse(Convert.ToString(o));
            }
            else
            {
                val = Convert.ToInt64(o);
            }

            if (string.IsNullOrEmpty(this.FormatString))
            {
                return Convert.ToString(o);
            }
            else
            {
                if (this.FormatProvider == null)
                {
                    return DateTime.FromBinary(val).ToString(this.FormatString);
                }
                return DateTime.FromBinary(val).ToString(this.FormatString, this.FormatProvider);
            }
		}

		public override int IndexOf(object o)
		{
			long val = Parse((string)o);
            return _innerList.BinarySearch(val);
		}
	}
}