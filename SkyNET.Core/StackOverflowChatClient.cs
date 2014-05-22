using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SkyNET
{
    using Web;

    public class StackOverflowChatClient : IChatClient
    {
        private Client Client { get; set; }
        public bool IsLoggedIn { get; set; }

        private string Fkey { get; set; }
        private List<string> _roomsIn = new List<string>();

        public void Login(BotCredentials credentials)
        {
            Client = new Client();
            try
            {
                LoginToStackExchange(credentials);
                LoginToStackOverflow();
                LoginToChat();
                IsLoggedIn = true;
            }
            catch
            {
                IsLoggedIn = false;
                throw;
            }
        }

        public void EnterRoom(string room)
        {
            this._roomsIn.Add(room);
            Client.Request(String.Format("http://chat.stackoverflow.com/rooms/{0}/", room)).Get();
        }

        public void LeaveRoom(string room)
        {
            this._roomsIn.Remove(room);
            Client.Request(string.Format("http://chat.stackoverflow.com/rooms/{0}/leave", room)).Get();
        }

        // TODO: Should the message include the fact that it is a mass broadcast?  i.e. should we append a default message to the mass broadcast messages?
        /// <summary>
        /// Sends a message to all rooms in which the bot is residing
        /// </summary>
        /// <param name="message"></param>
        public void SendMassMessage(string message)
        {
            string form = string.Format("text={0}&fkey={1}", Uri.EscapeDataString(message), this.Fkey);
            foreach (var room in _roomsIn)
            {
                Client.Request(string.Format("http://chat.stackoverflow.com/chats/{0}/messages/new", room)).Post(form, "application/x-www-form-urlencoded");
            }
        }

        /// <summary>
        /// Sends a message to the specified rooms - Bot does not have to be in the room for this to work.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="rooms"></param>
        public void SendMassMessage(string message, IEnumerable<string> rooms)
        {
            string form = string.Format("text={0}&fkey={1}", Uri.EscapeDataString(message), this.Fkey);
            foreach (var room in rooms)
            {
                Client.Request(string.Format("http://chat.stackoverflow.com/chats/{0}/messages/new", room)).Post(form, "application/x-www-form-urlencoded");
            }
        }

        public void SendMessage(string message, string room)
        {
            string form = string.Format("text={0}&fkey={1}", Uri.EscapeDataString(message), this.Fkey);
            Client.Request(string.Format("http://chat.stackoverflow.com/chats/{0}/messages/new", room)).Post(form, "application/x-www-form-urlencoded");
        }

        /// <summary>
        /// Logs in to Stack Exchange using the provided credentials via Open Id 
        /// </summary>
        /// <param name="credentials"></param>
        private void LoginToStackExchange(BotCredentials credentials)
        {
            Response loginResponse = Client.Request("https://openid.stackexchange.com/account/login/").Get();
            string fkey = GetFKeyFromHtml(loginResponse.Content);
            string form = string.Format("email={0}&password={1}&fkey={2}", Uri.EscapeDataString(credentials.Username), Uri.EscapeDataString(credentials.Password), fkey);
            Client.Request("https://openid.stackexchange.com/account/login/submit/").Post(form, "application/x-www-form-urlencoded");
        }

        /// <summary>
        /// Logs in to Stack Overflow
        /// </summary>
        private void LoginToStackOverflow()
        {
            Response loginResponse = Client.Request("http://stackoverflow.com/users/login/").Get();
            string fkey = GetFKeyFromHtml(loginResponse.Content);
            string form = string.Format("openid_identifier={0}&fkey={1}", Uri.EscapeDataString("https://openid.stackexchange.com/"), fkey);
            Client.Request("http://stackoverflow.com/users/authenticate/").Post(form, "application/x-www-form-urlencoded");
        }

        /// <summary>
        /// Logs in to Stack Overflow Chat and sets the FKey property for re-use
        /// </summary>
        private void LoginToChat()
        {
            Response chatResponse = Client.Request("http://chat.stackoverflow.com/").Get();
            Fkey = GetFKeyFromHtml(chatResponse.Content);
        }

        private string GetFKeyFromHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.SelectSingleNode("//input[@name='fkey']").Attributes["value"].Value;
        }

        public event System.EventHandler<MessageReceivedEventArgs> MessageReceived;

        public event System.EventHandler<UserEnteredEventArgs> UserEntered;

        public event System.EventHandler<UserLeftEventArgs> UserLeft;
    }
}
