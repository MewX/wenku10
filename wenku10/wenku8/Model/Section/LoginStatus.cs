﻿using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;
using Net.Astropenguin.UI;

namespace wenku8.Model.Section
{
    using Ext;
    using Resources;
    using Settings;

    class LoginStatus : ActiveData
    {
        public Page _frameContent;
        public Page FrameContent
        {
            get
            {
                return _frameContent;
            }
            private set
            {
                _frameContent = value;
                NotifyChanged( "FrameContent" );
            }
        }

        private const string AvatarLocation = FileLinks.ROOT_IMAGE + "USER_AVATAR";

        public BitmapImage Avatar { get; private set; }

        private ControlState _pagestatus = ControlState.Foreatii;
        public ControlState PageStatus
        {
            get
            {
                return _pagestatus;
            }
            private set
            {
                _pagestatus = value;
                NotifyChanged( "PageStatus" );
            }
        }

        private string _loginOrInfo;
        public string LoginOrInfo
        {
            get
            {
                return _loginOrInfo;
            }
            private set
            {
                _loginOrInfo = value;
                NotifyChanged( "LoginOrInfo" );
            }
        }

        private IMember Member;

        public LoginStatus()
        {
            Member = X.Singleton<IMember>( XProto.Member );
            MemberStatusChanged();
            Member.OnStatusChanged += Member_OnStatusChanged;
        }

        ~LoginStatus()
        {
            Member.OnStatusChanged -= Member_OnStatusChanged;
        }

        private void Member_OnStatusChanged( object sender, MemberStatus e )
        {
            MemberStatusChanged();
        }

        private void MemberStatusChanged()
        {
            StringResources stx = new StringResources();
            LoginOrInfo = Member.IsLoggedIn
                ? stx.Text( "Login_AccountName" )
                : stx.Text( "Login" )
                ;

            RefreshAvatar();

            if( !Member.IsLoggedIn )
            {
                if ( Member.WillLogin ) return;

                Image.Destroy( Avatar );
                NotifyChanged( "Avatar" );
                return;
            }

            X.Instance<IRuntimeCache>( XProto.WRuntimeCache, 0, false )
                .InitDownload(
                "USER_AVATAR"
                , X.Call<XKey[]>( XProto.WRequest, "GetUserAvatar" )
                , AvatarLoaded
                , ( string id, string url, Exception ex ) => { RefreshAvatar(); }
                , false
            );
        }

        private void AvatarLoaded( DRequestCompletedEventArgs e, string id )
        {
            Shared.Storage.WriteBytes( AvatarLocation, e.ResponseBytes );
            RefreshAvatar();
        }

        private void RefreshAvatar()
        {
            Image.Destroy( Avatar );
            Avatar = new BitmapImage();

            if ( !( Member.IsLoggedIn && Shared.Storage.FileExists( AvatarLocation ) ) )
            {
                NotifyChanged( "Avatar" );
                return;
            }

            Avatar.SetSourceFromUrl( AvatarLocation );
            NotifyChanged( "Avatar" );
        }

        public async void PopupLoginOrInfo()
        {
            if ( Member.IsLoggedIn )
            {
                FrameContent = new wenku10.Pages.Account( ClosePopup );
                PageStatus = ControlState.Reovia;
            }
            else
            {
                wenku10.Pages.Dialogs.Login Login = new wenku10.Pages.Dialogs.Login();
                if ( Member.Status == MemberStatus.RE_LOGIN_NEEDED )
                    Login.ShowExpireMessage();
                await Popups.ShowDialog( Login );
            }
        }

        private void ClosePopup()
        {
            FrameContent = null;
            PageStatus = ControlState.Foreatii;
        }
    }
}
