using UnityEngine;

namespace ThunderWire.Helpers
{
    public enum Axis { In, Out }

    /// <summary>
    /// Provides additional methods for Input
    /// </summary>
    public static class InputHelper
    {
        /// <summary>
        /// Linearly interpolates Keyboard Button
        /// </summary>
        public static float GetKeyAxis(Axis axis, float from, bool pressed, float speed)
        {
            switch (axis)
            {
                case Axis.In:
                    if (pressed)
                    {
                        if (from < 0.9f)
                        {
                            return from += Time.deltaTime * speed;
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        if (from > 0.1f)
                        {
                            return from -= Time.deltaTime * speed;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                case Axis.Out:
                    if (pressed)
                    {
                        if (from > -0.9f)
                        {
                            return from -= Time.deltaTime * speed;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        if (from < -0.1f)
                        {
                            return from += Time.deltaTime * speed;
                        }
                        else
                        {
                            return 0;
                        }
                    }            
            }

            return 0;
        }
    }
}
