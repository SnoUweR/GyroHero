using Android.App;
using Android.Content.PM;
using Android.Widget;
using Android.OS;
using Android.Content;
using System.Globalization;
using System.Threading;
using Xamarin.Forms;
using Firebase.Messaging;
using Firebase.Iid;
using Android.Util;

namespace Mob.Droid
{
    [Activity(Label = "GyroHero", Icon = "@drawable/icon", Theme = "@style/MainTheme"/*, MainLauncher = true*/, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        #region Private members
        private NotificationManager _notificationManager;
        private int notificationId = 0;
        private App _app;
        private Intent _intent;
        #endregion
        /// <summary>
        /// Start background task
        /// </summary>
        void StartService()
        {
            _intent = new Intent(this, typeof(BackgroundTaskService));
            StartService(_intent);
        }
        /// <summary>
        /// Stop background task
        /// </summary>
        void StopService()
        {
            MessagingCenter.Subscribe<StopBackgroundTask>(this, "BackgroundTaskService", message =>
            {
                _intent = new Intent(this, typeof(BackgroundTaskService));
                StopService(_intent);
            });
        }
        /// <summary>
        /// Do short toast
        /// </summary>
        /// <param name="Statement">Text</param>
        private void DoToast(string Statement)
        {
            Toast.MakeText(this, Statement, ToastLength.Short).Show();
        }
        /// <summary>
        /// OnCreate
        /// </summary>
        /// <param name="bundle">Bundle</param>
        protected override void OnCreate(Bundle bundle)
        {
            //var splash = new SplashActivity(this);
            base.OnCreate(bundle);
            //var splash = new SplashActivity();
            //splash.WithFullScreen();
            ///Set Russian culture
            var userSelectedCulture = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = userSelectedCulture;
            ///Style
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;
            ///Notification
            global::Xamarin.Forms.Forms.Init(this, bundle);
            _notificationManager =
                GetSystemService(Context.NotificationService) as NotificationManager;
            ///Application
            _app = new App(DoNotification, DoToast);
            LoadApplication(_app);
            StartService();

        }
        /// <summary>
        /// OnDestoy Events
        /// </summary>
        protected override void OnDestroy()
        {
            StopService();
            base.OnDestroy();
        }
        /// <summary>
        /// Build and notify a new notification
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="message">Message</param>
        public void DoNotification(string title, string message)
        {
            // Instantiate the builder and set notification elements:
            Notification.Builder builder = new Notification.Builder(this)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetVibrate(new long[] { 500, 500, 500 })
                .SetDefaults(NotificationDefaults.Sound)
                .SetSmallIcon(Resource.Drawable.Noti);
            // Build the notification:
            Notification notification = builder.Build();
            _notificationManager.Notify(notificationId++, notification);
        }
    }
}

