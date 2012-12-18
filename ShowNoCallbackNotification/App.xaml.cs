using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using SharedClasses;

namespace ShowNoCallbackNotification
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		int _notifcount;
		int NotificationCount
		{
			get { return _notifcount; }
			set
			{
				_notifcount = value;
				if (_notifcount == 0)
					Environment.Exit(0);
				//Exits if count is zero, app will just start again when needed
			}
		}

		const int cDefaultTimeoutSeconds = 3;
		private void ShowNotificationFromCommandlineArgs(string[] args)
		{
			if (args.Length < 2)//Does not have at least a title
			{
				ShowNoCallbackNotificationInterop.Notify(null, "This notification was shown because 'ShowNotificationFromCommandlineArgs' was ran without commandline arguments."); 
				//args = new string[] { "", "Title", "Message", "Warning", "20" };
				if (NotificationCount == 0)
					Environment.Exit(0);
				return;
			}

			//First arg is exe path
			//Format: thisapp.exe "Title" "Message" NotificationType SecondsToShow
			//use -99 SecondsToShow to show forever
			string title = args.Length > 1 ? args[1] : "[NOCOMMANDLINETITLE]";
			string message = args.Length > 2 ? args[2] : "[NOCOMMANDLINEMESSAGE]";

			string notiftypeStr = args.Length > 3 ? args[3] : "";
			ShowNoCallbackNotificationInterop.NotificationTypes notifType;
			if (!Enum.TryParse<ShowNoCallbackNotificationInterop.NotificationTypes>(notiftypeStr, true, out notifType))
				notifType = ShowNoCallbackNotificationInterop.NotificationTypes.Subtle;

			bool wasStickyOrTimeoutMinus99 = 
				args.Length > 4 
				&& 
				(args[4].Equals("sticky", StringComparison.InvariantCultureIgnoreCase) 
				|| args[4].Equals("-99")
				|| args[4].Equals("-1"));
			string timeoutSecondsStr = args.Length > 4 ? args[4] : cDefaultTimeoutSeconds.ToString();
			int timeoutSeconds;
			if (!int.TryParse(timeoutSecondsStr, out timeoutSeconds)
				|| timeoutSeconds < 0)
				timeoutSeconds = cDefaultTimeoutSeconds;
			
			NotificationCount++;
			TimeSpan? tmpTimeout = null;
			if (!wasStickyOrTimeoutMinus99)
				tmpTimeout = TimeSpan.FromSeconds(timeoutSeconds);
			WpfNotificationWindow.ShowNotification(
				title: title,
				message: message,
				notificationType: notifType,
				timeout: tmpTimeout,//If was -99 it will show forever
				onCloseCallback_WasClickedToCallback: (o, wasclickedon) => { NotificationCount--; });
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			Dictionary<string, string> userPrivilages;
			if (!LicensingInterop_Client.Client_ValidateLicense(out userPrivilages, err => UserMessages.ShowErrorMessage(err)))
				Environment.Exit(LicensingInterop_Client.cApplicationExitCodeIfLicenseFailedValidation);

			//base.OnStartup(e);
			//MainWindow not used at all
			SingleInstanceApplicationManager<MainWindow>.CheckIfAlreadyRunningElseCreateNew(
				(evtargs, win) =>
				{
					//Second instances (what to do first first instance window)
					ShowNotificationFromCommandlineArgs(evtargs.CommandLineArgs);
				},
				(args, win) =>
				{
					//First instance
					SharedClasses.AutoUpdating.CheckForUpdates(
						//SharedClasses.AutoUpdatingForm.CheckForUpdates(
						//exitApplicationAction: () => Dispatcher.Invoke((Action)delegate { this.Shutdown(); }),
						ActionIfUptoDate_Versionstring: (versionstring) => WpfNotificationWindow.SetCurrentVersionDisplayed(versionstring));

					ShowNotificationFromCommandlineArgs(args);
				});
		}
	}
}
