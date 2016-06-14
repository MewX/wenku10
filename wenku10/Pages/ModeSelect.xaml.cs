﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Controls;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.Logging;

using wenku8.Config;
using wenku8.Effects;
using wenku8.Effects.Stage;
using wenku8.Effects.Stage.CircleParty;
using wenku8.Effects.Stage.RectangleParty;
using wenku8.System;

namespace wenku10.Pages
{
    public sealed partial class ModeSelect : Page
    {
        public static readonly string ID = typeof( ModeSelect ).Name;

        public bool ProtoUnLocked { get; private set; }

        private bool ModeSelected = false;
        Frame RootFrame;

        public ModeSelect()
        {
            InitializeComponent();
            NavigationHandler.InsertHandlerOnNavigatedBack( DisableBack );
            SetTemplate();
        }

        private void DisableBack( object sender, XBackRequestedEventArgs e )
        {
            e.Handled = true;
        }

        private void SinglePlayer( object sender, RoutedEventArgs e )
        {
            if ( ModeSelected ) return;
            ModeSelected = true;
            LoadingRing.IsActive = true;

            NavigationHandler.OnNavigatedBack -= DisableBack;

            var j = Dispatcher.RunIdleAsync( x => {
                MainStage.Instance.ClearNavigate( typeof( LocalModeTxtList ), "" );
            } );
        }

        private void SetTemplate()
        {
            RootFrame = MainStage.Instance.RootFrame;

            var j = Dispatcher.RunIdleAsync( x =>
            {
                BaumkuchenSecret( new Baumkuchen( ReactorRings ) );
            } );

            GetAnnouncements();
        }

        private void BaumkuchenSecret( Baumkuchen BCret )
        {
            int[] Sequence = new int[] { 1, 2, 3, 4, 5, 4, 3, 2, 3, 4, 5, 6, 7, 8 };

            int j = 0;
            int l = Sequence.Length;

            BCret.TouchHandler = ( i ) =>
            {
                if ( i == 1 ) j = 0;
                if ( i != Sequence[ j ] )
                {
                    j = 0;
                }
                else
                {
                    j++;
                    if( l == j )
                    {
                        BCret.TouchHandler = null;
                        BCret.Flush();
                        StartMultiplayer();
                    }
                }
            };
        }

        private void StartMultiplayer()
        {
            ForeText.Visibility = Visibility.Visible;

            LayoutRoot.Children.Remove( ForeText );

            MainStage.Instance.ObjectLayer.Children.Add( ForeText );

            OSenseText.Visibility = Visibility.Collapsed;

            Storyboard SB = SenseGround.Resources[ "Multiplayer" ] as Storyboard;
            SB.Begin();

            SB.Completed += ( x2, e2 ) => CoverTheWholeScreen();
        }

        private void CoverTheWholeScreen()
        {
            RectWaltzPrelude RP = new RectWaltzPrelude( MainStage.Instance.Canvas );
            RP.SetParty();
            RP.OnComplete( PlayForAMoment );
            RP.Play();
        }

        private void PlayForAMoment()
        {
            RectWaltzInterlude RP = new RectWaltzInterlude( MainStage.Instance.Canvas );
            RP.SetParty();
            RP.Play();

            TextTransition TextTrans = new TextTransition( t => SenseText.Text = t );
            TextTrans.SetTransation( SenseText.Text, "wenku8" );
            TextTrans.Play();

            TextTrans.OnComplete( GoMultiplayer );
        }

        private void GoMultiplayer()
        {
            NavigationHandler.OnNavigatedBack -= DisableBack;
            MainStage.Instance.ClearNavigate( typeof( MainPage ), "" );

            var j = Dispatcher.RunIdleAsync( x => DimTheWholeScreen() );
        }

        private void DimTheWholeScreen()
        {
            ScreenColorTransform SC = new ScreenColorTransform( MainStage.Instance.Canvas );
            SC.SetScreen(
                Properties.APPEARENCE_THEME_MAJOR_COLOR
                , ThemeManager.StringColor( "#FF0F65C0" )
            );

            SC.Play();

            SC.OnComplete( () =>
            {
                SC = new ScreenColorTransform( MainStage.Instance.Canvas );
                SC.SetScreen(
                    ThemeManager.StringColor( "#FF0F65C0" )
                    , ThemeManager.StringColor( "#00000000" )
                );

                SC.DisposeOnComplete = true;
                SC.Play();

                Storyboard sb = ForeText.Resources[ "FadeOut" ] as Storyboard;
                sb.Begin();
                sb.Completed += ( s, e ) =>
                {
                    MainStage.Instance.ObjectLayer.Children.Remove( ForeText );
                };

                TextTransition TextTrans = new TextTransition( t => SenseText.Text = t );
                TextTrans.SetDuration( 100 );
                TextTrans.SetTransation( SenseText.Text, "     " );
                TextTrans.Play();
            } );
        }

        private async void GetAnnouncements()
        {
            global::wenku8.Model.Loader.NewsLoader AS = new global::wenku8.Model.Loader.NewsLoader();
            await AS.Load();

            NewsLoading.IsActive = false;
            if ( AS.HasNewThings )
            {
                Storyboard sb = ( Storyboard ) NotiRect.Resources[ "Notify" ];
                sb?.Begin();
            }
        }

        private void ShowNews( object sender, RoutedEventArgs e ) { ShowNews(); }

        private async void ShowNews()
        {
            Dialogs.Announcements NewsDialog = new Dialogs.Announcements();
            await Popups.ShowDialog( NewsDialog );

            Storyboard sb = ( Storyboard ) NotiRect.Resources[ "Notify" ];
            sb?.Stop();
        }
    }
}
