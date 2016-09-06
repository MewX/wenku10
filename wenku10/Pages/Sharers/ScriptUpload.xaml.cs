﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;

using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.AdvDM;
using wenku8.Model.Book;
using wenku8.Model.ListItem;
using wenku8.Model.ListItem.Sharers;
using wenku8.Model.REST;
using wenku8.Resources;
using CryptAES = wenku8.System.CryptAES;
using AESManager = wenku8.System.AESManager;
using TokenManager = wenku8.System.TokenManager;

namespace wenku10.Pages.Sharers
{
    sealed partial class ScriptUpload : Page
    {
        public static readonly string ID = typeof( ScriptUpload ).Name;

        private SpiderBook SelectedBook;
        private AESManager AESMgr;
        private TokenManager TokMgr;
        private Action<string,string> OnExit;

        private string ReservedId;
        private bool LockedFile = false;
        private volatile bool Uploading = false;

        public ScriptUpload()
        {
            this.InitializeComponent();
            SetTemplate();
        }

        public ScriptUpload( HubScriptItem HSI, Action<string, string> OnExit )
            :this()
        {
            // Set Update template
            ReservedId = HSI.Id;

            Anon.IsChecked = string.IsNullOrEmpty( HSI.AuthorId );

            Encrypt.IsChecked = HSI.Encrypted;
            ForceCommentEnc.IsChecked = HSI.ForceEncryption;

            NameInput.Text = HSI.Name;
            DescInput.Text = HSI.Desc;

            ZoneInput.Text = string.Join( ", ", HSI.Zone );
            TypesInput.Text = string.Join( ", ", HSI.Type );
            TagsInput.Text = string.Join( ", ", HSI.Tags );

            AddToken_Btn.IsEnabled
                = AddKey_Btn.IsEnabled
                = ForceCommentEnc.IsEnabled
                = Encrypt.IsEnabled
                = Anon.IsEnabled
                = Keys.IsEnabled
                = AccessTokens.IsEnabled
                = false;

            PredefineFile( HSI.Id );

            this.OnExit = OnExit;
        }

        public ScriptUpload( Action<string,string> OnExit )
            :this()
        {
            this.OnExit = OnExit;
        }

        public ScriptUpload( BookItem Book, Action<string, string> OnExit )
            : this()
        {
            LockedFile = true;
            Book.PropertyChanged += Book_PropertyChanged;
            PredefineFile( Book.Id );
            this.OnExit = OnExit;
        }

        private void Book_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            BookItem B = ( BookItem ) sender;
            switch( e.PropertyName )
            {
                case "Title":
                    NameInput.PlaceholderText = B.Title;
                    if ( string.IsNullOrEmpty( NameInput.Text ) ) NameInput.Text = B.Title;
                    break;
                case "Intro":
                    DescInput.PlaceholderText = B.Intro;
                    if ( string.IsNullOrEmpty( DescInput.Text ) ) DescInput.Text = B.Intro;
                    break;
                case "Press":
                    ZoneInput.PlaceholderText = B.PressRaw;
                    if ( string.IsNullOrEmpty( ZoneInput.Text ) ) ZoneInput.Text = B.PressRaw;
                    break;
            }
        }

        private async void PredefineFile( string Id )
        {
            SelectedBook = await SpiderBook.CreateAsyncSpider( Id );
            FileName.Text = SelectedBook.MetaLocation;
        }

        private void SetTemplate()
        {
            AESMgr = new AESManager();
            AESMgr.PropertyChanged += KeyMgr_PropertyChanged;
            TokMgr = new TokenManager();
            TokMgr.PropertyChanged += TokMgr_PropertyChanged;

            Keys.DataContext = AESMgr;
            AccessTokens.DataContext = TokMgr;

            StringResources stx = new StringResources();
            FileName.Text = stx.Text( "PickAFile" );
        }

        private void KeyMgr_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( e.PropertyName == "SelectedItem" ) Keys.SelectedItem = AESMgr.SelectedItem;
        }

        private void TokMgr_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( e.PropertyName == "SelectedItem" ) AccessTokens.SelectedItem = TokMgr.SelectedItem;
        }

        private void PreSelectKey( object sender, RoutedEventArgs e )
        {
            if( string.IsNullOrEmpty( ReservedId ) )
            {
                Keys.SelectedItem = AESMgr.SelectedItem;
            }
            else
            {
                Keys.SelectedValue = AESMgr.GetAuthById( ReservedId )?.Value;
            }
        }

        private void PreSelectToken( object sender, RoutedEventArgs e )
        {
            if ( string.IsNullOrEmpty( ReservedId ) )
            {
                AccessTokens.SelectedItem = TokMgr.SelectedItem;
            }
            else
            {
                AccessTokens.SelectedValue = TokMgr.GetAuthById( ReservedId )?.Value;
            }
        }

        private async void Upload( object sender, RoutedEventArgs e )
        {
            if ( MarkUpload() ) return;

            CryptAES Crypt = null;

            // Validate inputs
            try
            {
                Message.Text = "";
                if ( !Shared.Storage.FileExists( SelectedBook.PSettings.Location ) )
                {
                    SelectedBook = null;
                    FileName.Text = "---------";
                }

                if ( SelectedBook == null )
                    throw new ValidationError( "VL_NoBook" );

                if ( Encrypt.IsChecked == true )
                {
                    Crypt = Keys.SelectedItem as CryptAES;

                    if ( Crypt == null )
                        throw new ValidationError( "VL_NoKey" );
                }

                if ( AccessTokens.SelectedItem == null )
                    throw new ValidationError( "VL_NoToken" );
            }
            catch ( ValidationError ex )
            {
                StringResources stx = new StringResources( "Error" );

                Message.Text = stx.Str( ex.Message );
                MarkNotUpload();
                return;
            }

            // Check whether the script uuid is reserved
            NameValue<string> Token = ( NameValue<string> ) AccessTokens.SelectedItem;
            if ( string.IsNullOrEmpty( ReservedId ) )
            {
                ReservedId = await ReserveId( Token.Value );
            }

            string Id = ReservedId;
            string Name = NameInput.Text.Trim();
            if ( string.IsNullOrEmpty( Name ) )
                Name = NameInput.PlaceholderText;

            string Desc = DescInput.Text.Trim();
            if ( string.IsNullOrEmpty( Id ) )
            {
                Message.Text = "Failed to reserve id";
                return;
            }

            string Zone = ZoneInput.Text;
            string[] Types = TypesInput.Text.Split( ',' );
            string[] Tags = TagsInput.Text.Split( ',' );

            SelectedBook.AssignId( Id );
            string Data = SelectedBook.PSettings.ToString();

            if ( Crypt != null ) Data = Crypt.Encrypt( Data );

            new RuntimeCache().POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ScriptUpload(
                    Token.Value, Id
                    , Data, Name, Desc
                    , Zone, Types, Tags
                    , Encrypt.IsChecked == true
                    , ForceCommentEnc.IsChecked == true
                    , Anon.IsChecked == true )
                , ( Res, QueryId ) =>
                {
                    try
                    {
                        JsonStatus.Parse( Res.ResponseString );
                        TokMgr.AssignId( Token.Name, Id );
                        if ( Crypt != null ) AESMgr.AssignId( Crypt.Name, Id );

                        var j = Dispatcher.RunIdleAsync( ( x ) => { OnExit( Id, Token.Value ); } );
                    }
                    catch ( Exception ex )
                    {
                        ServerMessage( ex.Message );
                    }

                    MarkNotUpload();
                }
                , ( c1, c2, ex ) =>
                {
                    ServerMessage( ex.Message );
                    MarkNotUpload();
                }
                , false
            );
        }

        private async void PickFile( object sender, RoutedEventArgs e )
        {
            if ( LockedFile ) return;

            Message.Text = "";
            IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
            if ( ISF == null ) return;

            LoadingRing.IsActive = true;

            try
            {
                SelectedBook = await SpiderBook.ImportFile( await ISF.ReadString(), false );
                if ( !SelectedBook.CanProcess )
                {
                    StringResources stx = new StringResources( "ERROR" );
                    throw new InvalidDataException( stx.Str( "HS_INVALID" ) );
                }

                FileName.Text = ISF.Name;
                int LDot = ISF.Name.LastIndexOf( '.' );
                NameInput.PlaceholderText = ~LDot == 0 ? ISF.Name : ISF.Name.Substring( 0, LDot );
            }
            catch ( Exception ex )
            {
                Message.Text = ex.Message;
            }

            LoadingRing.IsActive = false;
        }

        public async Task<string> ReserveId( string AccessToken )
        {
            TaskCompletionSource<string> TCS = new TaskCompletionSource<string>();

            RuntimeCache RCache = new RuntimeCache();
            RCache.POST(
                Shared.ShRequest.Server
                , Shared.ShRequest.ReserveId( AccessToken )
                , ( e, QueryId ) =>
                {
                    try
                    {
                        JsonObject JDef = JsonStatus.Parse( e.ResponseString );
                        string Id = JDef.GetNamedString( "data" );
                        TCS.SetResult( Id );
                    }
                    catch( Exception ex )
                    {
                        Logger.Log( ID, ex.Message, LogType.WARNING );
                        TCS.TrySetResult( null );
                    }
                }
                , ( cache, Id, ex ) =>
                {
                    Logger.Log( ID, ex.Message, LogType.WARNING );
                    TCS.TrySetResult( null );
                }
                , false
            );

            return await TCS.Task;
        }

        private void ServerMessage( string Mesg )
        {
            var j = Dispatcher.RunIdleAsync( ( x ) => { Message.Text = Mesg; } );
        }

        private void AddKey( object sender, RoutedEventArgs e ) { AESMgr.NewAuth(); }
        private void AddToken( object sender, RoutedEventArgs e ) { TokMgr.NewAuth(); }

        private bool MarkUpload()
        {
            if ( Uploading ) return true;
            Uploading = true;

            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                LoadingRing.IsActive = true;
                Upload_Btn.IsEnabled = false;
            } );

            return false;
        }

        private void MarkNotUpload()
        {
            Uploading = false;
            var j = Dispatcher.RunIdleAsync( ( x ) =>
            {
                LoadingRing.IsActive = false;
                Upload_Btn.IsEnabled = true;
            } );
        }

        private class ValidationError : Exception { public ValidationError( string Mesg ) : base( Mesg ) { } }
    }
}