using UnityEngine;
using UnityEngine.UI;

namespace Riptide.Demos.PlayerHosted
{
    public class UIManager : MonoBehaviour
    {
        private static UIManager _singleton;
        public static UIManager Singleton
        {
            get => _singleton;
            private set
            {
                if (_singleton == null)
                    _singleton = value;
                else if (_singleton != value)
                {
                    Debug.Log($"{nameof(UIManager)} instance already exists, destroying object!");
                    Destroy(value);
                }
            }
        }

        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject gameMenu;
        [SerializeField] private InputField usernameField;

        [SerializeField] private GameObject gameUI;
        [SerializeField] private Slider healthbar;
        [SerializeField] private WeaponUI pistolUI;
        [SerializeField] private WeaponUI teleporterUI;
        [SerializeField] private WeaponUI laserUI;
        [SerializeField] private InputField hostIPField;
        [SerializeField] private AudioSource hurtAudio;



        internal string Username => usernameField.text;

        private void Awake()
        {
            Singleton = this;
        }

        public void HostClicked()
        {
            mainMenu.SetActive(false);
            gameMenu.SetActive(true);
            gameUI.SetActive(true);  

            NetworkManager.Singleton.StartHost();
        }

        public void HealthUpdated(float health, float maxHealth, bool playHurtSound)
        {
            healthbar.value = health / maxHealth;

            if (playHurtSound)
                hurtAudio.Play();
        }

        public void JoinClicked()
        {
            if (string.IsNullOrEmpty(hostIPField.text))
            {
                Debug.Log("Enter an IP!");
                return;
            }

            NetworkManager.Singleton.JoinGame(hostIPField.text);
            mainMenu.SetActive(false);
            gameMenu.SetActive(true);
            gameUI.SetActive(true);
        }

        public void LeaveClicked()
        {
            NetworkManager.Singleton.LeaveGame();
            BackToMain();
        }

        internal void BackToMain()
        {
            mainMenu.SetActive(true);
            gameMenu.SetActive(false);
            gameUI.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        internal void UpdateUIVisibility()
        {
            if (Cursor.lockState == CursorLockMode.None)
                gameMenu.SetActive(true);
            else
                gameMenu.SetActive(false);
        }

        public void AmmoUpdated(WeaponType type, byte loaded, ushort total)
        {
            switch (type)
            {
                case WeaponType.pistol:
                    pistolUI.UpdateAmmo(loaded, total);
                    break;
                case WeaponType.teleporter:
                    teleporterUI.UpdateAmmo(loaded, total);
                    break;
                case WeaponType.laser:
                    laserUI.UpdateAmmo(loaded, total);
                    break;
                default:
                    Debug.Log($"Can't update ammo display for unknown weapon type '{type}'!");
                    break;
            }
        }

        public void ActiveWeaponUpdated(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.none:
                    pistolUI.SetActive(false);
                    teleporterUI.SetActive(false);
                    laserUI.SetActive(false);
                    break;
                case WeaponType.pistol:
                    pistolUI.SetActive(true);
                    teleporterUI.SetActive(false);
                    laserUI.SetActive(false);
                    break;
                case WeaponType.teleporter:
                    pistolUI.SetActive(false);
                    teleporterUI.SetActive(true);
                    laserUI.SetActive(false);
                    break;
                case WeaponType.laser:
                    pistolUI.SetActive(false);
                    teleporterUI.SetActive(false);
                    laserUI.SetActive(true);
                    break;
                default:
                    Debug.Log($"Can't set UI as active for unknown weapon type '{type}'!");
                    break;
            }
        }
    }
}
