using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace Magician.WebResourceLiveSync.Helpers
{
    public class Toaster
    {
        public const string APP_ID = "Magician.WebResourceLiveSync";

        public void Show(string header, string body)
        {
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            var stringElements = xml.GetElementsByTagName("text");
            
            if (stringElements.Length != 2)
            {
                throw new Exception("Invalid notification template encountered");
            }

            stringElements[0].AppendChild(xml.CreateTextNode(header));
            stringElements[1].AppendChild(xml.CreateTextNode(body));

            var toast = new ToastNotification(xml);

            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }
    }
}
