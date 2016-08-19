﻿using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Net.Astropenguin.IO;
using Net.Astropenguin.Logging;

namespace wenku8.System
{
    using Config;

    sealed class Migration
    {
        public static readonly string ID = typeof( Migration ).Name;

        public Migration() { }

        public async Task Migrate()
        {
            // Current Version
            if ( Properties.VERSION == Bootstrap.Version )
            {
                Logger.Log( ID, "Already the latest version", LogType.INFO );
                return;
            }

            try
            {
                switch ( Properties.VERSION )
                {
                    // Keep up to 5 migration versions
                    case "1.7.4t":
                    case "1.7.5t":
                    case "1.7.6t":
                    case "1.7.7t":
                    case "1.7.8t":
                        break;

                    case "1.2.7b":
                    case "1.2.8b":
                    case "1.2.9b":
                    case "1.2.10b":
                    case "1.3.0b":
                        v131b();
                        break;

                    default:
                        Logger.Log( ID, "Unknown Version: Will try to migrate", LogType.ERROR );
                        // Just to ensure the migration should be run asynchronously
                        await Task.Delay( 1 );
                        break;
                }
            }
            catch ( Exception ex )
            {
                Logger.Log( ID, ex.Message, LogType.ERROR );
            }

            Properties.VERSION = Bootstrap.Version;
        }

        private void v131b()
        {
            // Setting -> Settings
            AppStorage Storage = new AppStorage();
            IsolatedStorageFile ISFS = Storage.GetISOStorage();
            if( ISFS.DirectoryExists( "Setting" ) )
            {
                ISFS.MoveDirectory( "Setting", "Settings" );
            }
        }
    }
}