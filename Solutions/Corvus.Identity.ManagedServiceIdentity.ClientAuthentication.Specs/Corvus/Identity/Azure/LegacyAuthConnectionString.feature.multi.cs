namespace Corvus.Identity.Azure
{
    using NUnit.Framework;

    /// <summary>
    /// Adds in multi-host-mode execution.
    /// </summary>
    [TestFixtureSource(nameof(Inputs))]
    public partial class LegacyAuthConnectionStringsFeature
    {
        internal static readonly Modes[] Inputs = { Modes.Direct, Modes.DiWithConnectionString };

        public LegacyAuthConnectionStringsFeature(Modes mode)
        {
            this.Mode = mode;
        }

        public enum Modes
        {
            Direct,
            DiWithConnectionString,
        }

        public Modes Mode { get; }
    }
}