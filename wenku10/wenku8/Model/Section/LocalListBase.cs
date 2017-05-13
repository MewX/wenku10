﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;

namespace wenku8.Model.Section
{
	using ListItem;
	using Net.Astropenguin.Linq;
	using Pages;
	using Resources;
	using Settings;
	using Storage;

	class LocalListBase : SearchableContext
	{
		public bool FavOnly { get; protected set; }

		private string _loading = null;
		public string Loading
		{
			get { return _loading; }
			protected set
			{
				_loading = value;
				NotifyChanged( "Loading" );
			}
		}

		public bool Processing { get; protected set; }

		private bool _term = true;
		public bool Terminate
		{
			get { return _term; }
			set
			{
				_term = value;
				NotifyChanged( "Terminate" );
			}
		}

		public async void ProcessAll()
		{
			if ( Processing || SearchSet == null ) return;
			Processing = true;
			Terminate = false;

			LocalBook[] Books = SearchSet.Cast<LocalBook>().ToArray();

			if ( await Shared.TC.ConfirmTranslate( "__ALL__", "All" ) )
			{
				Shared.TC.SetPrefs( Books );
			}

			NotifyChanged( "Processing" );
			foreach ( LocalBook b in Books )
			{
				await ItemProcessor.ProcessLocal( b );
				if ( Terminate ) break;
			}
			Terminate = true;
			Processing = false;
			NotifyChanged( "Processing" );
		}

		public LocalBook GetById( string Id )
		{
			return Data?.Cast<LocalBook>().FirstOrDefault( x => x.aid == Id );
		}

		protected override IEnumerable<ActiveItem> Filter( IEnumerable<ActiveItem> Items )
		{
			if ( Items != null && FavOnly )
			{
				string[] ids = new BookStorage().GetIdList();
				Items = Items.Where( x => ids.Contains( ( x as LocalBook ).aid ) );
			}

			return base.Filter( Items );
		}

		public void CleanUp()
		{
			Data = Data.Where( x =>
			{
				LocalBook b = x as LocalBook;
				return b.CanProcess || ( FavOnly && b.IsFav ) || ( b.Processed && b.ProcessSuccess );
			} );

			NotifyChanged( "SearchSet" );
		}

		public async Task ToggleFavs()
		{
			if ( !FavOnly )
			{
				BookStorage BS = new BookStorage();
				string[] BookIds = BS.GetIdList();

				List<ActiveItem> SS = new List<ActiveItem>();

				foreach ( string Id in BookIds )
				{
					if ( Data != null && Data.Any( x => ( x as LocalBook ).aid == Id ) )
					{
						continue;
					}

					LocalBook LB = await LocalBook.CreateAsync( Id );
					if ( !( LB.CanProcess || LB.ProcessSuccess ) )
					{
						XParameter Param = BS.GetBook( Id );
						LB.Name = Param.GetValue( AppKeys.GLOBAL_NAME );
						LB.Desc = "Source is unavailable";
						LB.CanProcess = false;
					}

					LB.IsFav = true;
					SS.Add( LB );
				}

				if ( 0 < SS.Count )
				{
					if ( Data == null ) Data = SS;
					else Data = Data.Concat( SS );
				}

				FavOnly = true;
			}
			else
			{
				FavOnly = false;
				if ( Data != null )
				{
					Data = Data.Where( x =>
					{
						LocalBook LB = x as LocalBook;
						if ( LB.IsFav ) return LB.ProcessSuccess || LB.Processing || LB.CanProcess;

						return true;
					} );
				}
			}

			NotifyChanged( "SearchSet" );
		}

		public class DownloadBookContext : INamable
		{
			private Uri _url;

			public Regex Re = new Regex( @"https?://.+/(\d+)\.([\w\W]+\.)?txt$" );

			public Uri Url { get { return _url; } }

			public string Id { get; private set; }
			public string Title { get; private set; }

			public string Name
			{
				get { return _url == null ? "" : _url.ToString(); }
				set
				{
					Match m = Re.Match( value );
					if ( !m.Success )
					{
						throw new Exception( "Invalid Url" );
					}
					else
					{
						Id = m.Groups[ 1 ].Value;
						Title = m.Groups[ 2 ].Value;
					}

					_url = new Uri( value );
				}
			}
		}

	}
}