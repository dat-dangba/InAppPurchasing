using UnityEngine.Purchasing.Security;

namespace DBD.InAppPurchasing
{
    public class TestTangle
    {
        private static byte[] data = System.Convert.FromBase64String(
            "P149s9Aj/+kVnrChksXifQ8PkO6RztzOzpc5PrDKVdPVly/2reLB5KIhLyAQoiEqIqIhISCAOVMAWS2lwQJI8ocMi8NknAhPDHxBzsEdLjwQoiECEC0mKQqmaKbXLSEhISUgI1jVXIw5Q4rmhfg3jcJ9reBd7pPhadHJ6Ed5DqZWd6UPqw+adUvZYJth8qjH/FJlTq0/+BUM2oKnxMNdXl7kuG1GC5yJT7IRTqZBiJ2mMCQc1ifR94A36prL2/L2raH07uRQg9Hn4OP3JaBeKGIwRrzhS2M3CieEYVzOcx7ROJbma3FBdGvzhnF9qJ3Q1s7JQmQGtJKYZom0fj71JADqRaZP+ae0GC0ypTqvfBDNkFSg615dQZp/6hCovIaMoSIjISAh");

        private static int[] order = new int[] { 11, 9, 9, 3, 11, 12, 8, 8, 11, 11, 13, 11, 12, 13, 14 };
        private static int key = 32;

        public static readonly bool IsPopulated = true;

        public static byte[] Data()
        {
            if (IsPopulated == false)
                return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}