namespace LegacyRenewalApp
{
    public class SupportFeeCalculator
    {
        public decimal CalculateFee(bool include,string plancode)
        {
            if (!include)
            { 
                return 0m;
            }

            switch (plancode)
            {
                case "START":
                    return 250m;
                case "PRO":
                    return 400m;
                case "ENTERPRISE":
                    return 700m;
                default:
                    return 0m;
            }
        }
    }
}