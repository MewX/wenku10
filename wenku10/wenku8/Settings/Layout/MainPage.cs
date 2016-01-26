﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Loaders;

namespace wenku8.Settings.Layout
{
    using Ext;
    using Model.ListItem;

    class MainPage : ActiveData, IMainPageSettings
    {
        private const string TFileName = FileLinks.ROOT_SETTING + FileLinks.LAYOUT_MAINPAGE;
        private const string EN_CUSTOM = "CustomSection";
        private const string EN_SPICKS = "StaffPicks";

        private XRegistry LayoutSettings;

        #region Section Definitions
        private static readonly Dictionary<string, Tuple<Type, string>> SectionDefs = new Dictionary<string, Tuple<Type, string>>()
        {
            {
                "History"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.History )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_LOCAL_HISTORY" )
                )
            },
            {
                "NewestEntries"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.NavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_NEW_ARRIVALS" )
                )
            },
            {
                "RecentUpdate"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.NavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_RECENT_UPDATE" )
                )
            },
            {
                "TopList_DDigest"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.NavList )
                    ,  X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_DDigest" )
                )
            },
            {
                "TopList_HITs"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.NavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_HITs" )
                )
            },
            {
                "TopList_WDigest"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.NavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_WDigest" )
                )
            },
            {
                "TopList_Favourite"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.NavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_Favourite" )
                )
            },
            {
                "Finished"
                , new Tuple<Type, string>(
                    typeof( wenku10.Pages.NavList )
                    , X.Const<string>( XProto.WProtocols, "COMMAND_XML_PARAM_FIN" )
                )
            },
        };
        #endregion

        private IEnumerable<ActiveItem> _secList = null;
        // The Param of Selected Section
        private XParameter WSSec
        {
            get
            {
                return Customs.First( ( x ) => x.GetBool( "custom" ) );
            }
        }
        private IList<XParameter> Customs
        {
            get
            {
                return LayoutSettings.GetParametersWithKey( "custom" );
            }
        }

        public bool IsCustomSectionEnabled
        {
            get { return LayoutSettings.GetParameter( EN_CUSTOM ).GetBool( "enable" ); }
            set
            {
                LayoutSettings.SetParameter( EN_CUSTOM, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        public bool IsStaffPicksEnabled
        {
            get { return LayoutSettings.GetParameter( EN_SPICKS ).GetBool( "enable" ); }
            set
            {
                LayoutSettings.SetParameter( EN_SPICKS, new XKey( "enable", value ) );
                LayoutSettings.Save();
            }
        }

        public ActiveItem SelectedSection
        {
            get
            {
                return SectionList.First( ( x ) => x.Payload == WSSec.ID );
            }
        }

        public IEnumerable<ActiveItem> SectionList
        {
            get
            {
                if ( _secList == null )
                {
                    List<ActiveItem> Items = new List<ActiveItem>();

                    StringResources stx = new StringResources( "NavigationTitles" );
                    foreach ( string Key in SectionDefs.Keys )
                    {
                        Items.Add( new ActiveItem(
                            stx.Text( Key )
                            , stx.Text( "Desc_" + Key )
                            , Key
                        ) );
                    }
                    _secList = Items;
                }

                return _secList;
            }
        }

        public MainPage()
        {
			LayoutSettings = new XRegistry( AppKeys.TS_CXML, TFileName );
            InitParams();
        }

        public void InitParams()
        {
            bool Changed = false;
            XParameter SectionKey;
            // Create Item if not availble
            foreach ( string Key in SectionDefs.Keys )
            {
                SectionKey = LayoutSettings.GetParameter( Key );
                if ( SectionKey == null )
                {
                    LayoutSettings.SetParameter( Key, new XKey( "custom", false ) );
                    Changed = true;
                }
            }

            SectionKey = LayoutSettings.GetParameters().FirstOrDefault(
                ( x ) => x.GetBool( "custom" )
            );

            if ( SectionKey == null )
            {
                LayoutSettings.SetParameter(
                    "NewestEntries", new XKey( "custom", true )
                );
                LayoutSettings.SetParameter( EN_CUSTOM, new XKey( "enable", true ) );
                LayoutSettings.SetParameter( EN_SPICKS, new XKey( "enable", true ) );
                Changed = true;
            }

            if ( Changed ) LayoutSettings.Save();
        }

        public void SectionSelected( ActiveItem A )
        {
            foreach( XParameter Param in Customs )
            {
                Param.SetValue( new XKey( "custom", Param.ID == A.Payload ) );
                LayoutSettings.SetParameter( Param );
            }
            LayoutSettings.Save();
        }

        public Tuple<Type, string> PayloadCommand( string Payload )
        {
            return SectionDefs[ Payload ];
        }

        public IEnumerable<SubtleUpdateItem> NavSections()
        {
            IEnumerable<XParameter> Params = IsCustomSectionEnabled
                ? Customs.Where( ( x ) => !x.GetBool( "custom" ) )
                : Customs
                ;

            List<SubtleUpdateItem> Secs = new List<SubtleUpdateItem>();
            StringResources stx = new StringResources( "NavigationTitles" );
            foreach ( XParameter Param in Params )
            {
                Tuple<Type, string> Def = SectionDefs[ Param.ID ];
                Secs.Add(
                    new SubtleUpdateItem(
                        stx.Text( Param.ID ), stx.Text( "Desc_" + Param.ID )
                        , Def.Item1 , Def.Item2
                    )
                );
            }

            return Secs;
        }
    }
}
