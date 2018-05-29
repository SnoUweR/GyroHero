using Mob.Dto;
using Mob.Requests;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace Mob
{
    public delegate void RentManipulate(RentViewModel rent);
    public delegate string Navi(RentEnd page);
    public delegate Task<bool> ConfirmActionResult(string text);
    public delegate Task<string> InputBox(string text);

    public class RentStringFormats
    {
        public string Id { get; set; }
        public string RentTime { get; set; }
        public decimal Payment { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public string ClientName { get; set; }

        public RentStringFormats(Rent rent)
        {
            Id = rent.Id.ToString();
            RentTime = rent.RentTime.ToString(@"hh\:mm");
            Payment = rent.Payment;//.ToString();
            Date = rent.Date.ToString();
            Time = rent.Time.ToString(@"hh\:mm");
            switch (rent.Type)
            {

                case "G": Type = "Гироскутер"; break;
                case "C": Type = "Велосипед"; break;
                default:
                    Type = "";
                    break;
            }
            ClientName = rent.ClientName;
        }

    }

    public class Rent
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public TimeSpan RentTime { get; set; }
        public string ClientName { get; set; }
        public decimal Payment { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string Type { get; set; }
        // public bool Completed { get; set; }
    }

    public class ClientInfo
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string DocumentType { get; set; }
        public string Serial { get; set; }
        public string Number { get; set; }
    }
    #region MVVM
    public delegate void PropertyChange(string Property);
    public class TimeFormat
    {
        public Color Color { get; set; }
        public string Time { get; set; }
    }

    public class RentModel
    {
        public TimeSpan Overtime { get; set; }
        public PriceInfo RentPrice { get; set; }
        public ClientInfo Client { get; set; }
        [JsonIgnore]
        public ConfirmActionResult Confirm { get; set; }
        [JsonIgnore]
        public Navi Navigate { get; set; }
        [JsonIgnore]
        public InputBox Test { get; set; }

        public void RecalculateTime(DateTime storedDate)
        {
            _time = new TimeSpan(0,0,Convert.ToInt32((DateTime.Now - storedDate).Duration().TotalSeconds));
            _inProgress = false;
            CurentTime = new TimeFormat();
            if (_time > RentPrice.Time)
            {
                CurentTime.Time = $"+{(_time - RentPrice.Time):T}";
            }
            else
            {
                CurentTime.Color = Color.Green;
                CurentTime.Time = $"{(RentPrice.Time - _time):T}";
            }
        }

        public int StoredId = 0;

        public TimeFormat CurentTime { get; set; }

        private TimeSpan _time;
        /// <summary>
        /// has already notify about overtime?
        /// </summary>
        private bool isNotify = false;
        private object _lock = new object();
        public bool _inProgress;
        /// <summary>
        /// Remove action
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CanRemoveAsync()
        {
            if (_time.TotalSeconds != 0)
            {
                App.Toast("Нельзя удалить!");
                return false;
            }
            var ans = await Confirm($"Удалить?");
            if (ans)
            {
                _inProgress = false;
               // App.Database.DeleteStoredRent(StoredId);
                return true;
            }
            return false;
        }
        /// <summary>
        /// Complete action
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CanCompleteAsync()
        {
            if (_time.TotalSeconds == 0)
            {
                App.Toast("Нельзя завершить!");
                return false;
            }
            var ans = await Confirm($"Завершить?");
            if (ans != true)
                return false;
            if (_inProgress)
                StopReceivedMessages();
            // _button.Source = "stop.png";
            var rent = new Rent
            {
                Date = DateTime.Now.Date,
                Time = DateTime.Now.TimeOfDay,
                Payment = RentPrice.Price,
                Type = RentPrice.Vehicle,
                ClientName = Client.Name,
                RentTime = _time//,
                //Completed = true
            };
            _inProgress = false;
            if (_time > RentPrice.Time)
            {
                var overtime = _time - RentPrice.Time;
                var res = await Test($"Необходимо доплатить за {overtime}");
                if (res == null)
                    return false;
                if (res != "")
                {
                    rent.Payment += Convert.ToDecimal(res);
                }
            }
            App.Database.SaveRent(rent);
            App.Database.DeleteStoredRent(rent.Id);
            App.Toast($"+{rent.Payment}₽");
            return true;
        }

        public double Diff { get { return (RentPrice.Time - _time).TotalSeconds; } }

        public RentModel(ClientInfo client, PriceInfo rentPrice)
        {
            Client = client;
            RentPrice = rentPrice;
            _time = new TimeSpan(0, 0, 0, 0);
            CurentTime = new TimeFormat {Time= $"{RentPrice.Time}", Color = Color.Green };           
        }
        /// <summary>
        /// Stop recieve messages
        /// </summary>
        private void StopReceivedMessages()
        {
            MessagingCenter.Unsubscribe<TickMessage>(this, "TickMessage");
            _inProgress = false;
        }

        private void HandleReceivedMessages(PropertyChange OnPropertyChange)
        {
            _inProgress = true;

            MessagingCenter.Subscribe<TickMessage>(this, "TickMessage", message =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    lock (_lock)
                    {
                        _time = _time.Add(new TimeSpan(0, 0, 1));

                        if (_time > RentPrice.Time)
                        {
                            CurentTime.Color = Color.Red;
                            CurentTime.Time = $"+{(_time - RentPrice.Time):T}";
                            if (!isNotify)
                            {
                                App.DoNotify = $"У {Client.Name} закончилось время!";
                                isNotify = true;
                            }
                        }
                        else
                        {
                            CurentTime.Color = Color.Green;
                            CurentTime.Time = $"{(RentPrice.Time - _time):T}";
                        }
                        OnPropertyChange("Time");
                        OnPropertyChange("TimeColor");
                    }
                });
            });
        }

        /// <summary>
        /// start-stop actions
        /// </summary>
        public void ChangeMode(PropertyChange OnPropertyChange)
        {
            if (_inProgress)
                StopReceivedMessages();
            else
                HandleReceivedMessages(OnPropertyChange);
            OnPropertyChange("Image");
        }
    }


    public class RentViewModel : INotifyPropertyChanged
    {
        private RentModel _rent;

        public event PropertyChangedEventHandler PropertyChanged;

        public RentManipulate Delete { get; set; }
        #region Commands
        public ICommand ChangeMode { protected set; get; }

        public ICommand Remove { protected set; get; }

        public ICommand Complete { protected set; get; }

        public ICommand Call { protected set; get; }
        #endregion

        private async Task<int> StoreRent()
        {
            return await Task.Run(async () =>
            {
                var stored = new StoredRent();
                stored.Rent = JsonConvert.SerializeObject(_rent);
                stored.Date = DateTime.Now;
                stored.Type = _rent.RentPrice.Vehicle;
                GyroServer.SendOrder(_rent);
                return App.Database.StoreRent(stored);              
            });
        }

        private async Task SendRentToServer()
        {

        }

        public string GetRentInfo()
        {
            return $"{_rent.Client.Name}\n" +
                         $"Документ: {_rent.Client.DocumentType} серия {_rent.Client.Serial} номер {_rent.Client.Number}\n" +
                         $"Всего времени {_rent.RentPrice.Time}\n" +
                         $"Внесено {_rent.RentPrice.Price:c}";
        }

        public RentViewModel(RentModel rent, RentManipulate delete)
        {
            _rent = rent;
            Delete = delete;
            ///Press Button play-stop
            this.ChangeMode = new Command( async(nothing) =>
            {
                try
                {
                    if (_rent.StoredId == 0)
                        rent.StoredId = await StoreRent();
                    _rent.ChangeMode(OnPropertyChanged);
                }
                catch (Exception ex)
                {
                    App.Database.SaveError(new Error { Date = DateTime.Now, Invoker = $"{this.GetType().Name}->ChangeMode", Message = ex.Message });
                }
            });
            ///Delete rent from view
            this.Remove = new Command(async (nothing) =>
            {
                try
                {
                    if (await _rent.CanRemoveAsync())
                        Delete(this);
                }
                catch (Exception ex)
                {
                    App.Database.SaveError(new Error { Date = DateTime.Now, Invoker = $"{this.GetType().Name}->Remove", Message = ex.ToString() });
                }
            });
            this.Call = new Command(() =>
            {
                if (_rent.Client.Phone == "")
                {
                    App.Toast("Номер отсутствует!");
                    return;
                }
                //var ans = await _rent.Confirm($"Вызвать {item.ClientName}?");
                //if (ans == true)
                //{
                Device.OpenUri(new Uri($"tel:{_rent.Client.Phone}"));
                //}
                //else
                //{
                //    App.Toast("Отмена");
                //}
            });
            ///complete rent and remove
            this.Complete = new Command(async (nothing) =>
            {
                try
                {
                    if (await _rent.CanCompleteAsync())
                        Delete(this);
                }
                catch (Exception ex)
                {
                    App.Database.SaveError(new Error { Date = DateTime.Now, Invoker = $"{this.GetType().Name}->Complete", Message = ex.Message });
                }
            });
        }
        #region Bindable properties
        /// <summary>
        /// Time left
        /// </summary>
        public string Time
        {
            protected set
            {
                if (_rent.CurentTime.Time != value)
                {
                    _rent.CurentTime.Time = value;
                    OnPropertyChanged("Time");
                }
            }
            get
            {
                return _rent.CurentTime.Time;
            }
        }
        /// <summary>
        /// Time color
        /// </summary>
        public Color TimeColor
        {
            protected set
            {
                if (_rent.CurentTime.Color != value)
                {
                    _rent.CurentTime.Color = value;
                    OnPropertyChanged("TimeColor");
                }
            }
            get
            {
                return _rent.CurentTime.Color;
            }
        }
        /// <summary>
        /// Client name
        /// </summary>
        public string ClientName
        {
            protected set
            {
                if (_rent.Client.Name != value)
                {
                    _rent.Client.Name = value;
                    OnPropertyChanged("ClientName");
                }
            }
            get
            {
                return _rent.Client.Name;
            }
        }
        /// <summary>
        /// image stop or play
        /// </summary>
        public string Image
        {
            protected set
            {
                OnPropertyChanged("Image");
            }
            get
            {
                return _rent._inProgress ? "stop.png" : "play.png";
            }
        }

        public double Diff { get { return _rent.Diff; } }
        #endregion
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this,
                    new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RentView : ViewCell
    {
        #region Controls
        private Label _titleLabel;

        private Label _descLabel;

        private Image _button;

        private TapGestureRecognizer _tap;

        #endregion
        #region Menu
        MenuItem _deleteAction;
        MenuItem _callAction;
        MenuItem _completeAction;
        #endregion
        protected override void OnBindingContextChanged()
        {
            _tap.NumberOfTapsRequired = 1;
            _button.GestureRecognizers.Add(_tap);
            base.OnBindingContextChanged();
        }
        public RentView()
        {
            try
            {
                _titleLabel = new Label
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 25,
                    FontAttributes = Xamarin.Forms.FontAttributes.Bold,
                    TextColor = Color.Black
                };
                _titleLabel.SetBinding(Label.TextProperty, "ClientName");
                _descLabel = new Label
                {
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    FontSize = 12,
                    TextColor = Color.Black
                };
                _descLabel.SetBinding(Label.TextProperty, "Time");
                _descLabel.SetBinding(Label.TextColorProperty, "TimeColor");
                StackLayout viewLayoutItem = new StackLayout()
                {
                    HorizontalOptions = LayoutOptions.StartAndExpand,
                    Orientation = StackOrientation.Vertical,
                    Children = { _titleLabel, _descLabel }
                };
                _tap = new TapGestureRecognizer();
                _tap.SetBinding(TapGestureRecognizer.CommandProperty, "ChangeMode");
                _button = new Image
                {
                    Source = "play.png",
                    HorizontalOptions = LayoutOptions.Start,
                    VerticalOptions = LayoutOptions.Start,
                    BackgroundColor = Color.Transparent,
                };
                _button.SetBinding(Image.SourceProperty, "Image");
                _completeAction = new MenuItem { Icon = "thumb.png", IsDestructive = true, Text = "Завершить" };
                _completeAction.SetBinding(MenuItem.CommandProperty, "Complete");
                _completeAction.SetBinding(MenuItem.CommandParameterProperty, new Binding("."));
                _callAction = new MenuItem { Icon = "phone.png", IsDestructive = true, Text = "Вызвать" };
                _callAction.SetBinding(MenuItem.CommandProperty, "Call");
                _callAction.SetBinding(MenuItem.CommandParameterProperty, new Binding("."));
                _deleteAction = new MenuItem { Icon = "garbage.png", IsDestructive = true, Text = "Удалить" };
                _deleteAction.SetBinding(MenuItem.CommandProperty, "Remove");
                _deleteAction.SetBinding(MenuItem.CommandParameterProperty, new Binding("."));
                ContextActions.Add(_callAction);
                ContextActions.Add(_deleteAction);
                ContextActions.Add(_completeAction);
                StackLayout viewLayout = new StackLayout()
                {
                    Orientation = StackOrientation.Horizontal,
                    Padding = new Thickness(25, 10, 25, 15),
                    Children = { viewLayoutItem, _button }
                };
                View = viewLayout;
            }
            catch (Exception ex)
            {
                App.Database.SaveError(new Mob.Dto.Error
                {
                    Date = DateTime.Now,
                    Invoker = this.GetType().Name,
                    Message = ex.Message
                });
            }
        }
    }

    #endregion
    public class RentInfo : TabbedPage
    {

        private App.DoNotification _doNotification;
        public RentInfo(App.DoNotification doNotification)
        {
            try
            {
                _doNotification = doNotification;
                this.Title = "Клиенты";
                this.BarBackgroundColor = Color.Violet;
                this.BarBackgroundColor = Color.WhiteSmoke;
                this.BarTextColor = Color.Black;
                //this.ToolbarItems.Add(new ToolbarItem("test", "adduser.png", async() => { await this.ScaleTo(0.8, 50, Easing.Linear); await Task.Delay(100); await this.ScaleTo(1, 50, Easing.Linear); }));
                this.ItemsSource = new RentInfoTab[]
                {
                new RentInfoTab("Gyro", 1, "wheel.png" ),
                new RentInfoTab("Cycle", 2, "cycle.png" )
                };
                this.ItemTemplate = new DataTemplate(() =>
                {
                    return new RentTabContent();
                });
            }
            catch (Exception ex)
            {
                App.Database.SaveError(new Mob.Dto.Error { Date = DateTime.Now, Invoker = this.GetType().Name, Message = ex.Message });
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

    }

    class RentInfoTab
    {
        public RentInfoTab(string name, int number, string icon)
        {
            this.Name = name;
            this.Number = number;
            this.Icon = icon;
        }
        public string Name { private set; get; }
        public int Number { private set; get; }
        public string Icon { private set; get; }
    }

    public class RentTabContent : ContentPage
    {
        private ObservableCollection<RentViewModel> _rentList;

        private List<PriceInfo> _gyroPrices;
        private List<PriceInfo> _cyclePrices;
        private ListView listView;
        /// <summary>
        /// Configmation contextPage
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private async Task<bool> ConfirmActionResult(string text)
        {
            return await DisplayAlert("Подтвердите", text, "Да", "Нет");
        }
        private Task<string> InputBox(string text)
        {
            // wait in this proc, until user did his input 
            var tcs = new TaskCompletionSource<string>();

            var lblTitle = new Label { Text = text, HorizontalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold };
            var lblMessage = new Label { Text = "Доплата", FontSize = 8 };
            var txtInput = new Entry { Keyboard = Keyboard.Numeric };

            var btnOk = new Button
            {
                Text = "Принять",
                WidthRequest = 100,
                BackgroundColor = Color.FromRgb(0.8, 0.8, 0.8),
            };
            btnOk.Clicked += async (s, e) =>
            {
                // close page
                var result = txtInput.Text;
                await Navigation.PopModalAsync();
                // pass result
                tcs.SetResult(result);
            };

            var btnCancel = new Button
            {
                Text = "Отмена",
                WidthRequest = 100,
                BackgroundColor = Color.FromRgb(0.8, 0.8, 0.8)
            };
            btnCancel.Clicked += async (s, e) =>
            {
                // close page
                await Navigation.PopModalAsync();
                // pass empty result
                tcs.SetResult(null);
            };



            var slButtons = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Children = { btnOk, btnCancel },
            };

            // var PaymentLayout

            var layout = new StackLayout
            {
                Padding = new Thickness(0, 40, 0, 0),
                VerticalOptions = LayoutOptions.StartAndExpand,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Orientation = StackOrientation.Vertical,
                Children = { lblTitle, txtInput, lblMessage, slButtons },
            };

            // create and show page
            var page = new ContentPage();
            page.Content = layout;
            Navigation.PushModalAsync(page);
            // open keyboard
            txtInput.Focus();

            // code is waiting her, until result is passed with tcs.SetResult() in btn-Clicked
            // then proc returns the result
            return tcs.Task;
        }
        /// <summary>
        /// Restore saved rents
        /// </summary>
        private void RestoreRents()
        {
            try
            {
                _rentList.Clear();
                List<StoredRent> storedList = GetStoredRents();
                foreach (var storedRent in storedList)
                {
                    var rent = RestoreRent(storedRent);
                    _rentList.Add(new RentViewModel(rent, DeleteRent));
                }
            }
            catch (Exception ex)
            {
                App.Database.SaveError(new Error { Date = DateTime.Now, Invoker = $"{this.GetType().Name}->RestoreRents", Message = ex.ToString() });
            }
        }
        /// <summary>
        /// Restore rent from database
        /// </summary>
        /// <param name="storedRent"></param>
        /// <returns></returns>
        private RentModel RestoreRent(StoredRent storedRent)
        {
            var rent = JsonConvert.DeserializeObject<RentModel>(storedRent.Rent);
            rent.Confirm = ConfirmActionResult;
            rent.StoredId = storedRent.Id;
            rent.Test = InputBox;
            rent.Navigate = Navigate;
            rent.RecalculateTime(storedRent.Date);
            return rent;
        }
        /// <summary>
        /// Getting stored rents for current tab
        /// </summary>
        /// <returns>List of stored rent</returns>
        private List<StoredRent> GetStoredRents()
        {
            return App.Database.GetStoredRents(this.Title.Substring(0, 1));
        }
        /// <summary>
        /// Add new rent
        /// </summary>
        /// <param name="rentItem"></param>
        private void AddNewRent(RentModel rentItem)
        {
            rentItem.Navigate = Navigate;
            rentItem.Confirm = ConfirmActionResult;
            rentItem.Test = InputBox;
            _rentList.Add(new RentViewModel(rentItem, DeleteRent));
            MoveLastRent();
        }
        /// <summary>
        /// Move last rent for sort rents by time
        /// </summary>
        private void MoveLastRent()
        {
            for (int i = 0; i < _rentList.Count - 1; i++)
            {
                if (_rentList[_rentList.Count - 1].Diff < _rentList[i].Diff)
                {
                    _rentList.Move(_rentList.Count - 1, i);
                    return;
                }
            }
        }
        /// <summary>
        /// Delete rent from view
        /// </summary>
        /// <param name="rent">Rent</param>
        private void DeleteRent(RentViewModel rent)
        {
            try
            {
                _rentList.Remove(rent);
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", "Не могу удалить:(", "OK");
                App.Database.SaveError(new Mob.Dto.Error { Date = DateTime.Now, Invoker = this.GetType().Name, Message = ex.Message });
            }
        }

        protected override void OnBindingContextChanged()
        {
            ///Get prices from database
            switch (this.Title)
            {
                case "Gyro":
                    _gyroPrices = App.Database.GetPrices("G");
                    break;
                case "Cycle":
                    _cyclePrices = App.Database.GetPrices("C");
                    break;
                default: break;
            }
            RestoreRents();
        }

        public RentTabContent()
        {
            try
            {
                this.SetBinding(ContentPage.TitleProperty, "Name");
                this.SetBinding(ContentPage.IconProperty, "Icon");
                this.WidthRequest = 100;
                this.ToolbarItems.Clear();
                this.BackgroundColor = Color.Azure;
                ///==================================================
                listView = new ListView();
                //RestoreRents();
                _rentList = new ObservableCollection<RentViewModel>();
                listView.ItemsSource = _rentList;
                listView.RowHeight = 80;
                listView.BackgroundColor = Color.White;
                listView.ItemTemplate = new DataTemplate(typeof(RentView));
                listView.ItemTapped += async (sender, e) =>
                {
                    RentViewModel item = (RentViewModel)e.Item;
                    await DisplayAlert("Информация", item.GetRentInfo() ,"OK");
                    ((ListView)sender).SelectedItem = null;
                };
                ///==================================================
                var btn = new Button { Text = "Новый", BackgroundColor = Color.White, TextColor = Color.Green };
                btn.Clicked += async (sender, args) =>
                    {
                        var p = this.Title == "Gyro" ? new Person(_gyroPrices) : new Person(_cyclePrices);
                        p.AddNewRentInfo = AddNewRent;
                        await Navigation.PushAsync(p);
                    };
                ///
                StackLayout viewLayout = new StackLayout()
                {
                    Orientation = StackOrientation.Vertical,
                    Padding = new Thickness(5, 10, 5, 0),
                    Children = { listView, btn }
                };
                Content = viewLayout;
            }
            catch (Exception ex)
            {
                App.Database.SaveError(new Mob.Dto.Error { Date = DateTime.Now, Invoker = this.GetType().Name, Message = ex.Message });
            }
        }

        private string Navigate(RentEnd page)
        {
            Navigation.PushModalAsync(page);
            return page.Extra.Text;
        }
    }
}