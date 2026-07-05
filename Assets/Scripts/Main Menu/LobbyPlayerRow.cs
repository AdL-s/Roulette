using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image readyIcon;

    [Header("Optional: swap sprites instead of just color")]
    [SerializeField] private Sprite readySprite;
    [SerializeField] private Sprite notReadySprite;

    public void Setup(Friend member, bool isReady)
    {
        nameText.text = member.Name;
        LoadAvatar(member.Id);
        SetReady(isReady);
    }
    
    public void SetReady(bool isReady)
    {
        if (readySprite != null && notReadySprite != null)
            readyIcon.sprite = isReady ? readySprite : notReadySprite;
        else
            readyIcon.color = isReady ? Color.green : Color.gray;
    }

    private async void LoadAvatar(SteamId steamId)
    {
        var avatar = await SteamFriends.GetLargeAvatarAsync(steamId);
        if (avatar.HasValue)
        {
            var img = avatar.Value;
            var texture = new Texture2D((int)img.Width, (int)img.Height, TextureFormat.RGBA32, false);
            texture.LoadRawTextureData(img.Data);
            texture.Apply();

            FlipTextureVertically(texture);

            avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
    private void FlipTextureVertically(Texture2D texture)
    {
        var pixels = texture.GetPixels32();
        var flipped = new Color32[pixels.Length];
        int width = texture.width;
        int height = texture.height;

        for (int y = 0; y < height; y++)
        {
            System.Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
        }

        texture.SetPixels32(flipped);
        texture.Apply();
    }
}