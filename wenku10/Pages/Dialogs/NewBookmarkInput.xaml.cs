﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Net.Astropenguin.Loaders;

using wenku8.Model.Text;
using wenku8.Settings.Theme;

namespace wenku10.Pages.Dialogs
{
	sealed partial class NewBookmarkInput : ContentDialog
	{
		private IEnumerable<ColorItem> PresetColors;
		private Paragraph BookmarkTarget;

		public string AnchorName { get; private set; }
		public bool Canceled { get; private set; }

		private NewBookmarkInput()
		{
			this.InitializeComponent();
			Canceled = true;
			PresetColors = global::wenku8.System.ThemeManager.PresetColors();
		}

		public NewBookmarkInput( Paragraph P )
			:this()
		{
			BookmarkTarget = P;
			SetTemplate();
		}

		private void SetTemplate()
		{
			StringResources stx = new StringResources( "Message" );

			PrimaryButtonText = stx.Str( "OK" );
			SecondaryButtonText = stx.Str( "Cancel" );

			stx = new StringResources( "AppBar" );
			Title = stx.Text( "Bookmark" );

			stx = new StringResources( "AppResources" );
			BookmarkName.PlaceholderText = stx.Text( "DefaultToParagraph" );

			ColorGrid.ItemsSource = PresetColors;
		}

		private void ContentDialog_PrimaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			Canceled = false;
			ColorItem C = ColorGrid.SelectedItem as ColorItem;
			if ( C == null ) C = PresetColors.First();
			BookmarkTarget.AnchorColor = new SolidColorBrush( C.TColor );

			AnchorName = BookmarkName.Text.Trim();

			if( string.IsNullOrEmpty( AnchorName ) )
			{
				AnchorName = BookmarkTarget.Text.Trim();
			}

			if( string.IsNullOrEmpty( AnchorName ) )
			{
				AnchorName = "Bookmark - 1";
			}
		}

		private void ContentDialog_SecondaryButtonClick( ContentDialog sender, ContentDialogButtonClickEventArgs args )
		{
			Canceled = true;
		}

		internal void SetName( string bookmarkName )
		{
			BookmarkName.Text = bookmarkName;
		}
	}
}
