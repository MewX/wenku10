﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.StartScreen;

using Net.Astropenguin.Loaders;

using wenku8.CompositeElement;
using wenku8.Effects;
using wenku8.Model.Book.Spider;
using wenku8.Model.Interfaces;
using wenku8.Model.ListItem;
using wenku8.Model.Pages;
using wenku8.Model.Section;
using wenku8.Resources;
using wenku8.Storage;

namespace wenku10.Pages
{
	sealed partial class BookSpidersView : Page, ICmdControls, IAnimaPage, INavPage
	{
		#pragma warning disable 0067
		public event ControlChangedEvent ControlChanged;
		#pragma warning restore 0067

		public bool NoCommands { get; }
		public bool MajorNav { get { return true; } }

		public IList<ICommandBarElement> MajorControls { get; private set; }
		public IList<ICommandBarElement> Major2ndControls { get; private set; }
		public IList<ICommandBarElement> MinorControls { get; private set; }

		private BookSpiderList FileListContext;
		private SpiderBook SelectedBook;

		public BookSpidersView()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		public async void Parse( SpiderBook Book )
		{
			FileListContext.ImportItem( Book );
			await ItemProcessor.ProcessLocal( Book );
		}

		#region Anima
		Storyboard AnimaStory = new Storyboard();

		public async Task EnterAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 0, 1 );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 30, 0 );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}

		public async Task ExitAnima()
		{
			AnimaStory.Stop();
			AnimaStory.Children.Clear();

			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot, "Opacity", 1, 0, 350, 0, Easings.EaseInCubic );
			SimpleStory.DoubleAnimation( AnimaStory, LayoutRoot.RenderTransform, "Y", 0, 30, 350, 0, Easings.EaseInCubic );

			AnimaStory.Begin();
			await Task.Delay( 350 );
		}
		#endregion

		public void SoftOpen()
		{
			if ( FileListContext == null )
			{
				FileListContext = new BookSpiderList();
				LayoutRoot.DataContext = FileListContext;
			}
			else
			{
				FileListContext.Reload();
			}
		}

		public void SoftClose() { }

		private void SetTemplate()
		{
			InitAppBar();
			LayoutRoot.RenderTransform = new TranslateTransform();
		}

		private void InitAppBar()
		{
			StringResources stx = new StringResources( "ContextMenu" );

			SecondaryIconButton ImportSpider = UIAliases.CreateSecondaryIconBtn( SegoeMDL2.OpenFile, stx.Text( "ImportSpider" ) );
			ImportSpider.Click += ( s, e ) => FileListContext.OpenSpider();

			Major2ndControls = new ICommandBarElement[] { ImportSpider };
		}

		private void ShowBookAction( object sender, RightTappedRoutedEventArgs e )
		{
			LSBookItem G = ( LSBookItem ) sender;
			FlyoutBase.ShowAttachedFlyout( G );

			SelectedBook = ( SpiderBook ) G.DataContext;
		}

		private void ToggleFav( object sender, RoutedEventArgs e )
		{
			SelectedBook.ToggleFav();
		}

		private void RemoveSource( object sender, RoutedEventArgs e )
		{
			try
			{
				SelectedBook.RemoveSource();
			}
			catch ( Exception ) { }

			FileListContext.CleanUp();
		}

		private async void CopySource( object sender, RoutedEventArgs e )
		{
			FileListContext.Add( await SelectedBook.Clone() );
		}

		private void EditSource( object sender, RoutedEventArgs e )
		{
			EditItem( SelectedBook );
		}

		private async void Reanalyze( object sender, RoutedEventArgs e )
		{
			await SelectedBook.Reload();
			await ItemProcessor.ProcessLocal( SelectedBook );
		}

		private void FileList_ItemClick( object sender, ItemClickEventArgs e )
		{
			SpiderBook Item = ( SpiderBook ) e.ClickedItem;
			// Prevent double processing on the already processed item
			if ( !Item.ProcessSuccess && Item.CanProcess )
			{
				// Skip awaiting because ProcessSuccess will handle if skip
				var j = ItemProcessor.ProcessLocal( Item );
			}

			if ( Item.ProcessSuccess )
			{
				ControlFrame.Instance.NavigateTo( PageId.BOOK_INFO_VIEW, () => new BookInfoView( Item.GetBook() ) );
			}
		}

		private async void PinItemToStart( object sender, RoutedEventArgs e )
		{
			if( SelectedBook.ProcessSuccess )
			{
				BookInstruction Book = SelectedBook.GetBook();
				string TileId = await PageProcessor.PinToStart( Book );

				if ( !string.IsNullOrEmpty( TileId ) )
				{
					PinManager PM = new PinManager();
					PM.RegPin( Book, TileId, true );

					await PageProcessor.RegLiveSpider( SelectedBook, Book, TileId );
				}

			}
		}

		private void TextBox_TextChanging( TextBox sender, TextBoxTextChangingEventArgs args )
		{
			FileListContext.SearchTerm = sender.Text.Trim();
		}

		private void EditItem( IMetaSpider LB )
		{
			ControlFrame.Instance.NavigateTo( PageId.PROC_PANEL, () => new ProcPanelWrapper( LB.MetaLocation ) );
		}

	}
}
