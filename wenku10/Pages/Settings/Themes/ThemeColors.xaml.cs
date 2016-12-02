﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.UI.Icons;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku8.CompositeElement;
using wenku8.Config;
using wenku8.Model.Interfaces;
using wenku8.Settings.Theme;
using Net.Astropenguin.Controls;

namespace wenku10.Pages.Settings.Themes
{
    public sealed partial class ThemeColors : Page, INavPage, ICmdControls
    {
        public static readonly string ID = typeof( ThemeColors ).Name;

        #pragma warning disable 0067
        public event ControlChangedEvent ControlChanged;
        #pragma warning restore 0067

        public bool NoCommands { get; }
        public bool MajorNav { get; }

        public IList<ICommandBarElement> MajorControls { get; private set; }
        public IList<ICommandBarElement> Major2ndControls { get; private set; }
        public IList<ICommandBarElement> MinorControls { get; private set; }

        private ObservableCollection<ThemeSet> PresetThemeColors;
        private ThemeSet SelectedTheme;
        private global::wenku8.System.ThemeManager Manager;

        public Visibility IsSystemSet
        {
            get
            {
                return SelectedTheme == null
                  ? Visibility.Collapsed
                  : SelectedTheme.IsSystemSet;
            }
        }

        public void SoftOpen() { NavigationHandler.InsertHandlerOnNavigatedBack( CloseThemesetFrame ); }
        public void SoftClose() { NavigationHandler.OnNavigatedBack -= CloseThemesetFrame; }

        private void CloseThemesetFrame( object sender, XBackRequestedEventArgs e )
        {
            if( ThemeSetFrame.Content != null )
            {
                e.Handled = true;
                ThemeSetFrame.Content = null;
            }
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            Logger.Log( ID, string.Format( "OnNavigatedTo: {0}", e.SourcePageType.Name ), LogType.INFO );
            if ( SelectedTheme != null )
            {
                if ( Presets.SelectedItem != SelectedTheme )
                {
                    Presets.SelectedItem = SelectedTheme;
                }
                else SetThemeBlocks( SelectedTheme );
            }
        }

        public ThemeColors()
        {
            this.InitializeComponent();
            SetTemplates();
        }

        private void Presets_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count() < 1 ) return;
            SetThemeBlocks( e.AddedItems[ 0 ] as ThemeSet );
        }

        private void SetTemplates()
        {
            Manager = new global::wenku8.System.ThemeManager();
            ThemePresets();
            InitAppBar();
        }

        private void InitAppBar()
        {
            StringResources stx = new StringResources( "AppBar" );

            List<ICommandBarElement> Btns = new List<ICommandBarElement>();

            if ( Properties.ENABLE_ONEDRIVE )
            {
                AppBarButtonEx OneDriveBtn = UIAliases.CreateAppBarBtnEx( new IconOneDrive(), stx.Text( "Sync" ) );

                ButtonOperation Op = new ButtonOperation( OneDriveBtn );
                Op.SetOp( Manager.OneDriveSync );
                Op.SetComplete( ThemePresets );

                Btns.Add( OneDriveBtn );
            }

            AppBarButton SaveBtn = UIAliases.CreateAppBarBtn( Symbol.Save, stx.Text( "Save" ) );
            SaveBtn.Click += SaveBtn_Click;

            Btns.Add( SaveBtn );

            MajorControls = Btns.ToArray();
        }

        private void SetThemeBlocks( ThemeSet ColorSet )
        {
            List<ThemeTextBlock> ThemeBlocks = new List<ThemeTextBlock>();

            Type TypeInfo = typeof( ThemeSet );

            // Major Color Schemes
            PutThemeBlocks(
                ThemeBlocks
                , new string[] {
                    "APPEARENCE_THEME_MAJOR_COLOR"
                    , "APPEARENCE_THEME_MINOR_COLOR"
                    , "APPEARENCE_THEME_TEXT_COLOR_RELATIVE_TO_BACKGROUND"
                    , "APPEARENCE_THEME_SUBTLE_TEXT_COLOR"
                }
                , new string[]
                {
                    "APPEARENCE_THEME_MAJOR_BACKGROUND_COLOR"
                    , "APPEARENCE_THEME_MINOR_BACKGROUND_COLOR"
                }
                , ColorSet
            );

            // Major Text Schemes
            PutThemeBlocks(
                ThemeBlocks
                , new string[] {
                    "APPEARENCE_THEME_TEXT_COLOR_RELATIVE_TO_MAJOR"
                }
                , new string[]
                {
                    "APPEARENCE_THEME_MAJOR_COLOR"
                    , "APPEARENCE_THEME_MINOR_COLOR"
                }
                , ColorSet
            );

            // Shades Major Schemes
            PutThemeBlocks(
                ThemeBlocks
                , new string[] {
                    "APPEARENCE_THEME_TEXT_COLOR_RELATIVE_TO_MAJOR"
                }
                , new string[]
                {
                    "APPEARENCE_THEME_MAJOR_COLOR"
                    , "APPEARENCE_THEME_MINOR_COLOR"
                    , "APPEARENCE_THEME_HORIZONTAL_RIBBON_COLOR"
                    , "APPEARENCE_THEME_VERTICAL_RIBBON_COLOR"
                }
                , ColorSet
            );

            ThemeView.ItemsSource = ThemeBlocks;
            ThemeView.SelectedItem = ThemeBlocks;
            ViewShades( ThemeBlocks[ 0 ] );
        }

        private void ThemePresets()
        {
            StringResources stx = new StringResources( "Settings" );
            PresetThemeColors = new ObservableCollection<ThemeSet>(
                Manager.GetThemes()
            );

            Presets.ItemsSource = PresetThemeColors;

            // Current Color Set
            List<Color> CurrentColors = new List<Color>();

            Type P = typeof( Properties );
            foreach( KeyValuePair<string, string> Map in ThemeSet.ParamMap )
            {
                PropertyInfo PInfo = P.GetProperty( Map.Value );
                CurrentColors.Add( ( Color ) PInfo.GetValue( null ) );
            }

            SetThemeBlocks(
                new ThemeSet( "CurrentSet", false, CurrentColors.ToArray() )
            );
        }

        private void PutThemeBlocks( List<ThemeTextBlock> Blocks, string[] FGs, string[] BGs, ThemeSet ColorSet )
        {
            foreach ( string FG in FGs )
            {
                foreach ( string BG in BGs )
                {
                    Blocks.Add( new ThemeTextBlock( FG, BG, ColorSet ) );
                }
            }
        }

        private void ViewShades( ThemeTextBlock ThemeBlock )
        {
            List<ThemeTextBlock> ShadeBlocks = new List<ThemeTextBlock>();

            for( int j = 1; j < 10; j ++ )
            {
                ThemeTextBlock Block = new ThemeTextBlock(
                    ThemeBlock.FGName, ThemeBlock.BGName, ThemeBlock.ColorSet
                );
                Block.Shades( j * 10 );
                ShadeBlocks.Add( Block );

                if ( 4 < j )
                {
                    ThemeTextBlock BlockR = new ThemeTextBlock(
                        "APPEARENCE_THEME_RELATIVE_SHADES_COLOR", ThemeBlock.BGName, ThemeBlock.ColorSet
                    );
                    BlockR.Shades( j * 10 );
                    ShadeBlocks.Add( BlockR );
                }
            }

            ShadesView.ItemsSource = ShadeBlocks;
        }

        private void ThemeView_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            if ( e.AddedItems.Count() < 1 ) return;

            ViewShades( e.AddedItems[0] as ThemeTextBlock );
        }

        private async void SaveBtn_Click( object sender, RoutedEventArgs e )
        {
            // Save the selected set since in time this will be unselected and cause problem
            ThemeSet ThisSet = Presets.SelectedItem as ThemeSet;
            if ( ThisSet == null ) return;

            if ( !await MainSettings.Instance.ConfirmRestart( "Appearance_Theme" ) ) return;

            ThisSet.Apply();
        }

        private void ThemeEdit( object sender, RoutedEventArgs e )
        {
            ThemeSetFrame.Content = new EditColors( SelectedTheme );
        }

        private void ThemeDelete( object sender, RoutedEventArgs e )
        {
            Manager.RemoveTheme( SelectedTheme.Name );
            PresetThemeColors.Remove( SelectedTheme );
        }

        private async void ThemeRename( object sender, RoutedEventArgs e )
        {
            string OName = SelectedTheme.Name;
            Dialogs.Rename R = new Dialogs.Rename( SelectedTheme );
            await Popups.ShowDialog( R );
            if ( R.Canceled ) return;

            Manager.Save( SelectedTheme );
            Manager.Remove( OName );
        }

        private void ThemeCopy( object sender, RoutedEventArgs e )
        {
            StringResources stx = new StringResources( "Settings" );
            PresetThemeColors.Add(
                Manager.CopyTheme(
                    SelectedTheme
                    , stx.Text( "Appearance_Theme_ColorSet" )
                    + " " + DateTime.Now.ToString()
                )
            );
        }

        private void ThemeContextMenu( object sender, RightTappedRoutedEventArgs e )
        {
            StackPanel ThemeItem = sender as StackPanel;
            if ( ThemeItem == null ) return;

            FlyoutBase.ShowAttachedFlyout( ThemeItem );
            FlyoutBase ThemeContext = FlyoutBase.GetAttachedFlyout( ThemeItem );

            SelectedTheme = ThemeItem.DataContext as ThemeSet;
        }
    }
}
