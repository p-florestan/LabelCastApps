using LabelCast;

namespace LabelCastWeb
{
    /// <summary>
    /// Static App class to hold values which remain constant across all web requests.
    /// </summary>
    public static class App
    {
        /// <summary>
        /// Custom static configuration store for the web app
        /// </summary>
        public static LabelConfig Config { get; set; } = new LabelConfig();


    }
}
