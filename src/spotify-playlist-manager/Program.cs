using SpotifyPlaylistManager.Core;

namespace SpotifyPlaylistManager;

/// <summary>
/// Main entry point for the Spotify Playlist Manager application.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Spotify Playlist Manager v1.0");
        Console.WriteLine("============================");

        var clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            Console.Error.WriteLine("Error: SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET must be set.");
            Environment.Exit(1);
        }

        var authService = new AuthService(clientId, clientSecret);
        var spotifyClient = await authService.AuthenticateAsync();

        var playlistService = new PlaylistService(spotifyClient);

        Console.WriteLine("\nFetching your playlists...");
        var playlists = await playlistService.GetUserPlaylistsAsync();

        if (playlists.Count == 0)
        {
            Console.WriteLine("No playlists found.");
            return;
        }

        Console.WriteLine($"Found {playlists.Count} playlists:");
        for (int i = 0; i < playlists.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {playlists[i].Name} (ID: {playlists[i].Id})");
        }

        Console.Write("\nEnter the number of the playlist to transfer: ");
        if (!int.TryParse(Console.ReadLine(), out int selection) || selection < 1 || selection > playlists.Count)
        {
            Console.Error.WriteLine("Invalid selection.");
            return;
        }

        var selectedPlaylist = playlists[selection - 1];
        Console.Write("Enter target Spotify user ID to transfer to: ");
        var targetUserId = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            Console.Error.WriteLine("Invalid user ID.");
            return;
        }

        Console.WriteLine($"\nTransferring playlist '{selectedPlaylist.Name}' to user '{targetUserId}'...");
        var success = await playlistService.TransferPlaylistAsync(selectedPlaylist.Id, targetUserId);

        Console.WriteLine(success ? "Transfer completed successfully!" : "Transfer failed.");
    }
}
