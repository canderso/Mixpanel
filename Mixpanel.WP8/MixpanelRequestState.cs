using System;
using System.Net;
using System.Threading;

namespace Mixpanel
{
    internal class MixpanelRequestState
    {
        public string RequestData { get; set; }

        public HttpWebRequest Request { get; set; }
        public HttpWebResponse Response { get; set; }

        public Exception Error { get; set; }
        public object Result { get; set; }

        public ManualResetEvent Completed { get; set; }

        public MixpanelRequestState()
        {
        }
    }
}
