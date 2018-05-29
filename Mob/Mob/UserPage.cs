using Mob.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Java.Lang;
using Mob.Droid.Push;
using Xamarin.Forms;

namespace Mob
{

    public partial class UserPage : ContentPage
    {

    }


    public partial class UserPage : ContentPage
    {
        private Dictionary<string, UserSettings> _userSettings;
        #region Controls
        Image _userImg;
        Entry _userNameEntry;
        Label _userToolTip;
        Image _userEditImg;

        Image _emailImg;
        Label _emailToolTip;
        Entry _emailEntry;
        Image _emailEditImg;

        Image _locationImg;
        Label _locationToolTip;
        Entry _locationEntry;
        Image _locationEditImg;

        Image _resetImg;
        Button _resetButton;

        Image _clearDBImg;
        Label _clearDBEntry;
        #endregion
        public UserPage()
        {
            try
            {
                this.Title = "Профиль";
                _userSettings = new Dictionary<string, UserSettings>();
                _userSettings.Add("UserName", App.Database.GetSettingsByName("UserName"));
                _userSettings.Add("ReportEmail", App.Database.GetSettingsByName("ReportEmail"));
                _userSettings.Add("Location", App.Database.GetSettingsByName("Location"));
                _userSettings.Add("DeviceToken", App.Database.GetSettingsByName("DeviceToken"));
                #region UserNameControls
                _userImg = new Image
                {
                    Source = "user64.png",
                    Scale = 0.5,
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Center
                };
                _userNameEntry = new Entry
                {
                    IsEnabled = false,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.Start,
                    TextColor = Color.Black,
                    Text = _userSettings["UserName"] == null ? "" : _userSettings["UserName"].Vlaue,
                    Placeholder = "Введите имя и фамилию",
                    FontSize = 18,
                };
                _userNameEntry.TextChanged += _userNameEntry_TextChanged;
                _userNameEntry.Unfocused += _userNameEntry_Unfocused;
                _userToolTip = new Label
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.Start,
                    FontSize = 12,
                    TextColor = Color.Black,
                    Text = "Имя и фамилия"
                };
                _userEditImg = new Image
                {
                    Scale = 0.65,
                    Opacity = 0.2,
                    Source = "edit.png",
                    HorizontalOptions = LayoutOptions.End,
                    IsEnabled = _userSettings["UserName"] != null ? false : true,
                    VerticalOptions = LayoutOptions.Center
                };
                _userEditImg.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    TappedCallback = (v, o) =>
                    {
                        if (_userNameEntry.IsEnabled)
                        {
                            _userNameEntry.IsEnabled = false;
                            _userToolTip.TextColor = Color.Black;
                            _userEditImg.Opacity = 0.2;
                            if (_userNameEntry.Text.Trim() != "")
                            {
                                if (_userSettings["UserName"]?.Vlaue != null)
                                {
                                    _userSettings["UserName"].Vlaue = _userNameEntry.Text.Trim();
                                    App.Database.SaveUserSettings(_userSettings["UserName"]);
                                }
                                else
                                {
                                    App.Database.SaveUserSettings(new UserSettings { Name = "UserName", Vlaue = _userNameEntry.Text.Trim() });
                                    _userSettings["UserName"] = App.Database.GetSettingsByName("UserName");
                                }
                            }
                        }
                        else
                        {
                            _userNameEntry.IsEnabled = true;
                            _userToolTip.TextColor = Color.Red;
                            _userEditImg.Opacity = 1;
                            _userNameEntry.Focus();
                        }

                    }
                });
                var userFieldLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Orientation = StackOrientation.Vertical,
                    Spacing = 0,
                    Children = { _userNameEntry, _userToolTip }
                };
                var userLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Orientation = StackOrientation.Horizontal,
                    Children = { _userImg, userFieldLayout, _userEditImg }
                };
                #endregion
                #region ReportEmailControls
                _emailImg = new Image { Source = "envelope.png", Scale = 0.5 };
                _emailEntry = new Entry
                {
                    IsEnabled = false,
                    TextColor = Color.Black,
                    Text = _userSettings["ReportEmail"] == null ? "" : _userSettings["ReportEmail"].Vlaue,
                    Placeholder = "Введите email",
                    FontSize = 18,
                    Keyboard = Keyboard.Email,
                    Margin = new Thickness(0)
                };
                _emailEntry.TextChanged += _emailEntry_TextChanged;
                _emailToolTip = new Label
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.StartAndExpand,
                    FontSize = 12,
                    TextColor = Color.Black,
                    Text = "Адрес отправки отчетов",
                    Margin = new Thickness(0),

                };
                _emailEditImg = new Image
                {
                    Scale = 0.65,
                    Opacity = 0.2,
                    Source = "edit.png",
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center
                };
                _emailEditImg.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    TappedCallback = (v, o) =>
                    {
                        if (_emailEntry.IsEnabled)
                        {

                            if (IsValid(_emailEntry.Text.Trim()))
                            {
                                _emailEntry.IsEnabled = false;
                                _emailToolTip.TextColor = Color.Black;
                                _emailEditImg.Opacity = 0.2;
                                if (_userSettings["ReportEmail"]?.Vlaue != null)
                                {
                                    _userSettings["ReportEmail"].Vlaue = _emailEntry.Text.Trim();
                                    App.Database.SaveUserSettings(_userSettings["ReportEmail"]);
                                }
                                else
                                {
                                    App.Database.SaveUserSettings(new UserSettings { Name = "ReportEmail", Vlaue = _emailEntry.Text.Trim() });
                                    _userSettings["ReportEmail"] = App.Database.GetSettingsByName("ReportEmail");
                                }
                            }
                            else
                                return;
                        }
                        else
                        {
                            _emailEntry.IsEnabled = true;
                            _emailToolTip.TextColor = Color.Red;
                            _emailEditImg.Opacity = 1;
                            _emailEntry.Focus();
                        }
                    }
                });
                var emailFieldLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Spacing = 0,
                    Orientation = StackOrientation.Vertical,
                    Children = { _emailEntry, _emailToolTip }
                };
                var emailLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Orientation = StackOrientation.Horizontal,
                    Children = { _emailImg, emailFieldLayout, _emailEditImg }
                };
                #endregion
                #region Location Controls
                _locationImg = new Image { Source = "placeholder.png", Scale = 0.5 };
                _locationEntry = new Entry
                {
                    IsEnabled = false,
                    TextColor = Color.Black,
                    Text = _userSettings["Location"] == null ? "" : _userSettings["Location"].Vlaue,
                    Placeholder = "Введите название точки",
                    FontSize = 18,
                    Keyboard = Keyboard.Email,
                    Margin = new Thickness(0)
                };
                _locationEntry.TextChanged += _locationEntry_TextChanged;
                _locationEntry.Unfocused += _locationEntry_Unfocused;
                _locationToolTip = new Label
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.StartAndExpand,
                    FontSize = 12,
                    TextColor = Color.Black,
                    Text = "Место проката",
                    Margin = new Thickness(0),

                };
                _locationEditImg = new Image
                {
                    Scale = 0.65,
                    Opacity = 0.2,
                    Source = "edit.png",
                    HorizontalOptions = LayoutOptions.End,
                    IsEnabled = _userSettings["Location"] != null ? false : true,
                    VerticalOptions = LayoutOptions.Center
                };
                _locationEditImg.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    TappedCallback = (v, o) =>
                    {
                        if (_locationEntry.IsEnabled)
                        {
                            _locationEntry.IsEnabled = false;
                            _locationToolTip.TextColor = Color.Black;
                            _locationEditImg.Opacity = 0.2;
                            if (_locationEntry.Text.Trim() != "")
                            {
                                if (_userSettings["Location"]?.Vlaue != null)
                                {
                                    _userSettings["Location"].Vlaue = _locationEntry.Text.Trim();
                                    App.Database.SaveUserSettings(_userSettings["Location"]);
                                }
                                else
                                {
                                    App.Database.SaveUserSettings(new UserSettings { Name = "Location", Vlaue = _locationEntry.Text.Trim() });
                                    _userSettings["Location"] = App.Database.GetSettingsByName("Location");
                                }
                            }
                        }
                        else
                        {
                            _locationEntry.IsEnabled = true;
                            _locationToolTip.TextColor = Color.Red;
                            _locationEditImg.Opacity = 1;
                            _locationEntry.Focus();
                        }
                    }
                });
                var _locationFieldLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Spacing = 0,
                    Orientation = StackOrientation.Vertical,
                    Children = { _locationEntry, _locationToolTip }
                };
                var locationLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Orientation = StackOrientation.Horizontal,
                    Children = { _locationImg, _locationFieldLayout, _locationEditImg }
                };
                #endregion
                #region ResetControls
                _resetImg = new Image { Source = "settings.png", Scale = 0.5 };
                _resetButton = new Button
                {
                    Text = "Отправить данные",
                    TextColor = Color.Red,
                    BackgroundColor = Color.Transparent,
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    IsEnabled = (_userSettings["Location"]?.Vlaue != null && _userSettings["UserName"]?.Vlaue != null) ? true : false,
                    Opacity = 0.5
                };
                _resetButton.Clicked += async (s, e) =>
                {
                    if (_userSettings["Location"]?.Vlaue != null && _userSettings["UserName"]?.Vlaue != null)
                    {
                        if (_userSettings["DeviceToken"]?.Vlaue == null)
                        {
                            var serviceFirb = new MyFirebaseIIDService();
                            App.Database.SaveUserSettings(new UserSettings { Name = "DeviceToken", Vlaue = serviceFirb.GetToken() });
                            _userSettings["DeviceToken"] = App.Database.GetSettingsByName("DeviceToken");
                        }

                        GyroServer.SendUserData();
                    }
                };
                var resetLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.EndAndExpand,
                    Orientation = StackOrientation.Horizontal,
                    Children = { _resetImg, _resetButton }
                };
                #endregion

                var mainLayout = new StackLayout
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    Orientation = StackOrientation.Vertical,
                    Padding = new Thickness(0, 5, 10, 0),
                    Children = { userLayout,
                    emailLayout,
                    locationLayout,
                    resetLayout
                }
                };

                Content = new StackLayout
                {
                    Children = {
                    mainLayout
                }
                };
            }
            catch (System.Exception ex)
            {
                App.Database.SaveError(new Mob.Dto.Error { Date = DateTime.Now, Invoker = this.GetType().Name, Message = ex.Message });
            }
        }

        private void _locationEntry_Unfocused(object sender, FocusEventArgs e)
        {
            _locationEntry.Text = UppercaseWords(_locationEntry.Text);
        }

        private void _userNameEntry_Unfocused(object sender, FocusEventArgs e)
        {
            _userNameEntry.Text = UppercaseWords(_userNameEntry.Text);
        }

        static string UppercaseWords(string value)
        {
            char[] array = value.ToCharArray();
            // Handle the first letter in the string.
            if (array.Length >= 1)
            {
                if (char.IsLower(array[0]))
                {
                    array[0] = char.ToUpper(array[0]);
                }
            }
            // Scan through the letters, checking for spaces.
            // ... Uppercase the lowercase letters following spaces.
            for (int i = 1; i < array.Length; i++)
            {
                if (array[i - 1] == ' ')
                {
                    if (char.IsLower(array[i]))
                    {
                        array[i] = char.ToUpper(array[i]);
                    }
                }
            }
            return new string(array);
        }

        protected override bool OnBackButtonPressed()
        {
            Navigation.PopAsync();
            return true;
        }

        private void _locationEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            _locationToolTip.TextColor = _locationEntry.Text != "" ? Color.Green : Color.Red;
        }

        public bool IsValid(string emailaddress)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(emailaddress);
            return match.Success ? true : false;
        }

        private void _emailEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            _emailToolTip.TextColor = IsValid(_emailEntry.Text) ? Color.Green : Color.Red;
        }

        private void _userNameEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            _userToolTip.TextColor = _userNameEntry.Text != "" ? Color.Green : Color.Red;
        }
    }
}