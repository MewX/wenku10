﻿using System;
using System.Threading.Tasks;
using System.Reflection;

using Net.Astropenguin.Logging;
using Net.Astropenguin.Logging.Handler;

using wenku10.SHHub;
using wenku10.Pages.Dialogs.GFlow;

namespace GR.GSystem
{
	using AdvDM;
	using Config;
	using Ext;
	using GFlow;
	using Model.REST;
	using Settings;
	using Storage;

	using ProcList = global::GFlow.Controls.GFProcedureList;
	using ResGFlow = global::GFlow.Resources.Shared;

	internal sealed class Bootstrap
	{
		public static readonly string ID = typeof( Bootstrap ).Name;

		public static FileSystemLog LogInstance;

		public static string Version = AppSettings.SimpVersion
#if DEBUG
			+ "d";
#elif TESTING
			+ "t";
#elif BETA
			+ "b";
#else
			+ "p";
#endif

		public async void Start()
		{
			X.Init();
			// Must follow Order!
			//// Fixed Orders
			// 1. Setting is the first to initialize
			AppSettingsInit();
			Logger.Log( ID, "Application Settings Initilizated", LogType.INFO );

			ActionCenter.Init();
			Logger.Log( ID, "ActionCenter Init", LogType.INFO );

			// Storage might already be initialized on Prelaunch
			if ( Resources.Shared.Storage == null )
			{
				// 2. following the appstorage, prepare directories.
				Resources.Shared.Storage = new GeneralStorage();
				Net.Astropenguin.IO.XRegistry.AStorage = Resources.Shared.Storage;
				Logger.Log( ID, "Shared.Storage Initilizated", LogType.INFO );
			}

			// SHRequest Init
			Resources.Shared.ShRequest = new SharersRequest(
				ONSSystem.Config.ServiceUri
				, Version
				, new string[] { "2.2.0t", "1.5.0b", "1.1.0p" } );

			// Connection Mode
			WHttpRequest.UA = string.Format( AppKeys.UA, Version );

			// Traslation API
			Resources.Shared.Conv = new Model.Text.TranslationAPI();
			await Resources.Shared.Conv.InitContextTranslator();
			await Resources.Shared.Conv.InitUITranslators();

			WCacheMode.Initialize();
			Logger.Log( ID, "WCacheMode Initilizated", LogType.INFO );
			//// End fixed orders

			// Shared Resources
			ResGFlow.SourceView = typeof( global::wenku10.Pages.ObjectViewer );
			ResGFlow.CreateRequest = x => new SHttpRequest( x ) { EN_UITHREAD = false };
			ProcList.Register( "EXTRACT", "/Resources:ProcEXTRACT", typeof( GrimoireExtractor ) );
			ProcList.Register( "MARK", "/Resources:ProcMARK", typeof( GrimoireMarker ) );
			ProcList.Register( "LIST", "/Resources:ProcLIST", typeof( GrimoireListLoader ) );
			ProcList.Register( "TRANSLATE", "/Resources:ProcTRANSLATE", typeof( TongWenTang ) );
			GrimoireExtractor.GRPropertyPage = typeof( EditProcExtract );
			GrimoireMarker.GRPropertyPage = typeof( EditProcMark );
			GrimoireListLoader.GRPropertyPage = typeof( EditProcListLoader );

			// Set Logger for libeburc
			EBDictManager.SetLogger();
		}

		private static bool L2 = false;
		public void Level2()
		{
			if ( L2 ) return;
			L2 = true;

			if ( X.Exists )
			{
				X.Instance<object>( XProto.LibStart );
			}
		}

		private void AppSettingsInit()
		{
			AppSettings.Initialize();
			if ( Properties.ENABLE_SYSTEM_LOG )
			{
				LogInstance = new FileSystemLog( FileLinks.ROOT_LOG + FileLinks.LOG_GENERAL );
			}

			// NetLog Re-initialize
			if ( Properties.ENABLE_RSYSTEM_LOG )
			{
				NetLog.Enabled = true;
				NetLog.RemoteIP = Properties.RSYSTEM_LOG_ADDRESS;
				NetLog.PostInit();
			}

			string Lang = Properties.LANGUAGE;
			// Language Override
			try
			{
				Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Lang;
			}
			catch ( Exception ) { }

#if DEBUG
			Logger.Log( ID, typeof( global::wenku10.App ).GetTypeInfo().Assembly.FullName, LogType.INFO );
#endif
			Logger.Log( ID, string.Format( "Device Info: {0} / {1}", AppSettings.DeviceName, AppSettings.DeviceId ), LogType.INFO );
			Logger.Log( ID, string.Format( "Language is {0}", Lang ), LogType.INFO );
		}
	}
}
