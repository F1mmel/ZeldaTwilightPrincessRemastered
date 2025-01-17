using System;
using System.Diagnostics;
using UnityEngine;
using Discord;
using Debug = UnityEngine.Debug;

public class DiscordManager : MonoBehaviour
{
    private Discord.Discord discord;
    private const long clientId = 1294704661932015709; // Deine Application ID (Client-ID)
    private UserManager userManager;

    public Texture2D avatarTexture; // Avatar Texture

    void Start()
    {
                    discord = new Discord.Discord(clientId, (ulong)CreateFlags.NoRequireDiscord);
                    userManager = discord.GetUserManager();
        
                    // Setze den Rich Presence Status
                    SetRichPresence();
                
                    // Registriere das Callback für die Aktualisierung des aktuellen Benutzers
                    userManager.OnCurrentUserUpdate += OnCurrentUserUpdate;
        
        return;
        
        // Überprüfen, ob Discord läuft
        if (IsDiscordRunning())
        {
            // Initialisiere den Discord Client
            discord = new Discord.Discord(clientId, (ulong)CreateFlags.Default);
            userManager = discord.GetUserManager();

            // Setze den Rich Presence Status
            SetRichPresence();
        
            // Registriere das Callback für die Aktualisierung des aktuellen Benutzers
            userManager.OnCurrentUserUpdate += OnCurrentUserUpdate;
        }
        else
        {
            // Discord ist nicht verfügbar
            Debug.LogWarning("Discord ist nicht erreichbar. Bitte starte Discord, um die Integration zu nutzen.");
        }
    }

    private bool IsDiscordRunning()
    {
        // Suche nach dem Discord-Prozess
        foreach (var process in Process.GetProcesses())
        {
            if (process.ProcessName.ToLower().Contains("discord"))
            {
                return true; // Discord läuft
            }
        }
        return false; // Discord läuft nicht
    }
    
    private void SetRichPresence()
    {
        var activity = new Activity
        {
            //State = "Ordon Village", // Based on current stage
            State = GetComponent<StageLoader>().Stage + "", // Based on current stage
            Details = "Fan Edition by Fimmel",
            Timestamps = new ActivityTimestamps()
            {
                Start = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            },
            Assets = new ActivityAssets()
            {
                LargeText = "Fan Edition by Fimmel",
                LargeImage = "logoshadowsmallerrpc",
                SmallImage = "linkiconsmall" // Based on current stage
            },
            Type = ActivityType.Playing,
        };

        // Setze die Aktivität
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
            Size = 512 // Die gewünschte Avatar-Größe (128, 256, 512, etc.)
        };

        discord.GetImageManager().Fetch(handle, (result, handleResult) =>
        {
            if (result == Result.Ok)
            {
                // Avatar erfolgreich abgerufen
                avatarTexture = discord.GetImageManager().GetTexture(handleResult);

                // Drehe die Textur um 180 Grad
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
        // Aktualisiere den Discord Client, um die Verbindung aktiv zu halten
        if (discord != null)
        {
            discord.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        // Bereinige den Discord Client
        if (userManager != null)
        {
            userManager.OnCurrentUserUpdate -= OnCurrentUserUpdate; // Registriere das Callback ab
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
                // Spiegeln an der X- und Y-Achse (Drehung um 180 Grad)
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
                // Spiegeln an der Y-Achse
                flippedTexture.SetPixel(original.width - 1 - x, y, pixels[y * original.width + x]);
            }
        }

        flippedTexture.Apply();
        return flippedTexture;
    }
}
