﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Storage;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

using wenku10.Pages;

namespace GR.PageExtensions
{
	using CompositeElement;
	using Data;
	using DataSources;
	using Database.Contexts;
	using Database.Models;
	using Model.Book;
	using Model.Book.Spider;
	using Model.ListItem;
	using Model.Loaders;
	using Model.Pages;
	using Model.Interfaces;
	using Resources;
	using Storage;

	sealed class BkProcPageExt : PageExtension, ICmdControls
	{
#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav => true;

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private BookSpiderVS ViewSource;

		private MenuFlyout ContextMenu;

		MenuFlyoutItem Reanalyze;
		MenuFlyoutItem Edit;
		MenuFlyoutItem Copy;
		MenuFlyoutItem DirectView;
		MenuFlyoutItem DeleteBtn;

		public BkProcPageExt( BookSpiderVS ViewSource )
			: base()
		{
			this.ViewSource = ViewSource;
		}

		public override void Unload()
		{
		}

		protected override void SetTemplate()
		{
			StringResources stx = new StringResources( "AppBar", "AppResources", "ContextMenu", "Resources" );
			ContextMenu = new MenuFlyout();

			Reanalyze = new MenuFlyoutItem() { Text = stx.Text( "Reanalyze", "ContextMenu" ) };
			Reanalyze.Click += Reanalyze_Click;
			ContextMenu.Items.Add( Reanalyze );

			Edit = new MenuFlyoutItem() { Text = stx.Text( "Edit", "ContextMenu" ) };
			Edit.Click += Edit_Click;
			ContextMenu.Items.Add( Edit );

			Copy = new MenuFlyoutItem() { Text = stx.Text( "Copy", "ContextMenu" ) };
			Copy.Click += Copy_Click;
			ContextMenu.Items.Add( Copy );

			DirectView = new MenuFlyoutItem() { Text = stx.Text( "DirectView", "ContextMenu" ) };
			DirectView.Click += DirectView_Click;
			ContextMenu.Items.Add( DirectView );

			DeleteBtn = new MenuFlyoutItem() { Text = stx.Text( "Delete", "ContextMenu" ) };
			DeleteBtn.Click += DeleteBtn_Click;
			ContextMenu.Items.Add( DeleteBtn );
		}

		private void Edit_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ControlFrame.Instance.NavigateTo( PageId.PROC_PANEL, () => new ProcPanelWrapper( ( ( SpiderBook ) Row.Source ).MetaLocation ) );
			}
		}

		private void Copy_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ViewSource.Copy( ( SpiderBook ) Row.Source );
			}
		}

		private async void Reanalyze_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				SpiderBook BkProc = ( SpiderBook ) Row.Source;

				if ( !BkProc.Processed && BkProc.CanProcess )
				{
					await ItemProcessor.ProcessLocal( BkProc );

					if( BkProc.GetBook().Packed == true )
					{
						new VolumeLoader( ( x ) => { } ).Load( BkProc.GetBook() );
					}
				}
			}
		}

		private void DirectView_Click( object sender, RoutedEventArgs e )
		{
			object DataContext = ( ( FrameworkElement ) sender ).DataContext;

			if ( DataContext is GRRow<IBookProcess> Row )
			{
				ControlFrame.Instance.SubNavigateTo( this, () => new DirectTextViewer( ( ( LocalBook ) Row.Source ).File ) );
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
				IBookProcess BkProc = Row.Source;

				Edit.Visibility = Visibility.Collapsed;
				Copy.Visibility = Visibility.Collapsed;
				DirectView.Visibility = Visibility.Collapsed;

				if( BkProc is SpiderBook )
				{
					Copy.Visibility = Visibility.Visible;
					Edit.Visibility = Visibility.Visible;
					DeleteBtn.IsEnabled = !BkProc.Processing;
				}
				else if( BkProc is LocalBook )
				{
					DirectView.Visibility = Visibility.Visible;
				}

				return ContextMenu;
			}
			return null;
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "ContextMenu" );

			SecondaryIconButton ImportSpider = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.OpenFile, stx.Text( "ImportSpider" ) );
			ImportSpider.Click += OpenSpider;

			Major2ndControls = new ICommandBarElement[] { ImportSpider };
		}

		public async void OpenSpider( object sender, RoutedEventArgs e )
		{
			IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
			if ( ISF == null ) return;

			var j = ViewSource.OpenSpider( ISF );
		}

		private async void PinItemToStart( object sender, RoutedEventArgs e )
		{
			SpiderBook B = ( SpiderBook ) ( ( FrameworkElement ) sender ).DataContext;
			if ( B.ProcessSuccess )
			{
				BookInstruction Book = B.GetBook();
				string TileId = await PageProcessor.PinToStart( Book );

				if ( !string.IsNullOrEmpty( TileId ) )
				{
					PinManager PM = new PinManager();
					PM.RegPin( Book, TileId, true );

					await PageProcessor.RegLiveSpider( B, Book, TileId );
				}
			}
		}
	}
}