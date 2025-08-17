using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LNE.Utilities
{
    public enum LetterCaseType
    {
        Default, // No change
        Lower, // Lowercase
        Upper, // Uppercase
        Title // First letter of each word capitalized
    }

    public static class TextUtility
    {
        /// <summary>
        /// Gets a random paragraph from a randomly selected text file in the specified folder
        /// with optional punctuation removal and letter case processing.
        /// </summary>
        /// <param name="filePath">The path to the text file</param>
        /// <param name="shouldRemovePunctuation">Whether to remove punctuation from the text</param>
        /// <param name="letterCaseType">The letter case transformation to apply</param>
        /// <returns>A processed random paragraph as a string</returns>
        public static string GetRandomParagraph(
            string filePath,
            bool shouldRemovePunctuation,
            LetterCaseType letterCaseType
        )
        {
            try
            {
                string randomParagraph = GetRandomRawParagraph(filePath);
                if (!string.IsNullOrEmpty(randomParagraph))
                {
                    if (shouldRemovePunctuation)
                    {
                        randomParagraph = RemovePunctuation(randomParagraph);
                    }

                    // Apply letter case processing
                    randomParagraph = ApplyLetterCase(randomParagraph, letterCaseType);

                    return randomParagraph;
                }
                else
                {
                    Debug.LogWarning("No valid paragraph found in the selected text file.");
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading random paragraph: {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets a random paragraph from a randomly selected text file in the specified folder.
        /// </summary>
        /// <returns>A random paragraph as a string</returns>
        private static string GetRandomRawParagraph(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogError("No .txt files found in the specified folder or folder doesn't exist.");
                return string.Empty;
            }

            string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), filePath);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"Selected text file not found at: {fullPath}");
                return string.Empty;
            }

            string fileContent = File.ReadAllText(fullPath);
            List<string> paragraphs = ExtractParagraphs(fileContent);

            if (paragraphs.Count == 0)
            {
                Debug.LogWarning($"No paragraphs found in the selected text file: {filePath}");
                return string.Empty;
            }

            // Get a random paragraph
            int randomIndex = UnityEngine.Random.Range(0, paragraphs.Count);
            return paragraphs[randomIndex];
        }

        /// <summary>
        /// Gets a random .txt file path from the specified folder.
        /// </summary>
        /// <returns>A random .txt file path, or empty string if none found</returns>
        public static string GetRandomTextFile(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Text files folder is not assigned or path is invalid.");
                return string.Empty;
            }

            string fullFolderPath = Path.Combine(Application.dataPath.Replace("Assets", ""), folderPath);

            if (!Directory.Exists(fullFolderPath))
            {
                Debug.LogError($"Text files folder not found at: {fullFolderPath}");
                return string.Empty;
            }

            string[] txtFiles = Directory.GetFiles(fullFolderPath, "*.txt", SearchOption.TopDirectoryOnly);

            if (txtFiles.Length == 0)
            {
                Debug.LogWarning($"No .txt files found in folder: {fullFolderPath}");
                return string.Empty;
            }

            // Convert back to relative path from Assets
            int randomIndex = UnityEngine.Random.Range(0, txtFiles.Length);
            string selectedFile = txtFiles[randomIndex];

            // Convert absolute path back to relative path starting with "Assets/"
            string relativePath = "Assets" + selectedFile.Substring(Application.dataPath.Length).Replace('\\', '/');

            return relativePath;
        }

        /// <summary>
        /// Extracts paragraphs from the text content, filtering out short lines and metadata.
        /// </summary>
        /// <param name="content">The full text content</param>
        /// <returns>A list of valid paragraphs</returns>
        private static List<string> ExtractParagraphs(string content)
        {
            List<string> paragraphs = new();

            // Split by double newlines to get potential paragraphs
            string[] potentialParagraphs = content.Split(
                new[] { "\r\n\r\n", "\n\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            foreach (string paragraph in potentialParagraphs)
            {
                string cleanedParagraph = paragraph.Trim();

                // Filter out short lines, chapter headers, sidenotes, and illustrations
                if (cleanedParagraph.Length > 100 &&
                    !cleanedParagraph.StartsWith("CHAPTER") &&
                    !cleanedParagraph.StartsWith("[Sidenote:") &&
                    !cleanedParagraph.StartsWith("[Illustration") &&
                    !cleanedParagraph.Equals("[Illustration]") &&
                    cleanedParagraph.Contains(" ")) // Must contain spaces (actual sentences)
                {
                    // Replace line breaks within paragraphs with spaces
                    cleanedParagraph = Regex.Replace(cleanedParagraph, @"\r?\n", " ");
                    // Clean up multiple spaces
                    cleanedParagraph = Regex.Replace(cleanedParagraph, @"\s+", " ");

                    paragraphs.Add(cleanedParagraph);
                }
            }

            return paragraphs;
        }

        /// <summary>
        /// Removes all punctuation from the given text.
        /// </summary>
        /// <param name="text">The text to clean</param>
        /// <returns>Text with all punctuation removed</returns>
        private static string RemovePunctuation(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            // Remove all punctuation characters but keep spaces and letters
            string result = Regex.Replace(text, @"[^\w\s]", "");

            // Clean up multiple spaces that might result from punctuation removal
            result = Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }

        /// <summary>
        /// Applies the specified letter case transformation to the given text.
        /// </summary>
        /// <param name="text">The text to transform</param>
        /// <param name="letterCaseType">The type of letter case transformation to apply</param>
        /// <returns>Text with the specified letter case transformation applied</returns>
        private static string ApplyLetterCase(string text, LetterCaseType letterCaseType)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return letterCaseType switch
            {
                LetterCaseType.Lower => text.ToLowerInvariant(),
                LetterCaseType.Upper => text.ToUpperInvariant(),
                LetterCaseType.Title => ConvertToTitleCase(text),
                _ => text
            };
        }

        /// <summary>
        /// Converts text to title case (first letter of each word capitalized).
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <returns>Text converted to title case</returns>
        private static string ConvertToTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            // Split into words and capitalize the first letter of each word
            string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) +
                               (words[i].Length > 1 ? words[i][1..].ToLowerInvariant() : "");
                }
            }

            return string.Join(" ", words);
        }

        /// <summary>
        /// Gets the first word from the given text.
        /// </summary>
        /// <param name="text">The text to extract the first word from</param>
        /// <returns>The first word, or empty string if no words found</returns>
        public static string GetFirstWord(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string[] words = text.Split(
                new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries
            );
            return words.Length > 0 ? words[0] : string.Empty;
        }

        /// <summary>
        /// Removes the first word from the given text and returns the modified text.
        /// </summary>
        /// <param name="text">The text to remove the first word from</param>
        /// <returns>The text with the first word removed, or empty string if no words remain</returns>
        public static string RemoveFirstWord(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            string[] words = text.Split(
                new[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (words.Length <= 1)
            {
                return string.Empty;
            }

            // Rebuild the string without the first word
            return string.Join(" ", words, 1, words.Length - 1);
        }
    }
}