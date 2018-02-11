﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using wenku10.Pages;

namespace GR.PageExtensions
{
	using AdvDM;
	using CompositeElement;
	using Data;
	using DataSources;
	using Database.Contexts;
	using Database.Models;
	using Model.ListItem;
	using Model.Loaders;
	using Model.Pages;
	using Model.Interfaces;
	using Resources;
	using Storage;
	using Net.Astropenguin.Linq;

	sealed class TextDocPageExt : PageExtension, ICmdControls
	{
		public readonly string ID = typeof( TextDocPageExt ).Name;

#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => false;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private bool Terminate = true;
		private bool Processing = false;
		private LocalBook[] PTargets;

		private TextDocVS ViewSource;

		private MenuFlyout ContextMenu;

		MenuFlyoutItem Reanalyze;
		MenuFlyoutItem DirectView;
		MenuFlyoutItem DeleteBtn;

		AppBarButton ProcessBtn;

		public TextDocPageExt( TextDocVS ViewSource )
			: base()
		{
			this.ViewSource = ViewSource;
		}

		public override void Unload()
		{
		}

		public async void ProcessItem( IGRRow obj )
		{
			if ( obj is GRRow<IBookProcess> Row )
			{
				await ItemProcessor.ProcessLocal( ( LocalBook ) Row.Source );
			}
		}

		protected override void SetTemplate()
		{
			InitAppBar();

			StringResources stx = new StringResources( "ContextMenu" );
			ContextMenu = new MenuFlyout();

			Reanalyze = new MenuFlyoutItem() { Text = stx.Text( "Reanalyze" ) };
			Reanalyze.Click += Reanalyze_Click;
			ContextMenu.Items.Add( Reanalyze );

			DirectView = new MenuFlyoutItem() { Text = stx.Text( "DirectView" ) };
			DirectView.Click += DirectView_Click;
			ContextMenu.Items.Add( DirectView );

			DeleteBtn = new MenuFlyoutItem() { Text = stx.Text( "Delete" ) };
			DeleteBtn.Click += DeleteBtn_Click;
			ContextMenu.Items.Add( DeleteBtn );
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "AppBar" );

			AppBarButton OpenFolder = UIAliases.CreateAppBarBtn( SegoeMDL2.OpenLocal, stx.Text( "OpenFolder" ) );
			OpenFolder.Click += ( s, e ) => ViewSource.BSData.OpenDirectory();

			AppBarButton OpenUrl = UIAliases.CreateAppBarBtn( SegoeMDL2.Link, stx.Text( "OpenUrl" ) );
			OpenUrl.Click += OpenUrl_Click;

			ProcessBtn = UIAliases.CreateAppBarBtn( Symbol.Play, stx.Text( "ProcessAll" ) );
			ProcessBtn.Click += ProcessAll;

			MajorControls = new ICommandBarElement[] { OpenFolder, OpenUrl, ProcessBtn };
		}

		private async void ProcessAll( object sender, RoutedEventArgs e )
		{
			StringResources stx = new StringResources( "AppBar", "AdvDM", "AppResources" );

			if ( Processing )
			{
				Terminate = !Terminate;
			}
			else
			{
				Terminate = false;
			}

			if ( Terminate )
			{
				ProcessBtn.Label = stx.Text( "ProcessAll" );
				ProcessBtn.Icon = new SymbolIcon( Symbol.Play );
				PTargets?
					.Where( x => !x.Processing && x.File != null )
					.ExecEach( x => x.Desc = stx.Text( "Ready", "AppResources" ) );
			}
			else
			{
				ProcessBtn.Label = stx.Text( "Pause" );
				ProcessBtn.Icon = new SymbolIcon( Symbol.Pause );
				PTargets?
					.Where( x => !x.Processing && x.File != null )
					.ExecEach( x => x.Desc = stx.Text( "Waiting", "AdvDM" ) );
			}

			if ( Processing ) return;
			Processing = true;

			PTargets = ( ( GRTable<IBookProcess> ) ViewSource.BSData.Table ).Items
				.Where( x =>
				{
					LocalBook Bk = ( LocalBook ) x.Source;
					return Bk.CanProcess && !Bk.Processed;
				} )
				.Remap( x =>
				{
					LocalBook Bk = ( LocalBook ) x.Source;
					Bk.Desc = stx.Text( "Waiting", "AdvDM" );
					return Bk;
				} );

			if ( PTargets.Any() )
			{

				if ( await Shared.TC.ConfirmTranslate( "__ALL__", "All" ) )
				{
					Shared.TC.SetPrefs( PTargets );
				}

				foreach ( LocalBook b in PTargets )
				{
					await ItemProcessor.ProcessLocal( b );
					if ( Terminate ) break;
				}
			}

			ProcessBtn.Label = stx.Text( "ProcessAll" );
			ProcessBtn.Icon = new SymbolIcon( Symbol.Play );
			PTargets = null;
			Terminate = false;
			Processing = false;
		}

		private async void OpenUrl_Click( object sender, RoutedEventArgs e )
		{
			StringResources stx = new StringResources( "AdvDM" );

			DownloadBookContext Context = new DownloadBookContext();
			wenku10.Pages.Dialogs.Rename UrlBox = new wenku10.Pages.Dialogs.Rename( Context, stx.Text( "Download_Location" ) )
			{
				Placeholder = "http://example.com/NN. XXXX.txt"
			};

			await Popups.ShowDialog( UrlBox );

			if ( UrlBox.Canceled ) return;

			ViewSource.IsLoading = true;

			RuntimeCache rCache = new RuntimeCache();
			ViewSource.Message = string.Format( "{0}. {1}", Context.Id, Context.Title );
			ViewSource.Message = stx.Text( "Active" );

			rCache.GET( Context.Url, ( DArgs, url ) =>
			{
				SaveTemp( DArgs, Context );

				ViewSource.Message = null;
				ViewSource.IsLoading = false;
			}
			, ( id, url, ex ) =>
			{
				Logger.Log( ID, ex.Message, LogType.WARNING );

				ViewSource.Message = "Cannot download: " + id;
				ViewSource.IsLoading = false;
			}, false );
		}

		private async void SaveTemp( DRequestCompletedEventArgs e, DownloadBookContext Context )
		{
			StorageFile ISF = await AppStorage.MkTemp(
				Context.Id
				+ ". "
				+ ( string.IsNullOrEmpty( Context.Title ) ? "[ Parse Needed ]" : Context.Title )
				+ ".txt"
			);

			await ISF.WriteBytes( e.ResponseBytes );

			ViewSource.BSData.ImportItem( new LocalBook( ISF ) );
		}

		private async void Reanalyze_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				await ItemProcessor.ProcessLocal( ( LocalBook ) Row.Source );
			}
		}

		private void DirectView_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ControlFrame.Instance.SubNavigateTo( Page, () => new DirectTextViewer( ( ( LocalBook ) Row.Source ).File ) );
			}
		}

		private void DeleteBtn_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ViewSource.Delete( Row );
			}
		}

		public override FlyoutBase GetContextMenu( FrameworkElement elem )
		{
			if ( elem.DataContext is GRRow<IBookProcess> Row )
			{
				LocalBook BkProc = ( LocalBook ) Row.Source;

				DirectView.IsEnabled = ( BkProc.File != null );
				Reanalyze.IsEnabled = BkProc.CanProcess && !( BkProc.Processed || BkProc.Processing );

				return ContextMenu;
			}
			return null;
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