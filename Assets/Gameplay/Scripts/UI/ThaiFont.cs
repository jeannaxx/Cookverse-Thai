using UnityEngine;

// ฟอนต์ภาษาไทยที่ฝังในโปรเจกต์ (Sarabun, ใบอนุญาต OFL)
// อยู่ที่ Assets/Gameplay/UI/Resources/Fonts/Sarabun-Regular.ttf
// ใช้แทนฟอนต์ของระบบ เพราะ Quest 3 (Android) ไม่มีฟอนต์ Thonburi ของ Mac
public static class ThaiFont
{
    public const string ResourcePath = "Fonts/Sarabun-Regular";

    private static Font cachedFont;

    public static Font Get()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        cachedFont = Resources.Load<Font>(ResourcePath);

        if (cachedFont == null)
        {
            Debug.LogWarning(
                $"[ฟอนต์] ไม่พบ {ResourcePath} จึงใช้ฟอนต์ของระบบแทน " +
                "ภาษาไทยอาจแสดงไม่ได้บนแว่น");
            cachedFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Thonburi", "Tahoma", "Arial Unicode MS", "Arial" },
                32);
        }

        return cachedFont;
    }
}
