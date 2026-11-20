namespace Konseki
{
    public enum AlleyVariant
    {
        QuietResidential = 0
    }

    public static class AlleyVariantInfo
    {
        public static AlleyVariant Current => AlleyVariant.QuietResidential;

        public static string DisplayName(AlleyVariant variant)
        {
            return "고요한 일본 주택가";
        }
    }
}
