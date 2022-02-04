namespace Pacman.Libs
{
    public static class UI
    {
        public static void MsgBox(string content)
            => Apis.Misc.UI.MsgBox(
                Properties.Resources.Name,
                content);

        public static void MsgBoxAsync(string content)
            => Apis.Misc.UI.MsgBoxAsync(
                Properties.Resources.Name,
                content);
    }
}
