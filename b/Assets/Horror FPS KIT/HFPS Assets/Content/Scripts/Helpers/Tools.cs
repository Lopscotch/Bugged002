using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using UnityEngine;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ThunderWire.Utility
{
    /// <summary>
    /// Basic Utility Tools for HFPS
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Get MainCamera Instance
        /// </summary>
        public static UnityEngine.Camera MainCamera()
        {
            return Object.FindObjectsOfType<UnityEngine.Camera>().Where(x => x.gameObject.tag == "MainCamera").SingleOrDefault();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Find All Scene Type Objects (Editor Only)
        /// </summary>
        public static List<T> FindAllSceneObjects<T>() where T : Object
        {
            return Resources.FindObjectsOfTypeAll<T>().Where(x => !EditorUtility.IsPersistent(x)).ToList();
        }
#endif

        /// <summary>
        /// Play OneShot Audio 2D
        /// </summary>
        public static void PlayOneShot2D(Vector3 position, AudioClip clip, float volume = 1f)
        {
            GameObject go = new GameObject("OneShotAudio");
            go.transform.position = position;
            AudioSource source = go.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.clip = clip;
            source.volume = volume;
            source.Play();
            Object.Destroy(go, clip.length);
        }

        /// <summary>
        /// Return full GameObject Inspector Path
        /// </summary>
        public static string GameObjectPath(this GameObject obj)
        {
            return string.Join("/", obj.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray());
        }

        /// <summary>
        /// Get string between two chars
        /// </summary>
        public static string GetBetween(this string str, char start, char end)
        {
            string result = str.Split(new char[] { start, end })[1];
            return result;
        }

        /// <summary>
        /// Get string between two chars (Regex)
        /// </summary>
        public static string RegexBetween(this string str, char start, char end)
        {
            Regex regex = new Regex($@"\{start}(.*?)\{end}");
            Match match = regex.Match(str);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return str;
        }

        /// <summary>
        /// Get strings between two chars (Regex)
        /// </summary>
        public static string[] RegexBetweens(this string str, char start, char end)
        {
            Regex regex = new Regex($@"\{start}(.*?)\{end}");
            MatchCollection collection = regex.Matches(str);
            
            if(collection.Count < 1)
            {
                return new string[0];
            }

            IEnumerable<string> match = collection.Cast<Match>().Select(x => x.Groups[1].Value);
            return match.ToArray();
        }

        /// <summary>
        /// Check if string between char match tag (Regex)
        /// </summary>
        public static bool RegexMatch(this string str, char start, char end, string tag)
        {
            Regex regex = new Regex($@"\{start}({tag})\{end}");
            Match result = regex.Match(str);
            return result.Success;
        }

        /// <summary>
        /// Replace string part inside two chars
        /// </summary>
        public static string ReplacePart(this string str, char start, char end, string replace)
        {
            string old = str.Substring(start, end - start + 1);
            return str.Replace(old, replace);
        }

        /// <summary>
        /// Replace string part inside two chars (Regex)
        /// </summary>
        public static string RegexReplace(this string str, char start, char end, string replace)
        {
            Regex regex = new Regex($@"\{start}([^\{end}]+)\{end}");
            string result = regex.Replace(str, replace);

            return result;
        }

        /// <summary>
        /// Replace tag inside two chars (Regex)
        /// </summary>
        public static string RegexReplaceTag(this string str, char start, char end, string tag, string replace)
        {
            Regex regex = new Regex($@"\{start}({tag})\{end}");
            string result = regex.Replace(str, replace);

            return result;
        }

        /// <summary>
        /// Get string with specified input between two chars
        /// </summary>
        public static string GetStringWithInput(this string str, char start, char end, InputController input = null)
        {
            string result = "";

            if (input == null)
            {
                input = InputController.Instance;
            }

            char[] stringCh = str.ToCharArray();

            if (stringCh.Contains(start) && stringCh.Contains(end))
            {
                string key = input.GetInput(str.RegexBetween(start, end)).ToString();
                result = str.RegexReplace(start, end, key);
            }

            return result;
        }

        /// <summary>
        /// Get string with specified input between two chars, with before and after separators
        /// </summary>
        public static string GetStringWithInput(this string str, char start, char end, char before, char after, InputController input = null)
        {
            if (input == null)
            {
                input = InputController.Instance;
            }

            char[] stringCh = str.ToCharArray();

            if (stringCh.Contains(start) && stringCh.Contains(end))
            {
                string key = input.GetInput(str.RegexBetween(start, end)).ToString();
                return str.RegexReplace(start, end, before + key + after);
            }

            return str;
        }

        /// <summary>
        /// Get Titled Case text
        /// </summary>
        public static string TitleCase(this string str)
        {
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            TextInfo textInfo = cultureInfo.TextInfo;
            return textInfo.ToTitleCase(str);
        }

        /// <summary>
        /// Clamp Vector3
        /// </summary>
        public static Vector3 Clamp(this Vector3 vec, float min, float max)
        {
            return new Vector3(Mathf.Clamp(vec.x, min, max), Mathf.Clamp(vec.y, min, max), Mathf.Clamp(vec.z, min, max));
        }
    }
}
