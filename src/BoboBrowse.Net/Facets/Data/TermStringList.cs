namespace BoboBrowse.Net.Facets.Data
{
    using System;
    using System.Collections.Generic;

    public class TermStringList : TermValueList<string>
    {
        public override void Add(string o)
        {
            if (o == null)
            {
                o = "";
            }
            _innerList.Add(o);
        }

        public override bool Contains(object o)
        {
            return IndexOf(o) >= 0;
        }

        public override string Format(object o)
        {
            return Convert.ToString(o);
        }

        public override int IndexOf(object o)
        {
            return _innerList.BinarySearch(Convert.ToString(o), StringComparer.Ordinal);
        }
    }
}
