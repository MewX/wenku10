﻿using System;
using System.Net;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

namespace wenku8.Model.Loaders
{
    using AdvDM;
    using Ext;
    using Book;
    using Book.Spider;
    using ListItem;
    using Resources;
    using Settings;
    using System;
    using System.Messages;

    class BookLoader : IBookLoader
    {
        public static readonly string ID = typeof( BookLoader ).Name;

        private BookItem CurrentBook;

        private Action<BookItem> CompleteHandler;

        public BookLoader( Action<BookItem> Handler )
        {
            CompleteHandler = Handler;
        }

        public void Load( BookItem b, bool useCache = false )
        {
            CurrentBook = b;

            if( b.IsLocal )
            {
                OnComplete( b );
                return;
            }

            if ( b is BookInstruction )
            {
                LoadInstruction( b as BookInstruction, useCache );
                return;
            }

            string id = b.Id;
            string Mode = X.Const<string>( XProto.WProtocols, "ACTION_BOOK_INFO" );

            if( CurrentBook.XTest( XProto.BookItemEx ) )
            {
                Mode = CurrentBook.XField<string>( "Mode" );
            }

            XKey[] ReqKeys = X.Call<XKey[]>( XProto.WRequest, "DoBookAction", Mode, id );
            if ( useCache )
            {
                string cacheName = X.Call<string>( XProto.WRuntimeCache, "GetCacheString", new object[] { ReqKeys } );
                if ( Shared.Storage.FileExists( FileLinks.ROOT_CACHE + cacheName ) )
                {
                    ExtractBookInfo( Shared.Storage.GetString( FileLinks.ROOT_CACHE + cacheName ), id );
                    return;
                }
            }

            X.Instance<IRuntimeCache>( XProto.WRuntimeCache )
                .InitDownload( id, ReqKeys, PrelaodBookInfo, PrelaodBookInfo, true );
        }

        public async void LoadInstruction( BookInstruction B, bool useCache )
        {
            if ( useCache && Shared.Storage.FileExists( B.TOCPath ) )
            {
                await CacheCover( B );
                OnComplete( B );
                return;
            }

            if ( !B.Packable )
            {
                SpiderBook SBook = new SpiderBook( B );
                await SBook.Process();
            }

            await CacheCover( B );

            B.PackVolumes();
            OnComplete( B );
        }

        public void LoadIntro( BookItem b, bool useCache = true )
        {
            if ( b is BookInstruction ) return;

            CurrentBook = b;
            // Description
            if ( Shared.Storage.FileExists( b.IntroPath ) )
            {
                CurrentBook.Intro = "OK";
            }
            else
            {
                X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
                    b.Id
                    , X.Call<XKey[]>( XProto.WRequest, "GetBookIntro", b.Id )
                    , SaveIntro, IntroFailed, useCache
                );
            }
        }

        private void IntroFailed( string arg1, string arg2, Exception arg3 )
        {
            CurrentBook.Intro = new ErrorMessage().DOWNLOAD;
        }

        private void SaveIntro( DRequestCompletedEventArgs e, string id )
        {
            Shared.Storage.WriteString( CurrentBook.IntroPath, e.ResponseString );
            CurrentBook.Intro = "OK";
        }

        private void PrelaodBookInfo( string cacheName, string id, Exception ex )
		{
			// This method is called when download is failed.
			Logger.Log( ID, "Download failed: " + cacheName, LogType.INFO );
			// Check if cache exist
			cacheName = Uri.EscapeDataString( cacheName );
			if ( Shared.Storage.FileExists( FileLinks.ROOT_CACHE + cacheName ) )
			{
				// Should inform user would using previous cache as data.
				ExtractBookInfo( Shared.Storage.GetString( FileLinks.ROOT_CACHE + cacheName ) , id );
				// MessageBox.Show( "Some information could not be downloaded, using previous cache." );
			}
			else
			{
				// Download failed and no cache is available.
				// Inform user there is a network problem
				// MessageBox.Show( "Some information could not be downloaded, please try again later." );
				OnComplete( null );
			}
		}

		private void PrelaodBookInfo( DRequestCompletedEventArgs e, string id )
		{
			// When download is successful
			ExtractBookInfo( e.ResponseString, id );
		}

        private void ExtractBookInfo( string InfoData, string id )
        {
            ////// App-specific approach
            CurrentBook.ParseXml( InfoData );

            if ( !Shared.Storage.FileExists( CurrentBook.CoverPath ) )
            {
                ///// App-specific approach
                X.Instance<IRuntimeCache>( XProto.WRuntimeCache ).InitDownload(
                    id, X.Call<XKey[]>( XProto.WRequest, "GetBookCover", id )
                    , CoverDownloaded, Utils.DoNothing, false
                );
            }
            else
            {
                SetCover( CurrentBook );
                // Cover cached immediately. Call once
                OnComplete( CurrentBook );
            }
            ////////// Active informations: Can not store in AppCache
        }

        private async Task CacheCover( BookItem B )
        {
            if( Shared.Storage.FileExists( CurrentBook.CoverPath ) )
            {
                SetCover( B );
                return;
            }

            if( !string.IsNullOrEmpty( B.CoverSrcUrl ) )
            {
                TaskCompletionSource<int> Awaitable = new TaskCompletionSource<int>();

                // Set the referer, as it is required by some site such as fanfiction.net
                new RuntimeCache( a => {
                    HttpRequest R = new WHTTPRequest( a );
                    R.EN_UITHREAD = true;

                    if ( !string.IsNullOrEmpty( B.OriginalUrl ) )
                    {
                        R.RequestHeaders[ HttpRequestHeader.Referer ] = B.OriginalUrl;
                    }

                    return R;
                } ).GET( new Uri( B.CoverSrcUrl ), ( a, b ) => {
                    CoverDownloaded( a, b );
                    Awaitable.TrySetResult( 0 );
                }
                // Failed handler
                , ( a, b, c ) => {
                    Awaitable.TrySetResult( 0 );
                }, false );

                await Awaitable.Task;
            }
        }

        private void CoverDownloaded( DRequestCompletedEventArgs e, string id )
        {
            // Write Cache
            Shared.Storage.WriteBytes( CurrentBook.CoverPath, e.ResponseBytes );
            // Read Image
            SetCover( CurrentBook );
            // Cover cached. Call once
            OnComplete( CurrentBook );
        }

        private void SetCover( BookItem B )
        {
            Worker.UIInvoke( () =>
            {
                BitmapImage bmp = new BitmapImage();
                bmp.SetSourceFromUrl( B.CoverPath );

                B.Cover = bmp;
            } );
        }

        // Loading itself is resources intensive
        // But dispatching itself is not
        private void OnComplete( BookItem b )
        {
            Worker.UIInvoke( () => { CompleteHandler( b ); } );
        }
    }
}
