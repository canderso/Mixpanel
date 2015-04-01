using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mixpanel;
using System.Collections.Generic;

namespace TestAppNet4Client
{
    /* All tests come from examples of this page: https://mixpanel.com/help/reference/http */

    [TestClass]
    public class MixpanelClientTest
    {
        private const string Token = "0f03b4b28b0340e39f287256ea850bd7";

        private TrackingEvent GetEvent1()
        {
            // Sample event tracking
            TrackingEvent evt = new TrackingEvent("Signed Up");
            evt.Properties = new TrackingEventProperties(Token);
            evt.Properties.DistinctId = "13793";
            evt.Properties.All["Referred by"] = "Friend";
            return evt;
        }

        private TrackingEvent GetEvent2()
        {
            // other sample using special & custom properties

            TrackingEvent evt = new TrackingEvent("Level Complete");
            evt.Properties = new TrackingEventProperties(Token);
            evt.Properties.DistinctId = "13793";
            evt.Properties.Time = MixpanelClient.ToEpochTime(DateTime.Now);
            evt.Properties.IP = "203.0.113.9";
            evt.Properties.Tag = "Tim Trefren";
            evt.Properties.All["Level Number"] = 9;
            return evt;
        }

        private ProfileUpdate GetProfileUpdate1()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.Set);
            pu.IP = "123.123.123.123";
            pu.OperationValues["Address"] = "1313 Mockingbird Lane";
            pu.OperationValues["Birthday"] = "1948-01-01";
            return pu;
        }

        private ProfileUpdate GetProfileUpdate2()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.SetOnce);
            pu.OperationValues["First login date"] = MixpanelClient.ConvertToMixpanelDate(DateTime.Now);
            return pu;
        }

        private ProfileUpdate GetProfileUpdate3()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.Add);
            pu.OperationValues["Coins gathered"] = 12;
            return pu;
        }

        private ProfileUpdate GetProfileUpdate4()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.Append);
            pu.OperationValues["Power Ups"] = "Bubble Lead";
            return pu;
        }

        private ProfileUpdate GetProfileUpdate5()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.Union);
            pu.OperationValues["Items purchased"] = new List<string>() { "socks", "shirts" };
            return pu;
        }

        private ProfileUpdate GetProfileUpdate6()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.Unset);
            pu.UnsetValueList = new List<string>() { "Days overdue" };
            return pu;
        }

        private ProfileUpdate GetProfileUpdate7()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.Delete);
            return pu;
        }

        private ProfileUpdate GetRevenueTracking()
        {
            ProfileUpdate pu = new ProfileUpdate(Token, "13793", ProfileUpdateOperation.Append);
            Dictionary<string, object> transactions = new Dictionary<string, object>();
            transactions["$time"] = "2013-01-03T09:00:00";
            transactions["$amount"] = 25.34f;
            pu.OperationValues["$transactions"] = transactions;
            return pu;
        }

        [TestMethod]
        public void NETFX_TrackEventTest1()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            TrackingEvent evt = GetEvent1();
            client.Track(evt);
        }

        [TestMethod]
        public void NETFX_TrackEventTest2()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            TrackingEvent evt = GetEvent2();
            client.Track(evt);
        }

        [TestMethod]
        public void NETFX_ProfileUpdateTest1()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate1();
            client.Track(pu);
        }

        [TestMethod]
        public void NETFX_ProfileUpdateTest2()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate2();
            client.Track(pu);
        }

        [TestMethod]
        public void NETFX_ProfileUpdateTest3()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate3();
            client.Track(pu);
        }

        [TestMethod]
        public void NETFX_ProfileUpdateTest4()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate4();
            client.Track(pu);
        }

        [TestMethod]
        public void NETFX_ProfileUpdateTest5()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate5();
            client.Track(pu);
        }

        [TestMethod]
        public void NETFX_ProfileUpdateTest6()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate6();
            client.Track(pu);
        }

        [TestMethod]
        public void NETFX_ProfileUpdateTest7()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate7();
            client.Track(pu);
        }

        [TestMethod]
        public void NETFX_RevenueTrackingTest()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetRevenueTracking();
            client.IsVerboseEnabled = true;
            client.IsGeolocationEnabled = false;
            client.Track(pu);
        }


        [TestMethod]
        public void NETFX_TrySendLocalElementsTest()
        {
            MixpanelClient client = MixpanelClient.GetCurrentClient();

            TrackingEvent e1 = GetEvent1();
            client.SaveElement(e1);

            TrackingEvent e2 = GetEvent2();
            client.SaveElement(e2);

            ProfileUpdate p1 = GetProfileUpdate1();
            client.SaveElement(p1);

            ProfileUpdate p2 = GetProfileUpdate2();
            client.SaveElement(p2);

            ProfileUpdate p3 = GetProfileUpdate3();
            client.SaveElement(p3);

            ProfileUpdate p4 = GetProfileUpdate4();
            client.SaveElement(p4);

            ProfileUpdate p5 = GetProfileUpdate5();
            client.SaveElement(p5);

            ProfileUpdate p6 = GetProfileUpdate6();
            client.SaveElement(p6);

            ProfileUpdate p7 = GetProfileUpdate7();
            client.SaveElement(p7);

            client.TrySendLocalElements();
        }
    }
}
