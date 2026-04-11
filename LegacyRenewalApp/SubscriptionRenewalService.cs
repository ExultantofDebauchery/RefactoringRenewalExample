using System;
using System.Net.Http.Headers;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService
    {
        private readonly CustomerRepository _customerRepository=new CustomerRepository();
        private readonly SubscriptionPlanRepository _subscriptionPlanRepository=new SubscriptionPlanRepository();
        private readonly DiscountCalculator _discountCalculator=new DiscountCalculator();
        private readonly TaxCalculator _taxCalculator=new TaxCalculator();
        private readonly PaymentFeeCalculator _paymentFeeCalculator=new PaymentFeeCalculator();
        private readonly IBillingGateway _billingGateway=new BillingGateway();
        private readonly SupportFeeCalculator _supportFeeCalculator=new SupportFeeCalculator();
        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            Validate(customerId,planCode,seatCount,paymentMethod);
            

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();

            var customerRepository = new CustomerRepository();
            var planRepository = new SubscriptionPlanRepository();

            var customer = customerRepository.GetById(customerId);
            var plan = planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = CalculateBaseAmount(plan, seatCount);
            var (discount,discountNotes)=_discountCalculator.CalculateDiscount(customer,plan,seatCount,baseAmount,useLoyaltyPoints);
            decimal subtotal=Math.Max(baseAmount-discount,300m);
            decimal supportFee = _supportFeeCalculator.CalculateFee(includePremiumSupport, normalizedPaymentMethod);
            var (paymentFee, paymentNotes) =
                _paymentFeeCalculator.Calculate(normalizedPaymentMethod, supportFee + subtotal);
            decimal taxRate = _taxCalculator.GetRate(customer.Country);
            decimal taxBase = subtotal + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = Math.Max(taxBase + taxAmount,500m);

            string notes=(discountNotes+paymentNotes).Trim();
            if (subtotal == 300m)
            {
                notes+="minimum invoice amount applied; ";
            }

            if (includePremiumSupport)
            {
                notes += "premium support included; ";
            }
            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            LegacyBillingGateway.SaveInvoice(invoice);

            SendEmail(customer, invoice, planCode);

            return invoice;
        }

        private void Validate(int customerId, string planCode, int seatCount, string paymentMethod)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer id must be positive");
            }

            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new ArgumentException("Plan code is required");
            }

            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be positive");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new ArgumentException("Payment method is required");
            }
        }

        private decimal CalculateBaseAmount(SubscriptionPlan plan, int seatcount)
        {
            return (plan.MonthlyPricePerSeat * seatcount * 12m) + plan.SetupFee;
        }

        private void SendEmail(Customer customer, RenewalInvoice invoice, string plancode)
        {
            if (string.IsNullOrWhiteSpace(customer.Email))return;
            string subject = "Subscription renewal invoice";
            string body =$"Hello {customer.FullName}, your renewal for plan {plancode} has been prepared.Final amount: {invoice.FinalAmount:F2}.";
            _billingGateway.SendEmail(customer.Email, subject, body);
        }
    }
}
