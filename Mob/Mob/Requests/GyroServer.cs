using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mob.Requests
{
    public class GyroServerResponse
    {
        public int Status { get; }
        private Dictionary<string, object> Data;
        public GyroServerResponse(string responseString)
        {
            var response = JObject.Parse(responseString);
            Status = (int)response["status"];
            try
            {
                Data = response["data"].ToObject<Dictionary<string, object>>();
            }
            catch(Exception ex)
            {

            }
        }
        public object GetData(string Key)
        {
            if (Data.ContainsKey(Key))
                return Data[Key];
            else
                return null;
        }
    }
    public static class GyroServer
    {
        private static readonly string _server = "http://gyro.snouwer.ru/";

        private static readonly HttpClient client = new HttpClient();
        /// <summary>
        /// Регистрация аренды на сервере
        /// </summary>
        /// <param name="rent"></param>
        async public static void SendOrder(RentModel rent)
        {
            try
            {
                var hash = App.Database.GetSettingsByName("UserMD");
                var values = new Dictionary<string, string>
                {
                   { "hash",  hash == null ? "" : hash.Vlaue},
                   { "client_name", rent.Client.Name },
                   { "total", rent.RentPrice.Price.ToString() }
                };

                var content = new FormUrlEncodedContent(values);

                var ServerResponse = await client.PostAsync(_server + "orders", content);

                var responseString = await ServerResponse.Content.ReadAsStringAsync();
                var response = new GyroServerResponse(responseString);

                if (response.Status != 200)
                {
                    App.Toast("Данные не отправлены на сервер!");
                }
            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// Отправка данных работкника на сервер
        /// </summary>
        async public static void SendUserData()
        {
            try
            {
                var values = new Dictionary<string, string>
                {
                   { "name", App.Database.GetSettingsByName("UserName").Vlaue},
                   { "location", App.Database.GetSettingsByName("Location").Vlaue}
                };

                var content = new FormUrlEncodedContent(values);

                var serverResponse = await client.PostAsync(_server + "workers", content);

                var responseString = await serverResponse.Content.ReadAsStringAsync();

                var response = new GyroServerResponse(responseString);

                if (response.Status == 200)
                {
                    var mdUser = new UserSettings { Name = "UserMD", Vlaue = response.GetData("Hash").ToString() };

                    App.Database.SaveUserSettings(mdUser);

                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
