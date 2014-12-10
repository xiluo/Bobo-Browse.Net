﻿/**
 * Bobo Browse Engine - High performance faceted/parametric search implementation 
 * that handles various types of semi-structured data.  Written in Java.
 * 
 * Copyright (C) 2005-2006  John Wang
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 * 
 * To contact the project administrators for the bobo-browse project, 
 * please go to https://sourceforge.net/projects/bobo-browse/, or 
 * send mail to owner@browseengine.com.
 */

namespace BoboBrowse.Net.Index
{
    using BoboBrowse.Net.Facets;
    using BoboBrowse.Net.Facets.Filter;
    using BoboBrowse.Net.Index.Digest;
    using BoboBrowse.Net.Search;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Store;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class BoboIndexer
    {
        private Directory _index;
	    private DataDigester _digester;
	    private IndexWriter _writer;	
	    private Analyzer _analyzer;
	
	    private class MyDataHandler : DataDigester.IDataHandler
        {
		    private IndexWriter _writer;
		    public MyDataHandler(IndexWriter writer){
			    _writer=writer;
		    }
		    public void HandleDocument(Document doc) 
            {
			    _writer.AddDocument(doc);		
		    }
	    }

        public Analyzer Analyzer
        {
            get { return _analyzer == null ? new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_CURRENT) : _analyzer; }
            set { _analyzer = value; }
        }
	
	    public BoboIndexer(DataDigester digester,Directory index)
            : base()
        {
		    _index = index;
		    _digester = digester;
	    }	

	    public void Index() 
        {
		    bool create = !BoboIndexReader.IndexExists(_index);
		
		    _writer=null;
		    try
            {
			    _writer=new IndexWriter(_index, this.Analyzer, create, IndexWriter.MaxFieldLength.UNLIMITED);
			    MyDataHandler handler = new MyDataHandler(_writer);
			    _digester.Digest(handler);
			    _writer.Optimize();
		    }
		    finally
            {
			    if (_writer != null)
                {
				    _writer.Close();
			    }
		    }
	    }	
    }
}
