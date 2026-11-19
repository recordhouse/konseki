using UnityEngine;

namespace Konseki
{
    public enum AlleyVariant
    {
        ShowaIzakaya = 0,
        NeonYokocho = 1,
        QuietResidential = 2
    }

    public static class AlleyVariantInfo
    {
        private const string PreferenceKey = "konseki.alley-variant";

        public static AlleyVariant Current
        {
            get => (AlleyVariant)Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, 2);
            set
            {
                PlayerPrefs.SetInt(PreferenceKey, (int)value);
                PlayerPrefs.Save();
            }
        }

        public static string DisplayName(AlleyVariant variant)
        {
            switch (variant)
            {
                case AlleyVariant.NeonYokocho:
                    return "비 내리는 네온 요코초";
                case AlleyVariant.QuietResidential:
                    return "고요한 일본 주택가";
                default:
                    return "쇼와 시대 이자카야 골목";
            }
        }
    }
}
