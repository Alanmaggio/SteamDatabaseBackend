﻿/*
 * Copyright (c) 2013-present, SteamDB. All rights reserved.
 * Use of this source code is governed by a BSD-style license that can be
 * found in the LICENSE file.
 */

using System;
using System.Threading;

namespace SteamDatabaseBackend
{
    internal class Watchdog : IDisposable
    {
        public Timer Timer { get; private set; }

        public Watchdog()
        {
            Timer = new Timer(OnTimer, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(20));
        }

        public void Dispose()
        {
            if (Timer != null)
            {
                Timer.Dispose();
                Timer = null;
            }
        }

        private void OnTimer(object state)
        {
            if (Settings.Current.IRC.Enabled && !IRC.Instance.IsConnected)
            {
                Log.WriteWarn(nameof(Watchdog), "Forcing IRC reconnect.");

                IRC.Instance.Connect();
            }

            if (Steam.Instance.IsLoggedOn)
            {
                AccountInfo.Sync();
            }
            else if (DateTime.Now.Subtract(Connection.LastSuccessfulLogin).TotalMinutes >= 5.0)
            {
                IRC.Instance.SendOps("[Watchdog] Forcing a Steam reconnect.");

                Log.WriteWarn(nameof(Watchdog), "Forcing a Steam reconnect.");

                Connection.Reconnect(null, null);
            }

            if (DateTime.Now.Subtract(Steam.Instance.DepotProcessor.LastServerRefreshTime).TotalHours >= 2.0)
            {
                Log.WriteWarn(nameof(Watchdog), "Refreshing depot cdn servers");

                TaskManager.Run(Steam.Instance.DepotProcessor.UpdateContentServerList);
            }
        }
    }
}
