using System;
using System.Diagnostics;
using UnityEngine;
using Discord;
using Debug = UnityEngine.Debug;

public class DiscordManager : MonoBehaviour
{
    private Discord.Discord discord;
    private const long clientId = 1294704661932015709;
    private UserManager userManager;

    public Texture2D avatarTexture;

    void Start()
    {
                    discord = new Discord.Discord(clientId, (ulong)CreateFlags.NoRequireDiscord);
                    userManager = discord.GetUserManager();
        
                    SetRichPresence();
                
                    userManager.OnCurrentUserUpdate += OnCurrentUserUpdate;
        
        return;
        
        if (IsDiscordRunning())
        {
            discord = new Discord.Discord(clientId, (ulong)CreateFlags.Default);
            userManager = discord.GetUserManager();

            SetRichPresence();
        
            userManager.OnCurrentUserUpdate += OnCurrentUserUpdate;
        }
        else
        {
            Debug.LogWarning("Discord ist nicht erreichbar. Bitte starte Discord, um die Integration zu nutzen.");
        }
    }

    private bool IsDiscordRunning()
    {
        foreach (var process in Process.GetProcesses())
        {
            if (process.ProcessName.ToLower().Contains("discord"))
            {
                return true;
            }
        }
        return false;
    }
    
    private void SetRichPresence()
    {
        var activity = new Activity
        {
            State = GetComponent<StageLoader>().Stage + "",
            Details = "Fan Edition by Fimmel",
            Timestamps = new ActivityTimestamps()
            {
                Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            },
            Assets = new ActivityAssets()
            {
                LargeText = "Fan Edition by Fimmel",
                LargeImage = "logoshadowsmallerrpc",
                SmallImage = "linkiconsmall"
            },
            Type = ActivityType.Playing,
        };

        discord.GetActivityManager().UpdateActivity(activity, result =>
        {
            if (result == Result.Ok)
            {
                Debug.Log("Rich Presence erfolgreich gesetzt.");
            }
            else
            {
                Debug.LogError("Fehler beim Setzen der Rich Presence: " + result);
            }
        });
    }

    private void OnCurrentUserUpdate()
    {
        var currentUser = userManager.GetCurrentUser();
        Debug.Log("Benutzer-ID: " + currentUser.Id);

        var handle = new Discord.ImageHandle
        {
            Type = Discord.ImageType.User,
            Id = currentUser.Id,
            Size = 512
        };

        discord.GetImageManager().Fetch(handle, (result, handleResult) =>
        {
            if (result == Result.Ok)
            {
                avatarTexture = discord.GetImageManager().GetTexture(handleResult);

                avatarTexture = RotateAndFlipTexture(avatarTexture);
                avatarTexture = FlipTextureY(avatarTexture);
    
                Debug.Log("Avatar Texture geladen!");
            }
            else
            {
                Debug.LogError("Fehler beim Abrufen des Avatars: " + result);
            }
        });
    }

    private void Update()
    {
        if (discord != null)
        {
            discord.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        if (userManager != null)
        {
            userManager.OnCurrentUserUpdate -= OnCurrentUserUpdate;
        }
        if (discord != null)
        {
            discord.Dispose();
        }
    }
    
    private Texture2D RotateAndFlipTexture(Texture2D original)
    {
        Texture2D processedTexture = new Texture2D(original.width, original.height);
        Color[] pixels = original.GetPixels();

        for (int y = 0; y < original.height; y++)
        {
            for (int x = 0; x < original.width; x++)
            {
                processedTexture.SetPixel(original.width - 1 - x, original.height - 1 - y, pixels[y * original.width + x]);
            }
        }

        processedTexture.Apply();
        return processedTexture;
    }

    private Texture2D FlipTextureY(Texture2D original)
    {
        Texture2D flippedTexture = new Texture2D(original.width, original.height);
        Color[] pixels = original.GetPixels();

        for (int y = 0; y < original.height; y++)
        {
            for (int x = 0; x < original.width; x++)
            {
                flippedTexture.SetPixel(original.width - 1 - x, y, pixels[y * original.width + x]);
            }
        }

        flippedTexture.Apply();
        return flippedTexture;
    }
}
