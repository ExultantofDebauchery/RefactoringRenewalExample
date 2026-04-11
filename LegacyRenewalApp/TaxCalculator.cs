namespace LegacyRenewalApp
{
    public class TaxCalculator
    {
        public decimal GetRate(string country)
        {
            switch (country)
            {
                case"Poland":
                    return 0.23m;
                case"Germany":
                    return 0.19m;
                case "Czech Republic":
                    return 0.21m;
                case "Norway":
                    return 0.25m;
                default:
                    return 0.20m;
            }
        }
    }
}