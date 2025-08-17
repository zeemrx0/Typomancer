using PurrNet;
using TMPro;
using UnityEngine;

namespace LNE.Spells
{
    public class SpellInputPresenter : NetworkBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TMP_Text _expectedWord;

        public bool IsActive => _inputField.gameObject.activeSelf;
        public string InputText => _inputField.text;

        private void Awake()
        {
            SetActive(false);
            _inputField.onDeselect.AddListener(OnDeselect);
        }

        private void OnDeselect(string _)
        {
            FocusInputField();
        }

        protected override void OnDespawned()
        {
            _inputField.onDeselect.RemoveListener(OnDeselect);
        }

        public void SetActive(bool active)
        {
            _expectedWord.gameObject.SetActive(active);
            _inputField.gameObject.SetActive(active);

            // Focus the input field when activating it
            if (active)
            {
                _inputField.text = "";
                _inputField.Select();
                _inputField.ActivateInputField();
            }
        }

        public void SetExpectedWord(string word)
        {
            _expectedWord.text = word;
        }

        public void ClearInputField()
        {
            _inputField.text = "";
        }

        private void FocusInputField()
        {
            if (!_inputField.gameObject.activeSelf)
            {
                return;
            }

            _inputField.Select();
            _inputField.ActivateInputField();
        }
    }
}