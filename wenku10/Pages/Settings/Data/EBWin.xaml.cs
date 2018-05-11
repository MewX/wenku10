﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using GR.Model.ListItem;

namespace wenku10.Pages.Settings.Data
{
	using EBDictManager = global::GR.GSystem.EBDictManager;

	sealed partial class EBWin : Page
	{
		EBDictManager DictMgr;
		public EBWin()
		{
			this.InitializeComponent();
			SetTemplate();
		}

		private void SetTemplate()
		{
			DictMgr = new EBDictManager();

			LayoutRoot.DataContext = DictMgr;
		}

		private void OpenNewDict( object sender, RoutedEventArgs e )
		{
			DictMgr.OpenNewDict();
		}

		private void InstallNewDict( object sender, RoutedEventArgs e )
		{
			DictMgr.Install();
		}

		private void RemoveDict( object sender, RoutedEventArgs e )
		{
			DictMgr.Remove( ( sender as Button ).DataContext as ActiveItem );
		}
	}
}