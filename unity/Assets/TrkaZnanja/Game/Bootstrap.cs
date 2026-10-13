// Pokretanje: igra se sama sastavi čim se pritisne Play (u bilo kojoj sceni) ili kad se pokrene build.
using UnityEngine;

namespace TrkaZnanja.Game
{
    public sealed class Bootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (App.I == null) App.Boot();
        }

        void Awake()
        {
            if (App.I == null) App.Boot();
        }
    }
}
