﻿using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

namespace wenku8.Settings.Layout
{
    using Resources;
    using Settings.Layout.ModuleThumbnail;

    class BookInfoView
    {
        public static readonly string ID = typeof( BookInfoView ).Name;

        private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_BOOKINFOVIEW;
        private const string RightToLeft = "RightToLeft";
        private const string HrTOCName = "HorizontalTOC";

        private Dictionary<string, BgContext> SectionBgs;

        public bool IsRightToLeft
        {
            get
            {
                return LayoutSettings.GetParameter( RightToLeft ).GetBool( "enable" );
            }
            set
            {
                LayoutSettings.SetParameter( RightToLeft, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        public bool HorizontalTOC
        {
            get
            {
                return LayoutSettings.GetParameter( HrTOCName ).GetBool( "enable" );
            }
            set
            {
                LayoutSettings.SetParameter( HrTOCName, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        private ListView Disp = null;
        private XRegistry LayoutSettings;

        private XParameter[] Modules
        {
            get { return LayoutSettings.GetParametersWithKey( "order" ); }
        }

        private Type[] LayoutDefs = new Type[]
        {
            typeof( ModuleThumbnail.InfoView )
            , typeof( ModuleThumbnail.Reviews )
            , typeof(  ModuleThumbnail.TOCView )
        };

        private Dictionary<string, ThumbnailBase> TBInstance;

        public BookInfoView()
        {
            LayoutSettings = new XRegistry( AppKeys.TS_CXML, TFileName );
            SectionBgs = new Dictionary<string, BgContext>();
            InitParams();
        }

        public BookInfoView( ListView DisplayList )
            : this()
        {
            Disp = DisplayList;
            DisplayList.DragItemsCompleted += OnReorder;
        }

        ~BookInfoView()
        {
            if ( Disp != null )
            {
                Net.Astropenguin.Helpers.Worker.UIInvoke(
                    () => Disp.DragItemsCompleted -= OnReorder
                );
            }
        }

        public void InitParams()
        {
            TBInstance = new Dictionary<string, ThumbnailBase>();

            int i = 0;

            bool Changed = false;

            // Get the last available index
            if ( Modules != null )
            {
                foreach ( XParameter P in Modules )
                {
                    int j = int.Parse( P.GetValue( "order" ) );
                    if ( i < j ) i = j;
                }
            }

            if ( LayoutSettings.GetParameter( RightToLeft ) == null )
            {
                LayoutSettings.SetParameter(
                    RightToLeft
                    , new XKey( "enable", Shared.LocaleDefaults.Get<bool>( "BookInfoView.IsRightToLeft" ) )
                );
            }

            if ( LayoutSettings.GetParameter( HrTOCName ) == null )
            {
                LayoutSettings.SetParameter(
                    HrTOCName
                    , new XKey( "enable", Shared.LocaleDefaults.Get<bool>( "BookInfoView.HorizontalTOC" ) )
                );
            }

            // Create Index Item if not available
            foreach ( Type P in LayoutDefs )
            {
                ThumbnailBase Tb = Activator.CreateInstance( P ) as ThumbnailBase;
                TBInstance.Add( Tb.ModName, Tb );

                XParameter LayoutKey = LayoutSettings.GetParameter( Tb.ModName );
                if ( LayoutKey == null )
                {
                    LayoutSettings.SetParameter(
                        Tb.ModName, new XKey[] {
                            new XKey( "order", ++i )
                            , new XKey( "enable", Tb.DefaultValue )
                        }
                    );

                    Changed = true;
                }
            }

            if ( Changed ) LayoutSettings.Save();
        }

        public BgContext GetBgContext( string Section )
        {
            if ( SectionBgs.ContainsKey( Section ) ) return SectionBgs[ Section ];

            BgContext b = new BgContext( LayoutSettings, Section );

            return SectionBgs[ Section ] = b; ;
        }

        public void SetOrder()
        {
            List<ThumbnailBase> Thumbnails = new List<ThumbnailBase>();

            IEnumerable<XParameter> Params = Modules.OrderBy(
                ( x ) => x.GetSaveInt( "order" )
            );

            foreach ( XParameter Param in Params )
            {
                if ( !Param.GetBool( "enable" ) ) continue;

                Disp.Items.Add( TBInstance[ Param.ID ] );
            }
        }

        public List<string> GetViewOrders()
        {
            List<string> Names = new List<string>();
            foreach (
                XParameter P in Modules
                    .Where( ( x ) => x.GetBool( "enable" ) )
                    .OrderBy( ( x ) => x.GetSaveInt( "order" ) )
            ) {
                Names.Add( TBInstance[ P.ID ].ViewName );
            }

            return Names;
        }

        public void Remove( string Name )
        {
            Disp.Items.Remove(
                Disp.Items.First( ( x ) => ( x as ThumbnailBase ).ModName == Name )
            );

            LayoutSettings.SetParameter( Name, new XKey( "enable", false ) );
            LayoutSettings.Save();
        }

        public void Insert( string Name )
        {
            if ( LayoutSettings.GetParameter( Name ).GetBool( "enable" ) ) return;

            int Index = LayoutSettings.GetParameter( Name ).GetSaveInt( "order" );
            IEnumerable<XParameter> Params = Modules.OrderBy(
                ( x ) => -x.GetSaveInt( "order" )
            );

            int InsertIdx = 0;
            foreach ( XParameter Param in Params )
            {
                if ( !Param.GetBool( "enable" ) ) continue;
                if ( Param.GetSaveInt( "order" ) <= Index )
                {
                    InsertIdx = Disp.Items.IndexOf(
                        TBInstance[ Param.ID ]
                    ) + 1;
                    break;
                }
            }

            Disp.Items.Insert( InsertIdx, TBInstance[ Name ] );

            LayoutSettings.SetParameter( Name, new XKey( "enable", true ) );
            LayoutSettings.Save();
        }

        public bool Toggle( string Name )
        {
            return LayoutSettings.GetParameter( Name ).GetBool( "enable" );
        }

        private void OnReorder( ListViewBase sender, DragItemsCompletedEventArgs args )
        {
            int InsertIdx = 0;
            // Give orders to the enabled first
            foreach ( object Inst in Disp.Items )
            {
                ThumbnailBase Inste = ( ThumbnailBase ) Inst;
                Logger.Log( ID, string.Format( "Order: {0} => {1}", InsertIdx, Inste.ModName ), LogType.DEBUG );

                LayoutSettings.SetParameter(
                    Inste.ModName, new XKey( "order", ++InsertIdx )
                );
            }

            // Then the disables
            IEnumerable<XParameter> Params = Modules.Where(
                ( XParameter x ) => !x.GetBool( "enable" )
            );

            foreach ( XParameter Param in Params )
            {
                Param.SetValue( new XKey( "order", ++InsertIdx ) );
                LayoutSettings.SetParameter( Param );
            }

            LayoutSettings.Save();
        }


        /// <summary>
        /// Background Context Object, Controls section backgrounds
        /// </summary>
        internal class BgContext : ActiveData
        {
            XRegistry LayoutSettings;

            private ImageSource bg, bg2;
            public ImageSource Background
            {
                get { return bg; }
                private set
                {
                    bg = value;
                    NotifyChanged( "Background" );
                }
            }
            public ImageSource Background2
            {
                get { return bg2; }
                private set
                {
                    bg2 = value;
                    NotifyChanged( "Background2" );
                }
            }

            private bool bgs = false, bgs2 = false;
            public bool BGState
            {
                get { return bgs; }
                private set
                {
                    bgs = value;
                    NotifyChanged( "BGState" );
                }
            }
            public bool BGState2
            {
                get { return bgs2; }
                private set
                {
                    bgs2 = value;
                    NotifyChanged( "BGState2" );
                }
            }
            public string Section { get; private set; }

            private bool SwState = false;

            public BgContext( XRegistry LayoutSettings, string Section )
            {
                this.LayoutSettings = LayoutSettings;

                this.Section = Section;
            }

            public async void ApplyBackgrounds()
            {
                XParameter P = LayoutSettings.GetParameter( Section );

                // Default value
                if ( P == null )
                {
                    SetBackground( "System" );
                    return;
                }

                string value = P.GetValue( "value" );
                if ( value == null ) return;

                switch ( P.GetValue( "type" ) )
                {
                    case "Custom":
                        IStorageFolder isf = await AppStorage.FutureAccessList.GetFolderAsync( value );
                        if ( isf == null ) return;

                        // Randomly pick an image
                        string[] Acceptables = new string[] { ".JPG", ".PNG", ".GIF" };
                        IEnumerable<IStorageFile> sfs = await isf.GetFilesAsync();

                        sfs = sfs.TakeWhile( x => Acceptables.Contains( x.FileType.ToUpper() ) );
                        int l = System.Utils.Rand.Next( sfs.Count() );

                        int i = 0;

                        IStorageFile Choice = null;
                        foreach ( IStorageFile f in sfs )
                        {
                            Choice = f;
                            if ( i++ == l ) break;
                        }

                        if ( Choice == null ) return;

                        // Copy this file to temp storage
                        await Choice.CopyAsync(
                            await Shared.Storage.CreateDirFromISOStorage( FileLinks.ROOT_BANNER )
                            , Section + ".image", NameCollisionOption.ReplaceExisting );

                        BitmapImage b = await Image.NewBitmap();
                        b.SetSourceFromUrl( FileLinks.ROOT_BANNER + Section + ".image" );
                        UpdateImage( b );
                        break;
                    case "Preset":
                        break;
                    default:
                    case "System":
                        UpdateImage( await Image.NewBitmap( new Uri( value, UriKind.Absolute ) ) );
                        break;
                }
            }

            public async void SetBackground( string type )
            {
                XParameter SecParam = LayoutSettings.GetParameter( Section );
                if ( SecParam == null ) SecParam = new XParameter( Section );

                string value = null;
                switch ( type )
                {
                    case "Custom":
                        IStorageFolder Location = await PickDirFromPicLibrary();
                        if ( Location == null ) return;

                        value = SecParam.GetValue( "value" );
                        if ( value == null ) value = Guid.NewGuid().ToString();

                        AppStorage.FutureAccessList.AddOrReplace( value, Location );

                        break;
                    case "Preset":
                        break;
                    case "System":
                        switch ( Section )
                        {
                            case "TOC":
                                value = "ms-appx:///Assets/Samples/BgTOC.jpg";
                                break;
                            case "INFO_VIEW":
                                value = "ms-appx:///Assets/Samples/BgInfoView.jpg";
                                break;
                        }

                        break;
                }

                SecParam.SetValue( new XKey[] {
                    new XKey( "type", type )
                    , new XKey( "value", value )
                } );

                LayoutSettings.SetParameter( SecParam );

                ApplyBackgrounds();
                LayoutSettings.Save();
            }

            private async Task<IStorageFolder> PickDirFromPicLibrary()
            {
                return await AppStorage.OpenDirAsync( x =>
                {
                    x.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                    x.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                } );
                ;
            }

            private void UpdateImage( BitmapImage b )
            {
                Action<BitmapImage> Front = async x =>
                {
                    if ( BGState = ( x != null ) )
                    {
                        Background = x;
                        BGState2 = false;
                        await Task.Delay( 1000 );
                        Image.Destroy( Background2 );
                    }
                };

                Action<BitmapImage> Back = async x =>
                {
                    if ( BGState2 = ( x != null ) )
                    {
                        Background2 = x;
                        BGState = false;
                        await Task.Delay( 1000 );
                        Image.Destroy( Background );
                    }
                };

                if ( SwState = !SwState ) Back = Front;

                // Show the back
                Back( b );
            }
        }
    }
}
