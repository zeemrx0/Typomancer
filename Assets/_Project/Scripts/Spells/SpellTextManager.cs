using LNE.Utilities;
using UnityEditor;
using UnityEngine;

namespace LNE.Spells
{
    public class SpellTextManager : MonoBehaviour
    {
        [SerializeField] private DefaultAsset _textFilesFolder;

        private string _textFilesFolderPath;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Cache the folder path when the DefaultAsset is assigned in the editor
            _textFilesFolderPath = _textFilesFolder ? AssetDatabase.GetAssetPath(_textFilesFolder) : string.Empty;
        }
#endif

        public string GetRandomParagraph(
            bool shouldRemovePunctuation = true,
            LetterCaseType letterCaseType = LetterCaseType.Default
        )
        {
            string randomFile = GetRandomFile();
            return TextUtility.GetRandomParagraph(randomFile, shouldRemovePunctuation, letterCaseType);
        }

        private string GetRandomFile()
        {
            return TextUtility.GetRandomTextFile(_textFilesFolderPath);
        }
    }
}