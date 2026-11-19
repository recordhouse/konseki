using UnityEngine;

namespace Konseki
{
    /// <summary>프로토타입 단계의 남성 주인공 설정을 한곳에 보관한다.</summary>
    public sealed class ProtagonistIdentity : MonoBehaviour
    {
        [SerializeField] private string characterName = "Ren";
        [SerializeField] private string role = "Male protagonist";

        public string CharacterName => characterName;
        public string Role => role;
    }
}
