using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Image readyIcon;

    [Header("Optional: swap sprites instead of just color")] [SerializeField]
    private Sprite readySprite;

    [SerializeField] private Sprite notReadySprite;

    private Texture2D _avatarTexture;
    private Sprite _avatarSprite;

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

        if (this == null) return;
        if (!avatar.HasValue) return;

        var img = avatar.Value;
        _avatarTexture = new Texture2D((int)img.Width, (int)img.Height, TextureFormat.RGBA32, false);
        _avatarTexture.LoadRawTextureData(img.Data);
        _avatarTexture.Apply();

        FlipTextureVertically(_avatarTexture);

        _avatarSprite = Sprite.Create(_avatarTexture, new Rect(0, 0, _avatarTexture.width, _avatarTexture.height),
            new Vector2(0.5f, 0.5f));
        avatarImage.sprite = _avatarSprite;
    }

    private void OnDestroy()
    {
        if (_avatarSprite != null) Destroy(_avatarSprite);
        if (_avatarTexture != null) Destroy(_avatarTexture);
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