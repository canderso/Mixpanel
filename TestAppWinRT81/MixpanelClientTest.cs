using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Mixpanel;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace TestAppWinRT81
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
        public async Task WINRT_TrackEventTest1()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            TrackingEvent evt = GetEvent1();
            await client.Track<TrackingEvent>(evt);
        }

        [TestMethod]
        public async Task WINRT_TrackEventTest2()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            TrackingEvent evt = GetEvent2();
            await client.Track<TrackingEvent>(evt);
        }

        [TestMethod]
        public async Task WINRT_ProfileUpdateTest1()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate1();
            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_ProfileUpdateTest2()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate2();
            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_ProfileUpdateTest3()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate3();
            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_ProfileUpdateTest4()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate4();
            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_ProfileUpdateTest5()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate5();
            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_ProfileUpdateTest6()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate6();
            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_ProfileUpdateTest7()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            ProfileUpdate pu = GetProfileUpdate7();
            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_RevenueTrackingTest()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();
            client.IsGeolocationEnabled = false;
            ProfileUpdate pu = GetRevenueTracking();
            client.IsVerboseEnabled = true;

            await client.Track<ProfileUpdate>(pu);
        }

        [TestMethod]
        public async Task WINRT_TrySendLocalElementsTest()
        {
            MixpanelClient client = await MixpanelClient.GetCurrentClient();

            TrackingEvent e1 = GetEvent1();
            await client.SaveElement<TrackingEvent>(e1);

            TrackingEvent e2 = GetEvent2();
            await client.SaveElement<TrackingEvent>(e2);

            ProfileUpdate p1 = GetProfileUpdate1();
            await client.SaveElement<ProfileUpdate>(p1);

            ProfileUpdate p2 = GetProfileUpdate2();
            await client.SaveElement<ProfileUpdate>(p2);

            ProfileUpdate p3 = GetProfileUpdate3();
            await client.SaveElement<ProfileUpdate>(p3);

            ProfileUpdate p4 = GetProfileUpdate4();
            await client.SaveElement<ProfileUpdate>(p4);

            ProfileUpdate p5 = GetProfileUpdate5();
            await client.SaveElement<ProfileUpdate>(p5);

            ProfileUpdate p6 = GetProfileUpdate6();
            await client.SaveElement<ProfileUpdate>(p6);

            ProfileUpdate p7 = GetProfileUpdate7();
            await client.SaveElement<ProfileUpdate>(p7);

            await client.TrySendLocalElements();
        }
    }
}
