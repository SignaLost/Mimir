namespace Mimir.Common;

/// <summary>Localization helpers.</summary>
internal static class Names
{
    /// <summary>Localizes a token, filling <c>$1</c>, <c>$2</c> from the words; returns the token unchanged before the game's localization exists.</summary>
    internal static string Localize(string token, params string[] words)
    {
        return Localization.m_instance != null ? Localization.m_instance.Localize(token, words) : token;
    }
}
