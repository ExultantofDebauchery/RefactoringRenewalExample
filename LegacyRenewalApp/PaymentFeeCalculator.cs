using System;

namespace LegacyRenewalApp
{
    public class PaymentFeeCalculator
    {
        public (decimal fee, string note) Calculate(string method, decimal amount)
        {
            switch (method)
            {
                case "CARD":
                    return (amount*0.02m, "card payment fee; ");
                case "BANK_TRANSFER":
                    return (amount*0.01m, "bank transfer fee; ");
                case "PAYPAL":
                    return (amount*0.035m, "paypal fee; ");
                case "INVOICE":
                    return (amount*0m, "invoice payment; ");
                default:
                    throw new ArgumentException("Invalid payment method");
            }
        }
    }
}