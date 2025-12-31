using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace IAPSample
{
    public class IAPManager : BaseIAPManager<IAPManager>
    {
        public Button button;
        public Text text;

        private const string productId = "coin_pack_1";

        protected override void Start()
        {
            base.Start();
            Init(b => { });
        }

        protected override void OnConnectCompleted(bool success)
        {
            button.interactable = success;
            text.text = GetLocalPrice(productId);
        }

        protected override void OnPurchaseCompleted(bool success, Product product)
        {
        }

        public void Purchase()
        {
            PurchaseProduct(productId, (b, product) => { });
        }
    }
}