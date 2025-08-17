using LNE.GAS;
using LNE.Inputs;
using LNE.Spells;
using LNE.Utilities;
using PurrNet;
using UnityEngine;

namespace LNE.Characters
{
    public class PlayerCharacterSpellPresenter : NetworkBehaviour
    {
        [SerializeField] private AbilityConfig _spellConfig;

        private PlayerInputPresenter _playerInputPresenter;
        private AbilitySystemComponent _abilitySystemComponent;
        private SpellTextManager _spellTextManager;
        private SpellInputPresenter _spellInputPresenter;

        private string _currentSpellText;
        private string _expectedWord;

        private void Awake()
        {
            _playerInputPresenter = GetComponent<PlayerInputPresenter>();
            _abilitySystemComponent = GetComponent<AbilitySystemComponent>();
            _spellTextManager = FindFirstObjectByType<SpellTextManager>();
            _spellInputPresenter = FindFirstObjectByType<SpellInputPresenter>();
        }

        protected override void OnSpawned()
        {
            _playerInputPresenter.SubmitWord += OnSubmitWord;
            _playerInputPresenter.SubmitWordAndEndSpell += OnSubmitWordAndEndSpell;
            _playerInputPresenter.ToggleSpell += OnToggleSpell;
        }

        protected override void OnDespawned()
        {
            _playerInputPresenter.SubmitWord -= OnSubmitWord;
            _playerInputPresenter.SubmitWordAndEndSpell -= OnSubmitWordAndEndSpell;
            _playerInputPresenter.ToggleSpell -= OnToggleSpell;
        }

        private void ActivateSpellMode()
        {
            _currentSpellText = _spellTextManager.GetRandomParagraph();
        }

        private void OnToggleSpell()
        {
            _spellInputPresenter.SetActive(!_spellInputPresenter.IsActive);
            _playerInputPresenter.SetMoveActionActive(!_spellInputPresenter.IsActive);

            if (_spellInputPresenter.IsActive)
            {
                ActivateSpellMode();
                UpdateSpellWord();
            }
        }

        private void OnSubmitWord()
        {
            string word = _spellInputPresenter.InputText.TrimEnd(' ').Split(' ')[0];
            _spellInputPresenter.ClearInputField();

            if (string.IsNullOrEmpty(word))
            {
                return;
            }

            Debug.Log($"Word entered: {word}");

            if (!string.Equals(word, _expectedWord))
            {
                Debug.Log($"Incorrect! Expected '{_expectedWord}' but got '{word}'");
                _spellInputPresenter.SetActive(false);
                return;
            }

            Debug.Log($"Correct! Word '{word}' matches expected '{_expectedWord}'");
            _abilitySystemComponent.TryActivateAbility(_spellConfig);

            // Remove the first word from the remaining text
            _currentSpellText = TextUtility.RemoveFirstWord(_currentSpellText);

            if (string.IsNullOrEmpty(_currentSpellText))
            {
                Debug.Log("Spell completed successfully!");
                // TODO: Handle spell completion (cast spell, etc.)
                return;
            }

            // Update the display to show progress
            UpdateSpellWord();
            string nextWord = TextUtility.GetFirstWord(_currentSpellText);
            Debug.Log($"Next word: {nextWord}");
        }

        private void OnSubmitWordAndEndSpell()
        {
            OnSubmitWord();
            _spellInputPresenter.SetActive(false);
        }

        private void UpdateSpellWord()
        {
            if (string.IsNullOrEmpty(_currentSpellText))
            {
                _currentSpellText = _spellTextManager.GetRandomParagraph();
            }

            _expectedWord = TextUtility.GetFirstWord(_currentSpellText);
            _spellInputPresenter.SetExpectedWord(_expectedWord);
        }
    }
}