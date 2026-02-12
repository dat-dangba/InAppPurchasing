using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace DBD.InAppPurchasing
{
    public abstract class BaseIAPManager<INSTANCE> : MonoBehaviour
    {
        [SerializeField] protected bool isUseDefaultPrice;
        [SerializeField] protected List<IAPProduct> iapProducts;

        public static INSTANCE Instance { get; private set; }

        private StoreController storeController;

        private List<string> purchasedProductIds = new();

        private bool isPurchasing;
        private string productIdPurchase = "";

        private Action<bool, Product> OnPurchaseProduct;

        private Action<bool> OnConnect;

        public bool IsConnected { get; private set; }

        protected abstract GameObject GetLoading();
        protected abstract bool IsVerifyPurchase();
        protected abstract byte[] GetSecurityData();
        protected abstract void OnConnectCompleted(bool success);
        protected abstract void OnPurchaseCompleted(bool success, Product product, Order order);

        protected virtual void Awake()
        {
            if (Instance == null)
            {
                Instance = GetComponent<INSTANCE>();

                Transform root = transform.root;
                if (root != transform)
                {
                    DontDestroyOnLoad(root);
                }
                else
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnEnable()
        {
        }

        protected virtual void OnDisable()
        {
        }

        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
        }

        protected virtual void FixedUpdate()
        {
        }

        public void Init(Action<bool> OnConnect)
        {
            this.OnConnect = OnConnect;
            ConnectStore();
        }

        private async void ConnectStore()
        {
            try
            {
                storeController = UnityIAPServices.StoreController();

                storeController.OnPurchasePending += OnPurchasePending;
                storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                storeController.OnPurchaseFailed += OnPurchaseFailed;
                storeController.OnPurchasesFetched += OnPurchaseFetched;

                storeController.OnStoreDisconnected += OnStoreDisconnected;
                Debug.Log("iap - Connecting to store.");
                await storeController.Connect();
                Debug.Log("iap - Connected");
                storeController.OnProductsFetchFailed += OnProductsFetchedFailed;
                storeController.OnProductsFetched += OnProductsFetched;

                FetchProducts();

                IsConnected = true;
            }
            catch (Exception e)
            {
                Debug.Log($"iap - error {e.Message}");
                OnConnectCompleted(false);
                OnConnect?.Invoke(false);
            }
        }

        private void FetchProducts()
        {
            var productDefinitions = new List<ProductDefinition>();
            foreach (var item in iapProducts)
            {
                string id = GetProductId(item.ProductId);
                productDefinitions.Add(new ProductDefinition(id, item.ProductType));
            }

            storeController.FetchProducts(productDefinitions);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription obj)
        {
            Debug.Log($"iap - OnStoreDisconnected {obj.message}");
            IsConnected = false;
        }

        private void OnPurchasePending(PendingOrder order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("iap - Could not find product in order.");
                StartCoroutine(PurchaseProductCompleted(false, null, null));
                return;
            }

            if (IsVerifyPurchase())
            {
                bool isVerified = ValidatePurchase(order.Info.Receipt);
                if (!isVerified)
                {
                    Debug.Log($"iap - Validate false - Product: {product.definition.id}");
                    StartCoroutine(PurchaseProductCompleted(false, product, order));
                    return;
                }
            }

            //Add the purchased product to the players inventory
            AddPurchasedProductId(order.CartOrdered.Items());
            StartCoroutine(PurchaseProductCompleted(true, product, order));

            Debug.Log($"iap - Purchase complete - Product: {product.definition.id}");

            storeController.ConfirmPurchase(order);
        }

        private bool ValidatePurchase(string receipt)
        {
#if UNITY_ANDROID
            CrossPlatformValidator validator = new CrossPlatformValidator(GetSecurityData(), Application.identifier);
            try
            {
                // Trả về danh sách sản phẩm hợp lệ trong receipt
                var result = validator.Validate(receipt);
                foreach (IPurchaseReceipt productReceipt in result)
                {
                    if (productReceipt.productID == productIdPurchase)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (IAPSecurityException)
            {
                return false;
            }
#elif UNITY_IOS || UNITY_EDITOR
            return true;
#endif
            return false;
        }

        private void OnPurchaseConfirmed(Order order)
        {
            var product = GetFirstProductInOrder(order);
            if (product == null)
            {
                Debug.Log("iap - Could not find product in purchase confirmation.");
            }

            switch (order)
            {
                case ConfirmedOrder confirmedOrder:
                    Debug.Log($"iap - Purchase confirmed id {product?.definition.id}");
                    break;
                case FailedOrder failedOrder:
                    Debug.Log($"iap - Confirmation failed id {product?.definition.id}");
                    Debug.Log($"iap - Confirmation failed reason {failedOrder.FailureReason.ToString()}");
                    Debug.Log($"iap - Confirmation failed details {failedOrder.Details}");
                    break;
                default:
                    Debug.Log("iap - Unknown OnPurchaseConfirmed result.");
                    break;
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            var product = GetFirstProductInOrder(order);

            Debug.Log($"iap - Purchase failed id {product?.definition.id}");
            Debug.Log($"iap - Purchase failed reason {order.FailureReason.ToString()}");
            Debug.Log($"iap - Purchase failed details {order.Details}");

            StartCoroutine(PurchaseProductCompleted(false, product, order));
        }

        private void OnProductsFetched(List<Product> products)
        {
            purchasedProductIds.Clear();
            Debug.Log($"iap - Products fetched successfully for {products.Count} products.");
            foreach (var item in products)
            {
                storeController.CheckEntitlement(item);
                Debug.Log($"iap - Products fetched id {item.definition.id}");
            }

            storeController.FetchPurchases();
        }

        private void OnProductsFetchedFailed(ProductFetchFailed failure)
        {
            Debug.Log($"iap - Products fetch failed for {failure.FailedFetchProducts.Count} products");
            Debug.Log($"iap - Failure reason {failure.FailureReason}");
            foreach (var item in failure.FailedFetchProducts)
            {
                Debug.Log($"iap - Fetched failed id {item.id}");
            }
        }

        private void OnPurchaseFetched(Orders orders)
        {
            Debug.Log($"iap - Purchase fetched ConfirmedOrders {orders.ConfirmedOrders.Count}");
            purchasedProductIds.Clear();
            foreach (var item in orders.ConfirmedOrders)
            {
                AddPurchasedProductId(item.CartOrdered.Items());
            }

            OnConnectCompleted(true);
            OnConnect?.Invoke(true);
        }

        private void AddPurchasedProductId(IReadOnlyList<CartItem> cartItems)
        {
            Debug.Log($"iap - Purchase fetched cartItems {cartItems.Count}");
            foreach (var item in cartItems)
            {
                if (item.Product.definition.type is ProductType.NonConsumable or ProductType.Subscription)
                {
                    Debug.Log($"iap - Purchase fetched {item.Product.definition.id}");
                    purchasedProductIds.Add(item.Product.definition.id);
                }
            }
        }

        private Product GetFirstProductInOrder(Order order)
        {
            return order.CartOrdered.Items().First()?.Product;
        }

        protected virtual Product GetProduct(string productId)
        {
            return storeController?.GetProducts()
                .FirstOrDefault(product => product.definition.id == productId);
        }

        public virtual Product GetProductByIAPProductId(string iapProductId)
        {
            string productId = GetProductId(iapProductId);
            return GetProduct(productId);
        }

        protected virtual string GetProductId(string productId)
        {
            string id;
#if UNITY_EDITOR || UNITY_ANDROID
            id = productId;
#else
        id = $"{Application.identifier}.{productId}";
#endif
            return id;
        }

        protected virtual bool IsInitialized()
        {
            return storeController != null && IsConnected;
        }

        public virtual void PurchaseProduct(string productId, Action<bool, Product> OnPurchaseProduct)
        {
            Debug.Log($"iap - purchasedProductIds {purchasedProductIds.Count}");
            if (isPurchasing)
            {
                return;
            }

            GetLoading().SetActive(true);
            isPurchasing = true;
            this.OnPurchaseProduct = OnPurchaseProduct;
            productIdPurchase = GetProductId(productId);

            if (!IsInitialized())
            {
                Debug.Log("iap - Store not connect.");

                StartCoroutine(PurchaseProductCompleted(false, null, null));
                return;
            }

            var product = GetProduct(productIdPurchase);

            if (product == null)
            {
                Debug.Log("iap - Could not find product purchase.");
                StartCoroutine(PurchaseProductCompleted(false, null, null));
                return;
            }

            Debug.Log($"iap - Purchase product {productIdPurchase}");
            storeController.PurchaseProduct(product);
        }

        private IEnumerator PurchaseProductCompleted(bool success, Product product, Order order)
        {
            yield return new WaitForSecondsRealtime(0.2f);

            if (isPurchasing)
            {
                isPurchasing = false;
                OnPurchaseCompleted(success, product, order);
                OnPurchaseProduct?.Invoke(success, product);
            }

            productIdPurchase = "";
            GetLoading().SetActive(false);
        }

        public virtual void RestorePurchases(Action<bool, string> callback)
        {
            if (!IsInitialized())
            {
                Debug.Log("iap - Store not connect.");
                callback?.Invoke(false, "Store not connect.");
                return;
            }

            GetLoading().SetActive(true);
            storeController.RestoreTransactions((b, s) =>
            {
                StartCoroutine(RestorePurchaseCompleted(callback, b, s));
            });
        }

        private IEnumerator RestorePurchaseCompleted(Action<bool, string> callback, bool b, string s)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            callback.Invoke(b, s);
            GetLoading().SetActive(false);
        }

        public virtual bool IsRemoveAds()
        {
            foreach (var item in iapProducts)
            {
                if (item.IsRemoveAds && IsProductPurchase(item.ProductId))
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool IsProductPurchase(string id)
        {
            string productId = GetProductId(id);
            return purchasedProductIds.Contains(productId);
        }

        public virtual string GetLocalPrice(string productId)
        {
            string defaultPrice = $"$ {GetDefaultPrice(productId)}";

            if (isUseDefaultPrice) return defaultPrice;

            if (!IsInitialized())
            {
                return defaultPrice;
            }

            string id = GetProductId(productId);
            Product product = GetProduct(id);
            if (product?.metadata == null)
            {
                return defaultPrice;
            }

            return product.metadata.localizedPriceString ?? defaultPrice;
        }

        private float GetDefaultPrice(string productId)
        {
            foreach (var item in iapProducts)
            {
                if (item.ProductId == productId)
                {
                    return item.ProductPrice;
                }
            }

            return 0;
        }
    }
}