﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

using Net.Astropenguin.DataModel;
using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;
using Net.Astropenguin.Messaging;

using libtaotu.Controls;
using libtaotu.Models.Procedure;
using static libtaotu.Pages.ProceduresPanel;

namespace wenku8.Model.Section
{
    using Book;
    using Loaders;
    using Taotu;

    sealed class ZoneSpider : ActiveData
    {
        public static readonly string ID = typeof( ZoneSpider ).Name;

        public string ZoneId { get { return PM.GUID; } }

        public Observables<BookItem, BookItem> Data { get; private set; }

        public ObservableCollection<Procedure> ProcList { get { return PM?.ProcList; } }
        public Uri Banner { get; private set; }

        private ProcManager PM;

        public string Message { get; private set; }

        private bool _IsLoading = false;
        public bool IsLoading
        {
            get { return _IsLoading; }
            private set
            {
                _IsLoading = value;
                NotifyChanged( "IsLoading" );
            }
        }

        public ZoneSpider()
        {
            MessageBus.OnDelivery += MessageBus_OnDelivery;
        }

        ~ZoneSpider()
        {
            MessageBus.OnDelivery -= MessageBus_OnDelivery;
        }

        private void MessageBus_OnDelivery( Message Mesg )
        {
            if ( Mesg.Payload is PanelLog )
            {
                PanelLog PLog = ( PanelLog ) Mesg.Payload;
                Message = Mesg.Content;
                NotifyChanged( "Message" );
            }
        }

        private void SetBanner()
        {
            WenkuListLoader PLL = ( WenkuListLoader ) ProcList.FirstOrDefault( x => x is WenkuListLoader );

            if ( PLL == null )
            {
                throw new InvalidFIleException();
            }

            Banner = PLL.BannerSrc;
            NotifyChanged( "Banner" );
        }

        public async void OpenFile()
        {
            try
            {
                IStorageFile ISF = await AppStorage.OpenFileAsync( ".xml" );
                if ( ISF == null ) return;

                IsLoading = true;

                XParameter Param = new XRegistry( await ISF.ReadString(), null, false ).Parameter( "Procedures" );
                PM = new ProcManager( Param );
                NotifyChanged( "ProcList" );

                SetBanner();

                ZSFeedbackLoader<BookItem> ZSF = new ZSFeedbackLoader<BookItem>( PM.CreateSpider() );
                Data = new Observables<BookItem, BookItem>( await ZSF.NextPage() );
                Data.ConnectLoader( ZSF );

                IsLoading = false;
                Data.LoadStart += ( s, e ) => IsLoading = true;
                Data.LoadEnd += ( s, e ) => IsLoading = false;

                NotifyChanged( "Data" );
            }
            catch( InvalidFIleException )
            {
                ProcManager.PanelMessage( ID, () => Res.RSTR( "InvalidXML" ), LogType.ERROR );
            }
            catch( Exception ex )
            {
                IsLoading = false;
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }
        }

        private class InvalidFIleException : Exception { }

    }
}