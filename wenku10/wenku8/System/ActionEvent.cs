﻿using Microsoft.Services.Store.Engagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wenku8.System
{
    static class ActionEvent
    {
#if !DEBUG
        public const string SECRET_MODE = "SecretMode";
        public const string NORMAL_MODE = "NormalMode";

        private static bool Normaled = false;
        private static bool Secreted = false;

        public static void Secret()
        {
            if ( Secreted ) return;
            Secreted = true;
            StoreServicesCustomEventLogger.GetDefault().Log( SECRET_MODE );
        }

        public static void Normal()
        {
            if ( Normaled ) return;
            Normaled = true;
            StoreServicesCustomEventLogger.GetDefault().Log( NORMAL_MODE );
        }
#else
        public static void Secret() { } 
        public static void Normal() { } 
#endif

    }
}