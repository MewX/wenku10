﻿using System;
using System.Collections;
using System.Threading.Tasks;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku10;

namespace GR.Model.Loaders
{
	using Book;
	using Database.Models;
	using Ext;
	using GR.Settings;
	using Resources;
	using Text;

	using TransferInst = GR.AdvDM.WRuntimeTransfer.TransferInst;

	sealed class AutoCache : ActiveData, IAutoCache
	{
		public static readonly string ID = typeof( AutoCache ).Name;

		private const int AutoLimit = 2;
		private static int CurrentCount = 1;

		IRuntimeCache wCache;
		// FavItem fitm;
		BookItem ThisBook;
		EpisodeStepper ES;
		Action<BookItem> OnComplete;

		public string StatusText { get; private set; }

		public AutoCache( BookItem b, Action<BookItem> Handler )
			: this()
		{
			// fitm = f;
			ThisBook = b;
			OnComplete = Handler;

			StatusText = "Ready";
			if ( CurrentCount < AutoLimit )
			{
				wCache.InitDownload(
					ThisBook.ZItemId
					, X.Call<XKey[]>( XProto.WRequest, "GetBookTOC", ThisBook.ZItemId )
					, OnWCacheInfo, OnWCacheInfoFailed, false );
			}
		}

		private void OnWCacheInfo( DRequestCompletedEventArgs e, string id )
		{
			Shared.Storage.WriteString( ThisBook.TOCPath, Manipulation.PatchSyntax( Shared.TC.Translate( e.ResponseString ) ) );
			// Parse TOC
			throw new NotImplementedException();
			WRegisterEpRequests();
		}

		private async void WRegisterEpRequests()
		{
			ES = new EpisodeStepper( new VolumesInfo( ThisBook ) );

			if ( AutoLimit < CurrentCount )
			{
				DispLog( string.Format( "Error: Limit Reached {0}/{1}", CurrentCount - 1, AutoLimit ) );
				return;
			}

			throw new NotImplementedException();

			/*
			try
			{
				bool NotCached = false;
				for ( ES.Rewind(); ES.NextStepAvailable(); ES.StepNext() )
				{
					Chapter C = new Chapter( ES.EpTitle, ThisBook.Id, ES.Vid, ES.Cid );
					if ( !C.IsCached )
					{
						if ( !NotCached ) CurrentCount++;

						NotCached = true;
						// Register backgrountd transfer
						await Task.Delay( TimeSpan.FromMilliseconds( 80 ) );

						XKey[] Request = X.Call<XKey[]>( XProto.WRequest, "GetBookContent", ThisBook.Id, ES.Cid );

						DispLog( ES.VolTitle + "[" + ES.EpTitle + "]" );
						App.RuntimeTransfer.RegisterRuntimeThread(
							Request
							, C.ChapterPath, Guid.NewGuid()
							, Uri.EscapeDataString( ThisBook.Title ) + "&" + Uri.EscapeDataString( ES.VolTitle ) + "&" + Uri.EscapeDataString( ES.EpTitle )
							, C.IllustrationPath
						);
					}
				}
			}
			catch ( Exception ex )
			{
				global::System.Diagnostics.Debugger.Break();
				Logger.Log( ID, ex.Message, LogType.ERROR );
			}
			*/

			App.RuntimeTransfer.StartThreadCycle( WEpLoaded );
			DispLog( "Complete" );

			OnComplete( ThisBook );
		}

		public AutoCache()
		{
			wCache = X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, false, false );

			// This runs when thread is being aborted when app quit
			if ( App.RuntimeTransfer.CurrentThread != null )
			{
				Logger.Log( ID, "Resuming Download Sessions ...", LogType.INFO );
				App.RuntimeTransfer.StartThreadCycle( WEpLoaded );
			}
		}

		// Thread Complete Processor
		public static void WEpLoaded( DRequestCompletedEventArgs e, TransferInst PArgs )
		{
			throw new NotImplementedException();
			// new ContentParser().Parse( Shared.TC.Translate( e.ResponseString ), PArgs.ID, PArgs.cParam );
		}

		private void OnWCacheInfoFailed( string cacheName, string id, Exception ex )
		{
			if ( Shared.Storage.FileExists( ThisBook.TOCPath ) )
			{
				WRegisterEpRequests();
			}
		}

		private void DispLog( string p )
		{
			Logger.Log( ID, p, LogType.DEBUG );
			Worker.UIInvoke( () =>
			{
				StatusText = p;
				NotifyChanged( "StatusText" );
			} );
		}

		internal static void DownloadVolume( BookItem ThisBook, Volume Vol )
		{
			if ( ThisBook.IsSpider() )
			{
				int i = 0; int l = Vol.Chapters.Count;

				ChapterLoader Loader = null;
				Loader = new ChapterLoader( ThisBook, C => {
					throw new NotImplementedException();
					// C.UpdateStatus();

					if ( i < l )
					{
						Loader.Load( Vol.Chapters[ i++ ] );
					}
				} );

				Loader.Load( Vol.Chapters[ i++ ] );
				return;
			}


			Worker.ReisterBackgroundWork( () =>
			{
				string id = ThisBook.ZItemId;

				foreach ( Chapter c in Vol.Chapters )
				{
					if ( !string.IsNullOrEmpty( c.Content.Text ) )
					{
						throw new NotImplementedException();
						Logger.Log( ID, "Registering: " + c.Title, LogType.DEBUG );
						App.RuntimeTransfer.RegisterRuntimeThread(
							X.Call<XKey[]>( XProto.WRequest, "GetBookContent", id, c.Meta[ AppKeys.GLOBAL_CID ] )
							, "DEPRECATED_THING", Guid.NewGuid()
							, Uri.EscapeDataString( ThisBook.Title ) + "&" + Uri.EscapeDataString( Vol.Title ) + "&" + Uri.EscapeDataString( c.Title )
							, "DEPRECATED_PARAM"
						);
					}
				}

				throw new NotImplementedException();

				/*
				App.RuntimeTransfer.StartThreadCycle( ( a, b ) =>
				{
					WEpLoaded( a, b );
					Worker.UIInvoke( () => { foreach ( Chapter C in Vol.Chapters ) C.UpdateStatus(); } );
				} );
				*/

				App.RuntimeTransfer.ResumeThread();
			} );
		}
	}
}