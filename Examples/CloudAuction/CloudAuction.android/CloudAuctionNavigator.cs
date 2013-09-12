using System;

using Android.App;
using Android.Content;
using CloudAuction.Shared;

namespace CloudAuction
{
    public class CloudAuctionNavigator : ICloudAuctionNavigator
    {
        private void Navigate(object navigationContext, Type type)
        {
            var activity = (Activity)navigationContext;
            var intent = new Intent(activity, type);
            activity.StartActivity(intent);
        }

        public void NavigateToAuctionView(object navigationContext)
        {
            Navigate(navigationContext, typeof(AuctionView));
        }

        public void NavigateToOrderView(object navigationContext)
        {
            Navigate(navigationContext, typeof(OrderView));
        }

        public void NavigateToOrderResultView(object navigationContext)
        {
            throw new NotImplementedException();
        }
    }
}