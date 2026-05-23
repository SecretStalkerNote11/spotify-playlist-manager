using SpotifyAPI.Web;

namespace SpotifyPlaylistManager.Core;

/// <summary>
/// Provides operations for managing Spotify playlists, including listing and transferring.
/// </summary>
public class PlaylistService
{
    private readonly SpotifyClient _spotify;

    public PlaylistService(SpotifyClient spotify)
    {
        _spotify = spotify ?? throw new ArgumentNullException(nameof(spotify));
    }

    /// <summary>
    /// Gets all playlists for the authenticated user.
    /// </summary>
    /// <returns>A list of simplified playlist objects.</returns>
    public async Task<List<SimplePlaylist>> GetUserPlaylistsAsync()
    {
        var playlists = new List<SimplePlaylist>();
        var request = new PlaylistsRequest { Limit = 50 };

        var response = await _spotify.Playlists.CurrentUsers(request);
        playlists.AddRange(response.Items ?? new List<SimplePlaylist>());

        // Paginate through all playlists
        while (response.Next != null)
        {
            response = await _spotify.NextPage(response);
            playlists.AddRange(response.Items ?? new List<SimplePlaylist>());
        }

        return playlists;
    }

    /// <summary>
    /// Transfers a playlist to another user by creating a copy in their account.
    /// Note: This requires the target user to have the playlist added via collaboration or snapshot.
    /// </summary>
    /// <param name="playlistId">The Spotify ID of the playlist to transfer.</param>
    /// <param name="targetUserId">The Spotify user ID of the recipient.</param>
    /// <returns>True if the transfer succeeded, false otherwise.</returns>
    public async Task<bool> TransferPlaylistAsync(string playlistId, string targetUserId)
    {
        try
        {
            // Get the full playlist to copy its details
            var playlist = await _spotify.Playlists.Get(playlistId);

            // Create a new playlist for the target user
            var createRequest = new PlaylistCreateRequest(playlist.Name)
            {
                Description = playlist.Description,
                Public = playlist.Public
            };

            var newPlaylist = await _spotify.Playlists.Create(targetUserId, createRequest);

            // Get all tracks from the original playlist
            var tracks = new List<PlaylistTrack<IPlayableItem>>();
            var tracksRequest = new PlaylistGetItemsRequest { Limit = 100 };
            var tracksResponse = await _spotify.Playlists.GetItems(playlistId, tracksRequest);
            tracks.AddRange(tracksResponse.Items ?? new List<PlaylistTrack<IPlayableItem>>());

            while (tracksResponse.Next != null)
            {
                tracksResponse = await _spotify.NextPage(tracksResponse);
                tracks.AddRange(tracksResponse.Items ?? new List<PlaylistTrack<IPlayableItem>>());
            }

            // Add tracks to the new playlist in batches
            var trackUris = tracks
                .Select(t => t.Track)
                .OfType<FullTrack>()
                .Select(t => t.Uri)
                .ToList();

            for (int i = 0; i < trackUris.Count; i += 100)
            {
                var batch = trackUris.Skip(i).Take(100).ToList();
                var addRequest = new PlaylistAddItemsRequest(batch);
                await _spotify.Playlists.AddItems(newPlaylist.Id, addRequest);
            }

            return true;
        }
        catch (APIException ex)
        {
            Console.Error.WriteLine($"API error during transfer: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error during transfer: {ex.Message}");
            return false;
        }
    }
}
