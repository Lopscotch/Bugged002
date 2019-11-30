using System;
using UnityEngine;

namespace HFPS.Prefs
{
    /// <summary>
    /// Provides main HFPS/Game PlayerPrefs functions and constants.
    /// </summary>
    public static class Prefs
    {
        public const string LOAD_STATE = "LoadState";
        public const string LOAD_LEVEL_NAME = "LevelToLoad";
        public const string LOAD_SAVE_NAME = "LoadSaveName";

        #region HFPS Functions
        /// <summary>
        /// Get value from (<see cref="LOAD_STATE"/>) constant.
        /// </summary>
        public static int Game_LoadState()
        {
            return PlayerPrefs.GetInt(LOAD_STATE);
        }

        /// <summary>
        /// Set value to (<see cref="LOAD_STATE"/>) constant.
        /// </summary>
        public static void Game_LoadState(int value)
        {
            PlayerPrefs.SetInt(LOAD_STATE, value);
        }

        /// <summary>
        /// Get value from (<see cref="LOAD_LEVEL_NAME"/>) constant.
        /// </summary>
        public static string Game_LevelName()
        {
            return PlayerPrefs.GetString(LOAD_LEVEL_NAME);
        }

        /// <summary>
        /// Set value to (<see cref="LOAD_LEVEL_NAME"/>) constant.
        /// </summary>
        public static void Game_LevelName(string value)
        {
            PlayerPrefs.SetString(LOAD_LEVEL_NAME, value);
        }

        /// <summary>
        /// Get value from (<see cref="LOAD_SAVE_NAME"/>) constant.
        /// </summary>
        public static string Game_SaveName()
        {
            return PlayerPrefs.GetString(LOAD_SAVE_NAME);
        }

        /// <summary>
        /// Set value to (<see cref="LOAD_SAVE_NAME"/>) constant.
        /// </summary>
        public static void Game_SaveName(string value)
        {
            PlayerPrefs.SetString(LOAD_SAVE_NAME, value);
        }
        #endregion

        /// <summary>
        /// Save Key/Value to PlayerPrefs.
        /// </summary>
        public static void Save(string key, object value)
        {
            Type type = value.GetType();

            if (type == typeof(string))
            {
                PlayerPrefs.SetString(key, value.ToString());
            }
            else if (type == typeof(int))
            {
                PlayerPrefs.SetInt(key, int.Parse(value.ToString()));
            }
            else if (type == typeof(float))
            {
                PlayerPrefs.SetFloat(key, float.Parse(value.ToString()));
            }
            else
            {
                Debug.LogError("Supported types are (string, int, float)!");
            }
        }

        /// <summary>
        /// Load PlayerPrefs Value.
        /// </summary>
        public static T Load<T>(string key)
        {
            return (T)PrefsConvert(typeof(T), key);
        }

        /// <summary>
        /// Check for Key existence.
        /// </summary>
        public static bool Exist(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        static object PrefsConvert(Type type, string key)
        {
            if (type == typeof(string))
            {
                return PlayerPrefs.GetString(key);
            }
            else if (type == typeof(int))
            {
                return PlayerPrefs.GetInt(key);
            }
            else if (type == typeof(float))
            {
                return PlayerPrefs.GetFloat(key);
            }

            return null;
        }
    }
}
