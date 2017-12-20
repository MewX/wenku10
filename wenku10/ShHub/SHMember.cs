﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.Logging;

using GR.AdvDM;
using GR.Ext;
using GR.Resources;
using GR.Settings;
using GR.Model.REST;
using GR.Model.Section.SharersHub;
using GR.GSystem;

namespace wenku10.SHHub
{
	sealed class SHMember : IMember
	{
		public static readonly string ID = typeof( SHMember ).Name;

		private RuntimeCache RCache = new RuntimeCache();

		public event TypedEventHandler<object, MemberStatus> OnStatusChanged;

		public bool CanRegister { get { return true; } }
		public bool IsLoggedIn { get; private set; }
		public bool WillLogin { get; private set; }
		public string Id { get; private set; }

		public MemberStatus Status { get; set; }

		public string CurrentAccount { get; set; }
		public string CurrentPassword { get; set; }

		public Activities Activities { get; private set; }

		private LoginInfo Remember;
		private LoginInfo LastAuth;

		public string ServerMessage
		{
			get; private set;
		}

		private XRegistry AuthReg;

		public SHMember()
		{
			WillLogin = false;

			AuthReg = new XRegistry( "<SHAuth />", FileLinks.ROOT_SETTING + FileLinks.SH_AUTH_REG );

			this.Activities = new Activities();

			XParameter MemberAuth = AuthReg.Parameter( "member-auth" );
			if ( MemberAuth != null ) RestoreAuth( MemberAuth );
		}

		public async Task<bool> Register()
		{
			Pages.Dialogs.Sharers.Register RegisterDialog = new Pages.Dialogs.Sharers.Register();
			await Popups.ShowDialog( RegisterDialog );
			if ( RegisterDialog.Canceled ) return false;

			var j = Popups.ShowDialog( new Pages.Dialogs.Login( this ) );
			return true;
		}

		public async void Login( string Account, string Password, bool Remember = false )
		{
			if ( WillLogin ) return;
			WillLogin = true;

			CurrentAccount = Account;

			if ( Remember )
				this.Remember = await CredentialVault.Protect( this, Account, Password );

			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.Login( Account, Password )
				, LoginResponse, LoginFailed
				, false
			);
		}

		public void Logout()
		{
			IsLoggedIn = false;
			Remember = null;
			LastAuth = null;
			new CredentialVault().Remove( this );

			UpdateStatus( MemberStatus.LOGGED_OUT );

			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.Logout()
				, ClearAuth, ClearAuth
				, false
			);
		}

		private void UpdateStatus( MemberStatus Status )
		{
			this.Status = Status;
			if ( OnStatusChanged != null )
			{
				Worker.UIInvoke( () => OnStatusChanged( this, Status ) );
			}
		}

		private void LoginResponse( DRequestCompletedEventArgs e, string id )
		{
			WillLogin = false;
			if ( e.ResponseHeaders.Contains( "Set-Cookie" ) )
			{
				SaveAuth( e.Cookies );
				return;
			}

			try
			{
				JsonStatus.Parse( e.ResponseString );
			}
			catch( Exception ex )
			{
				ServerMessage = ex.Message;
			}

			if( LastAuth != null )
			{
				LastAuth = null;
				new CredentialVault().Remove( this );
				UpdateStatus( MemberStatus.RE_LOGIN_NEEDED );
				return;
			}

			UpdateStatus( MemberStatus.LOGGED_OUT );
		}

		private void LoginFailed( string CachedData, string id, Exception ex )
		{
			WillLogin = false;
			UpdateStatus( IsLoggedIn ? MemberStatus.LOGGED_IN : MemberStatus.LOGGED_OUT );
		}

		private void ValidateSession()
		{
			RCache.POST(
				Shared.ShRequest.Server
				, Shared.ShRequest.SessionValid()
				, CheckResponse, ClearAuth
				, false
			);
		}

		private async void CheckResponse( DRequestCompletedEventArgs e, string QueryId )
		{
			try
			{
				JsonObject JObj = JsonStatus.Parse( e.ResponseString );
				Id = JObj.GetNamedString( "data" );

				IsLoggedIn = true;

				UpdateStatus( MemberStatus.LOGGED_IN );
			}
			catch ( Exception ex )
			{
				IsLoggedIn = false;
				Logger.Log( ID, ex.Message, LogType.DEBUG );
			}

			if ( IsLoggedIn )
			{
				if ( Remember != null )
					new CredentialVault().Store( Remember );
			}
			else
			{
				ClearAuth();

				if ( LastAuth == null )
				{
					LastAuth = await new CredentialVault().Retrieve( this );
					if ( !string.IsNullOrEmpty( LastAuth.Account ) )
					{
						CurrentAccount = LastAuth.Account;
						CurrentPassword = LastAuth.Password;
						Login( LastAuth.Account, LastAuth.Password, false );
						return;
					}
				}

				UpdateStatus( MemberStatus.RE_LOGIN_NEEDED );
			}
		}

		private void RestoreAuth( XParameter MAuth )
		{
			try
			{
				Cookie MCookie = new Cookie(
					MAuth.GetValue( "name" )
					, MAuth.GetValue( "domain" )
					, MAuth.GetValue( "path" )
				);
				MCookie.Value = MAuth.GetValue( "value" );
				WHttpRequest.Cookies.Add( Shared.ShRequest.Server, MCookie );

				CurrentAccount = MAuth.GetValue( "account" );
			}
			catch ( Exception ex )
			{
				Logger.Log( ID, ex.Message, LogType.WARNING );
			}

			ValidateSession();
		}

		private void SaveAuth( CookieCollection Cookies )
		{
			foreach ( Cookie cookie in Cookies )
			{
				if ( cookie.Name == "sid" )
				{
					Logger.Log( ID, string.Format( "Set-Cookie: {0}=...", cookie.Name ), LogType.DEBUG );

					XParameter MAuth = new XParameter( "member-auth" );
					MAuth.SetValue( new XKey[] {
						new XKey( "name", cookie.Name )
						, new XKey( "domain" , cookie.Domain )
						, new XKey( "path", cookie.Path )
						, new XKey( "value" , cookie.Value )
						, new XKey( "account", CurrentAccount )
					} );

					AuthReg.SetParameter( MAuth );
					AuthReg.Save();
					break;
				}
			}

			ValidateSession();
		}

		private void ClearAuth( string arg1, string arg2, Exception arg3 ) { ClearAuth(); }
		private void ClearAuth( DRequestCompletedEventArgs arg1, string arg2 ) { ClearAuth(); }
		private void ClearAuth()
		{
			WHttpRequest.Cookies = new CookieContainer();

			AuthReg.RemoveParameter( "member-auth" );
			AuthReg.Save();
		}
	}
}