﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI;

using Net.Astropenguin.Helpers;
using Net.Astropenguin.Messaging;
using Net.Astropenguin.UI.Icons;

using libtaotu.Pages;

using wenku10.Pages.Dialogs.Taotu;

namespace wenku8.Taotu
{
	using ThemeIcons;

	sealed class WenkuListLoader : GrimoireListLoader
	{
		protected override IconBase Icon { get { return new IconExoticUni() { AutoScale = true }; } }

		public override async Task Edit()
		{
			await Popups.ShowDialog( new EditProcListLoader( this ) );
			if ( SubEdit != null )
			{
				MessageBus.Send( typeof( ProceduresPanel ), "SubEdit", this );
			}
		}
	}
}