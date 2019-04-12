using Microsoft.Xna.Framework.Input;

namespace Game1.HelperClasses.Comparers
{
    public static class InputComparer
    {
        public static bool AreKeyboardStatesTheSame(KeyboardState previous, KeyboardState current)
        {
            return
                previous.IsKeyDown(Keys.W) && current.IsKeyDown(Keys.W) ||
                previous.IsKeyDown(Keys.A) && current.IsKeyDown(Keys.A) ||
                previous.IsKeyDown(Keys.S) && current.IsKeyDown(Keys.S) ||
                previous.IsKeyDown(Keys.D) && current.IsKeyDown(Keys.D) ||

                previous.IsKeyDown(Keys.Up) && current.IsKeyDown(Keys.Up) ||
                previous.IsKeyDown(Keys.Down) && current.IsKeyDown(Keys.Down) ||
                previous.IsKeyDown(Keys.Left) && current.IsKeyDown(Keys.Left) ||
                previous.IsKeyDown(Keys.Right) && current.IsKeyDown(Keys.Right);
        }
    }
}
