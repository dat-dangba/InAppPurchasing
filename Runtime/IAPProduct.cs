using System;
using UnityEngine.Purchasing;

[Serializable]
public class IAPProduct
{
    public string ProductId;
    public float ProductPrice;
    public ProductType ProductType;
    public bool IsRemoveAds;
}