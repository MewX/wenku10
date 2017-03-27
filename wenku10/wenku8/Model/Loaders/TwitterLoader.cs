﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.Services.Twitter;

using Net.Astropenguin.Linq;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Loaders
{
	using ListItem;

	sealed class TwitterLoader : ILoader<Tweet>
	{
		public static readonly string ID = typeof( TwitterLoader ).Name;

		public List<NameValue<bool>> Tags { get; set; }
		public string Keyword = "";

		public Action<IList<Tweet>> Connector { get; set; }

		public int CurrentPage { get { return 0; } }

		private bool _PageEnded = false;
		public bool PageEnded { get { return _PageEnded; } }

		private string MaxId;

		public async Task<IList<Tweet>> NextPage( uint count )
		{
			try
			{
				TwitterDataConfig TDC = new TwitterDataConfig();
				TDC.QueryType = TwitterQueryType.Search;
				TDC.Query = NextQuery();

				List<Tweet> Tweets = await TwitterService.Instance.RequestAsync( TDC, ( int ) count );

				int l = Tweets.Count();
				_PageEnded = l < count;

				if ( 0 < l )
				{
					ulong tId = ulong.Parse( Tweets.Last().Id ) - 1;
					MaxId = "max_id:" + tId.ToString();
				}

				return Tweets.ToList();
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.WARNING );
			}

			_PageEnded = true;
			return new Tweet[ 0 ];
		}

		private string NextQuery()
		{
			string TagsQ = Tags.Any( x => x.Value ) ? "#" + string.Join( " #", Tags.Where( x => x.Value ).Remap( x => x.Name ) ) : "";
			string PageQ = MaxId + " ";

			return ( Keyword + " " + TagsQ + " " + PageQ ).Trim() + " -filter:retweets";
		}

	}
}